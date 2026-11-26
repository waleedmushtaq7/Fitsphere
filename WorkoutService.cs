using FitSphere.Data;
using FitSphere.Models;

namespace FitSphere.Services;

public class PersonalRecord
{
    public string ExerciseName { get; set; }
    public double WeightKg { get; set; }
    public int Reps { get; set; }
    public DateTime Date { get; set; }
}

public class FinishResult
{
    public int Minutes { get; set; }
    public double Volume { get; set; }
    public int SetsDone { get; set; }
    public List<string> NewRecords { get; set; } = new();
}

public class WorkoutService
{
    readonly AppDatabase _db;
    public WorkoutService(AppDatabase db) => _db = db;

    // ---------- Plans ----------
    public async Task<List<WorkoutPlan>> PlansAsync() =>
        (await _db.ForUser<WorkoutPlan>(Svc.Uid)).OrderBy(p => p.CreatedAt).ToList();

    public async Task<List<PlanItem>> ItemsAsync(int planId)
    {
        var rows = await _db.Query<PlanExercise>("SELECT * FROM PlanExercise WHERE PlanId = ? ORDER BY OrderIndex", planId);
        var all = (await Svc.Exercises.AllAsync()).ToDictionary(e => e.Id);
        return rows.Where(r => all.ContainsKey(r.ExerciseId)).Select(r => new PlanItem { Pe = r, Ex = all[r.ExerciseId] }).ToList();
    }

    public async Task SavePlanAsync(WorkoutPlan plan, List<PlanItem> items)
    {
        plan.UserId = Svc.Uid;
        if (plan.Id == 0) plan.CreatedAt = DateTime.Now;
        await _db.Save(plan);
        await _db.Exec("DELETE FROM PlanExercise WHERE PlanId = ?", plan.Id);
        int i = 0;
        foreach (var it in items)
        {
            it.Pe.Id = 0;
            it.Pe.PlanId = plan.Id;
            it.Pe.ExerciseId = it.Ex.Id;
            it.Pe.OrderIndex = i++;
            await _db.Save(it.Pe);
        }
        await Svc.Reminders.RescheduleAsync();
    }

    public async Task DeletePlanAsync(int planId)
    {
        await _db.Exec("DELETE FROM PlanExercise WHERE PlanId = ?", planId);
        await _db.Exec("DELETE FROM WorkoutPlan WHERE Id = ?", planId);
        await Svc.Reminders.RescheduleAsync();
    }

    public async Task<List<WorkoutPlan>> TodaysPlansAsync()
    {
        if (await Svc.Habits.IsRestDayAsync(DateTime.Today)) return new List<WorkoutPlan>();
        var dow = DateTime.Today.DayOfWeek;
        return (await PlansAsync()).Where(p => p.HasDay(dow)).ToList();
    }

    public static int EstimateMinutes(IEnumerable<PlanItem> items) =>
        Math.Max(5, (int)Math.Round(items.Sum(i => i.Pe.Sets * (40 + i.Pe.RestSeconds)) / 60.0));

    // ---------- Sessions ----------
    public async Task<int> StartAsync(WorkoutPlan plan)
    {
        var items = await ItemsAsync(plan.Id);
        var session = new WorkoutSession { UserId = Svc.Uid, PlanId = plan.Id, PlanName = plan.Name, StartedAt = DateTime.Now };
        await _db.Save(session);
        foreach (var it in items)
        {
            // Pre-fill weight/reps from the rule-based progression suggestion (based on past sessions).
            var sg = await Svc.Insights.SuggestAsync(it.Ex, it.Pe.Sets, it.Pe.Reps);
            for (int n = 1; n <= it.Pe.Sets; n++)
            {
                await _db.Save(new SetLog
                {
                    SessionId = session.Id, ExerciseId = it.Ex.Id, ExerciseName = it.Ex.Name, SetNumber = n,
                    TargetReps = it.Pe.Reps, RestSeconds = it.Pe.RestSeconds,
                    Reps = sg.Reps > 0 ? sg.Reps : it.Pe.Reps, WeightKg = sg.Weight
                });
            }
        }
        return session.Id;
    }

    public async Task<WorkoutSession> ActiveSessionAsync() =>
        (await _db.ForUser<WorkoutSession>(Svc.Uid)).Where(s => !s.Completed).OrderByDescending(s => s.Id).FirstOrDefault();

