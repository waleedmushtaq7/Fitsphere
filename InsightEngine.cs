using FitSphere.Data;
using FitSphere.Models;

namespace FitSphere.Services;

public class Suggestion
{
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; }
    public string Kind { get; set; }      // Start, Increase, Hold, Reduce
    public string Headline { get; set; }
    public string Reason { get; set; }    // always cites the user's real numbers
    public double Weight { get; set; }
    public int Reps { get; set; }
}

public class Insight
{
    public string Title { get; set; }
    public string Body { get; set; }
    public string Tone { get; set; }      // Good, Info, Warn
}

/// <summary>
/// Rule-based "smart" features. Every output is computed from the user's own records and carries the reason.
/// Nothing here is random or hard-coded advice.
/// </summary>
public class InsightEngine
{
    readonly AppDatabase _db;
    public InsightEngine(AppDatabase db) => _db = db;

    static double Increment(Exercise ex) =>
        ex.Equipment == "Dumbbell" ? 2 : (ex.Equipment == "Barbell" && ex.MuscleGroup == "Legs") ? 5 : 2.5;

    static double Round05(double v) => Math.Round(v * 2) / 2;

    // ============ 1. Progressive-overload suggestion ============
    public async Task<Suggestion> SuggestAsync(Exercise ex, int targetSets, int targetReps)
    {
        var sets = await _db.Query<SetLog>(
            "SELECT s.* FROM SetLog s JOIN WorkoutSession w ON s.SessionId = w.Id WHERE w.UserId = ? AND w.Completed = 1 AND s.ExerciseId = ? AND s.Completed = 1",
            Svc.Uid, ex.Id);
        var sessions = sets.GroupBy(s => s.SessionId).OrderByDescending(g => g.Key).Take(2).Select(g => g.ToList()).ToList();

        var sg = new Suggestion { ExerciseId = ex.Id, ExerciseName = ex.Name, Reps = targetReps };
        if (sessions.Count == 0)
        {
            sg.Kind = "Start";
            sg.Headline = "Start light";
            sg.Reason = $"You haven't completed {ex.Name} yet. Pick a weight you can lift for {targetReps} clean reps; suggestions begin after your first session.";
            return sg;
        }

        var last = sessions[0];
        double w = last.Max(x => x.WeightKg);
        bool Success(List<SetLog> l) => l.Count >= targetSets && l.All(x => x.Reps >= targetReps);
        bool Fail(List<SetLog> l) => l.Average(x => x.Reps) < 0.8 * targetReps;
        int hit = last.Count(x => x.Reps >= targetReps);

        if (w <= 0) // bodyweight movement: progress with reps
        {
            if (Success(last))
            {
                sg.Kind = "Increase"; sg.Reps = targetReps + 2;
                sg.Headline = $"Aim for {sg.Reps} reps";
                sg.Reason = $"Last session you did {last.Count} sets with every set at {targetReps}+ reps (best {last.Max(x => x.Reps)}). Add 2 reps per set, or move to a harder variation.";
            }
            else
            {
                sg.Kind = "Hold";
                sg.Headline = $"Repeat {targetReps} reps";
                sg.Reason = $"Last session {hit} of {targetSets} sets reached {targetReps} reps. Hit the target on every set before adding reps.";
            }
            return sg;
        }

        double inc = Increment(ex);
        if (Success(last))
        {
            sg.Kind = "Increase"; sg.Weight = w + inc;
            sg.Headline = $"Try {Num.F(sg.Weight)} kg";
            sg.Reason = $"Last session you completed {last.Count} sets of {targetReps}+ reps at {Num.F(w)} kg. Every set hit the target, so add {Num.F(inc)} kg.";
        }
        else if (sessions.Count == 2 && Fail(sessions[0]) && Fail(sessions[1]))
        {
            sg.Kind = "Reduce"; sg.Weight = Round05(w * 0.9);
            sg.Headline = $"Drop to {Num.F(sg.Weight)} kg";
            sg.Reason = $"Your average reps were below 80% of the {targetReps}-rep target in your last two sessions at about {Num.F(w)} kg. A 10% reduction lets you rebuild with good form.";
        }
        else
        {
            sg.Kind = "Hold"; sg.Weight = w;
            sg.Headline = $"Stay at {Num.F(w)} kg";
            sg.Reason = $"At {Num.F(w)} kg you reached {targetReps}+ reps on {hit} of {targetSets} sets last time. Repeat the weight until every set hits the target.";
        }
        return sg;
    }

