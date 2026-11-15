using FitSphere.Data;
using FitSphere.Models;

namespace FitSphere.Services;

public class GoalSummary
{
    public double? WeightProgress { get; set; }   // 0..1
    public double StartWeight { get; set; }
    public double CurrentWeight { get; set; }
    public double TargetWeight { get; set; }
    public int WeekWorkouts { get; set; }
    public int WeekGoal { get; set; }
    public int WaterDaysHit { get; set; }
    public int WaterDaysTracked { get; set; }
}

public class MeasurementSummary
{
    public string Type { get; set; }
    public double Latest { get; set; }
    public double Change { get; set; }
    public DateTime Date { get; set; }
}

public class ProgressService
{
    readonly AppDatabase _db;
    public ProgressService(AppDatabase db) => _db = db;

    public static readonly string[] BodyTypes = { "Weight", "Chest", "Waist", "Hips", "Arms", "Thighs" };

    public async Task<List<BodyRecord>> RecordsAsync(string type = null)
    {
        var all = await _db.ForUser<BodyRecord>(Svc.Uid);
        return all.Where(r => type == null || r.Type == type).OrderBy(r => r.Date).ToList();
    }

    public async Task SaveRecordAsync(BodyRecord r)
    {
        r.UserId = Svc.Uid;
        await _db.Save(r);
        if (r.Type == "Weight") await SyncWeightAsync();
    }

    public async Task DeleteRecordAsync(BodyRecord r)
    {
        await _db.Delete(r);
        if (r.Type == "Weight") await SyncWeightAsync();
    }

    public async Task SyncWeightAsync()
    {
        var last = (await RecordsAsync("Weight")).LastOrDefault();
        if (last == null) return;
        Svc.User.WeightKg = last.Value;
        await Svc.Auth.SaveUserAsync();
    }

    public async Task<List<(string label, double value)>> WeightSeriesAsync(int days)
    {
        var from = DateTime.Today.AddDays(-days + 1);
        return (await RecordsAsync("Weight")).Where(r => r.Date.Date >= from)
            .Select(r => (r.Date.ToString("d MMM"), r.Value)).ToList();
    }

    public async Task<List<(string label, double value)>> WorkoutsPerWeekAsync(int weeks)
    {
        var hist = await Svc.Workouts.HistoryAsync();
        var res = new List<(string, double)>();
        var thisWeek = Dates.StartOfWeek(DateTime.Today);
        for (int i = weeks - 1; i >= 0; i--)
        {
            var start = thisWeek.AddDays(-7 * i);
            var end = start.AddDays(7);
            res.Add((start.ToString("d MMM"), hist.Count(s => s.StartedAt >= start && s.StartedAt < end)));
        }
        return res;
    }

    public async Task<List<(string label, double value)>> WorkoutsPerMonthAsync(int months)
    {
        var hist = await Svc.Workouts.HistoryAsync();
        var res = new List<(string, double)>();
        var thisMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        for (int i = months - 1; i >= 0; i--)
        {
            var start = thisMonth.AddMonths(-i);
            var end = start.AddMonths(1);
            res.Add((start.ToString("MMM"), hist.Count(s => s.StartedAt >= start && s.StartedAt < end)));
        }
        return res;
    }

    public async Task<List<MeasurementSummary>> MeasurementsAsync()
    {
        var all = (await RecordsAsync()).Where(r => r.Type != "Weight").ToList();
        return all.GroupBy(r => r.Type).Select(g =>
        {
            var ordered = g.OrderBy(r => r.Date).ToList();
            return new MeasurementSummary
            {
                Type = g.Key, Latest = ordered.Last().Value,
                Change = ordered.Last().Value - ordered.First().Value, Date = ordered.Last().Date
            };
        }).OrderBy(m => Array.IndexOf(BodyTypes, m.Type)).ToList();
    }

    public async Task<GoalSummary> GoalsAsync()
    {
        var u = Svc.User;
        var weights = await RecordsAsync("Weight");
        var g = new GoalSummary
        {
            CurrentWeight = u.WeightKg, TargetWeight = u.TargetWeightKg,
            StartWeight = u.StartWeightKg > 0 ? u.StartWeightKg : (weights.FirstOrDefault()?.Value ?? u.WeightKg),
            WeekGoal = u.WeeklyWorkoutGoal
        };
        if (u.TargetWeightKg > 0)
        {
            double span = g.StartWeight - g.TargetWeight;
            g.WeightProgress = Math.Abs(span) < 0.1
                ? (Math.Abs(g.CurrentWeight - g.TargetWeight) < 0.5 ? 1 : 0)
                : Math.Clamp((g.StartWeight - g.CurrentWeight) / span, 0, 1);
        }
        var ws = Dates.StartOfWeek(DateTime.Today);
        g.WeekWorkouts = (await Svc.Workouts.HistoryAsync()).Count(s => s.StartedAt >= ws);

        var water = await Svc.Habits.WaterByDayAsync(7);
        g.WaterDaysTracked = water.Count(kv => kv.Value > 0);
        g.WaterDaysHit = water.Count(kv => kv.Value >= u.WaterTargetMl);
        return g;
    }
}