    public Task<WorkoutSession> SessionAsync(int id) => _db.Get<WorkoutSession>(id);

    public async Task<List<SetLog>> SetsAsync(int sessionId) =>
        await _db.Query<SetLog>("SELECT * FROM SetLog WHERE SessionId = ? ORDER BY Id", sessionId);

    public Task SaveSetAsync(SetLog s) => _db.Save(s);

    public async Task<SetLog> AddSetAsync(SetLog like)
    {
        var s = new SetLog
        {
            SessionId = like.SessionId, ExerciseId = like.ExerciseId, ExerciseName = like.ExerciseName,
            SetNumber = like.SetNumber + 1, TargetReps = like.TargetReps, RestSeconds = like.RestSeconds,
            Reps = like.Reps, WeightKg = like.WeightKg
        };
        await _db.Save(s);
        return s;
    }

    public async Task<FinishResult> FinishAsync(WorkoutSession session)
    {
        var sets = await SetsAsync(session.Id);
        var done = sets.Where(x => x.Completed).ToList();
        var prior = await _db.Query<SetLog>(
            "SELECT s.* FROM SetLog s JOIN WorkoutSession w ON s.SessionId = w.Id WHERE w.UserId = ? AND w.Completed = 1 AND s.Completed = 1 AND w.Id <> ?",
            Svc.Uid, session.Id);

        var res = new FinishResult { SetsDone = done.Count };
        foreach (var g in done.GroupBy(x => x.ExerciseId))
        {
            var old = prior.Where(p => p.ExerciseId == g.Key).ToList();
            if (old.Count == 0) continue;
            bool weighted = g.Any(x => x.WeightKg > 0);
            double now = weighted ? g.Max(x => x.WeightKg) : g.Max(x => x.Reps);
            double before = weighted ? old.Max(x => x.WeightKg) : old.Max(x => x.Reps);
            if (now > before)
                res.NewRecords.Add(weighted ? $"{g.First().ExerciseName}: {Num.F(now)} kg" : $"{g.First().ExerciseName}: {now:0} reps");
        }

        session.Completed = true;
        session.EndedAt = DateTime.Now;
        session.DurationSeconds = (int)Math.Min((session.EndedAt.Value - session.StartedAt).TotalSeconds, 6 * 3600);
        session.TotalVolume = done.Sum(x => x.Reps * x.WeightKg);
        await _db.Save(session);

        res.Minutes = Math.Max(1, session.DurationSeconds / 60);
        res.Volume = session.TotalVolume;
        return res;
    }

    public async Task DiscardAsync(int sessionId)
    {
        await _db.Exec("DELETE FROM SetLog WHERE SessionId = ?", sessionId);
        await _db.Exec("DELETE FROM WorkoutSession WHERE Id = ?", sessionId);
    }

    public Task DeleteSessionAsync(int sessionId) => DiscardAsync(sessionId);

    public async Task<List<WorkoutSession>> HistoryAsync() =>
        (await _db.ForUser<WorkoutSession>(Svc.Uid)).Where(s => s.Completed).OrderByDescending(s => s.StartedAt).ToList();

    public async Task<List<PersonalRecord>> RecordsAsync()
    {
        var sessions = (await HistoryAsync()).ToDictionary(s => s.Id);
        var sets = await _db.Query<SetLog>(
            "SELECT s.* FROM SetLog s JOIN WorkoutSession w ON s.SessionId = w.Id WHERE w.UserId = ? AND w.Completed = 1 AND s.Completed = 1", Svc.Uid);
        var list = new List<PersonalRecord>();
        foreach (var g in sets.GroupBy(x => x.ExerciseId))
        {
            SetLog best = g.Any(x => x.WeightKg > 0)
                ? g.Where(x => x.WeightKg > 0).OrderByDescending(x => x.WeightKg * (1 + x.Reps / 30.0)).First()   // Epley 1RM estimate
                : g.OrderByDescending(x => x.Reps).First();
            list.Add(new PersonalRecord
            {
                ExerciseName = best.ExerciseName, WeightKg = best.WeightKg, Reps = best.Reps,
                Date = sessions.TryGetValue(best.SessionId, out var ss) ? ss.StartedAt : DateTime.Now
            });
        }
        return list.OrderByDescending(r => r.Date).ToList();
    }
}