    /// <summary>Suggestions for every exercise in the user's plans that already has history.</summary>
    public async Task<List<Suggestion>> PlanSuggestionsAsync()
    {
        var res = new List<Suggestion>();
        var seen = new HashSet<int>();
        foreach (var plan in await Svc.Workouts.PlansAsync())
            foreach (var it in await Svc.Workouts.ItemsAsync(plan.Id))
            {
                if (!seen.Add(it.Ex.Id)) continue;
                var sg = await SuggestAsync(it.Ex, it.Pe.Sets, it.Pe.Reps);
                if (sg.Kind != "Start") res.Add(sg);
            }
        return res;
    }

    // ============ 2. Personal progress insights ============
    public async Task<List<Insight>> BuildAsync()
    {
        var u = Svc.User;
        var list = new List<Insight>();
        var today = DateTime.Today;

        // --- Consistency ---
        var c = await Svc.Habits.ConsistencyAsync();
        if (c.Percent.HasValue && c.Scheduled >= 3)
        {
            int p = c.Percent.Value;
            string basis = $"You completed {c.Completed} of {c.Scheduled} scheduled workouts ({p}%) in the last {c.WindowDays} days.";
            if (p >= 80) list.Add(new Insight { Tone = "Good", Title = "Strong consistency", Body = basis + " Staying above 80% is the biggest driver of long-term progress." });
            else if (p >= 50) list.Add(new Insight { Tone = "Info", Title = "Room to be more consistent", Body = basis + " If your plan feels too packed, schedule fewer days that you can reliably keep." });
            else list.Add(new Insight { Tone = "Warn", Title = "Many missed sessions", Body = basis + " Consider fewer training days or moving workouts to times that suit your week." });
        }

        // --- Body weight trend vs goal ---
        var weights = (await Svc.Progress.RecordsAsync("Weight")).Where(r => r.Date.Date >= today.AddDays(-28)).ToList();
        if (weights.Count >= 2)
        {
            var first = weights.First(); var last = weights.Last();
            double span = (last.Date.Date - first.Date.Date).TotalDays;
            if (span >= 5)
            {
                double rate = (last.Value - first.Value) / span * 7;
                string basis = $"Your weight went from {Num.F(first.Value)} to {Num.F(last.Value)} kg over {span:0} days ({rate:+0.00;-0.00;0.00} kg/week).";
                switch (u.Goal)
                {
                    case "Weight Loss":
                        if (rate < -1.0) list.Add(new Insight { Tone = "Warn", Title = "Losing weight quickly", Body = basis + " That is faster than about 1 kg/week, which can cost muscle. Consider eating a little more." });
                        else if (rate <= -0.2) list.Add(new Insight { Tone = "Good", Title = "Weight loss on track", Body = basis + " That is a steady, sustainable pace for your goal." });
                        else if (rate > 0.2) list.Add(new Insight { Tone = "Warn", Title = "Weight is rising", Body = basis + " Your goal is weight loss, so review your daily calorie intake against your target." });
                        else list.Add(new Insight { Tone = "Info", Title = "Weight is steady", Body = basis + " For weight loss, a small calorie reduction may be needed to restart progress." });
                        break;
                    case "Muscle Building":
                        if (rate >= 0.1 && rate <= 0.5) list.Add(new Insight { Tone = "Good", Title = "Healthy lean-gain pace", Body = basis + " Gaining 0.1-0.5 kg/week is a good range for building muscle." });
                        else if (rate > 0.5) list.Add(new Insight { Tone = "Info", Title = "Gaining quickly", Body = basis + " Gains above 0.5 kg/week often include extra fat. A smaller surplus may suit you better." });
                        else list.Add(new Insight { Tone = "Warn", Title = "Weight isn't increasing", Body = basis + " For muscle building, try adding roughly 150-250 kcal per day." });
                        break;
                    default:
                        if (Math.Abs(rate) < 0.3) list.Add(new Insight { Tone = "Good", Title = "Weight is stable", Body = basis + " Stable weight fits a general-fitness goal." });
                        else list.Add(new Insight { Tone = "Info", Title = "Weight is changing", Body = basis + " Check this matches what you want from your current goal." });
                        break;
                }
            }
        }

        // --- Nutrition (last 7 days, only days that have entries) ---
        var food = await Svc.Nutrition.RangeAsync(today.AddDays(-6), today);
        var days = food.GroupBy(f => f.Date.Date).Select(g => (Cal: g.Sum(x => x.Calories), Prot: g.Sum(x => x.Protein))).ToList();
        if (days.Count >= 3)
        {
            double avgCal = days.Average(d => d.Cal), avgProt = days.Average(d => d.Prot);
            if (u.ProteinTarget > 0)
            {
                double ratio = avgProt / u.ProteinTarget;
                string basis = $"Across {days.Count} logged days you averaged {avgProt:0} g protein ({ratio * 100:0}% of your {u.ProteinTarget} g target).";
                if (ratio < 0.85) list.Add(new Insight { Tone = "Warn", Title = "Protein is below target", Body = basis + " Adding a protein-rich food to one meal can close the gap." });
                else if (ratio >= 0.95) list.Add(new Insight { Tone = "Good", Title = "Protein target met", Body = basis });
            }
            if (u.CalorieTarget > 0)
            {
                double ratio = avgCal / u.CalorieTarget;
                string basis = $"Across {days.Count} logged days you averaged {avgCal:0} kcal against a target of {u.CalorieTarget} kcal.";
                if (ratio > 1.1) list.Add(new Insight { Tone = u.Goal == "Muscle Building" ? "Info" : "Warn", Title = "Calories above target", Body = basis });
                else if (ratio < 0.8) list.Add(new Insight { Tone = u.Goal == "Weight Loss" ? "Info" : "Warn", Title = "Calories well below target", Body = basis + (u.Goal == "Weight Loss" ? " A moderate deficit is fine, but avoid dropping much lower." : " Eating too little can hold back training progress and recovery.") });
            }
        }

        // --- Training volume: last 7 days vs the 7 before ---
        var hist = await Svc.Workouts.HistoryAsync();
        double thisW = hist.Where(s => s.StartedAt.Date >= today.AddDays(-6)).Sum(s => s.TotalVolume);
        double prevW = hist.Where(s => s.StartedAt.Date >= today.AddDays(-13) && s.StartedAt.Date < today.AddDays(-6)).Sum(s => s.TotalVolume);
        if (thisW > 0 && prevW > 0)
        {
            double ch = (thisW - prevW) / prevW * 100;
            string basis = $"Your total lifted volume was {thisW:0} kg over the last 7 days versus {prevW:0} kg the week before ({ch:+0;-0}%).";
            if (ch >= 5) list.Add(new Insight { Tone = "Good", Title = "Training volume is up", Body = basis });
            else if (ch <= -25) list.Add(new Insight { Tone = "Info", Title = "Training volume dropped", Body = basis + " If this wasn't a planned deload, check your schedule." });
        }

        // --- Hydration ---
        var water = (await Svc.Habits.WaterByDayAsync(7)).Where(kv => kv.Value > 0).ToList();
        if (water.Count >= 3)
        {
            double avg = water.Average(kv => kv.Value);
            if (avg < u.WaterTargetMl * 0.75)
                list.Add(new Insight { Tone = "Warn", Title = "Hydration is low", Body = $"On {water.Count} tracked days you averaged {avg / 1000:0.0} L, under 75% of your {u.WaterTargetMl / 1000.0:0.0} L goal." });
        }

        // --- Recent personal records ---
        var recent = (await Svc.Workouts.RecordsAsync()).Where(r => r.Date.Date >= today.AddDays(-14)).Take(3).ToList();
        if (recent.Count > 0)
            list.Add(new Insight
            {
                Tone = "Good", Title = "Recent personal records",
                Body = string.Join(", ", recent.Select(r => r.WeightKg > 0 ? $"{r.ExerciseName} {Num.F(r.WeightKg)} kg x {r.Reps}" : $"{r.ExerciseName} {r.Reps} reps")) + ". These are your best sets from the last two weeks."
            });

        return list.OrderBy(i => i.Tone == "Warn" ? 0 : i.Tone == "Good" ? 1 : 2).ToList();
    }
}
