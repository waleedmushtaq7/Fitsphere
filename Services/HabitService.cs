using FitSphere.Data;
using FitSphere.Models;

namespace FitSphere.Services;

public enum DayState { None, Scheduled, Completed, Missed, Rest, Extra }

public class Consistency
{
    public int? Percent { get; set; }
    public int Completed { get; set; }
    public int Scheduled { get; set; }
    public int WindowDays { get; set; }
}

public class HabitService
{
    readonly AppDatabase _db;
    public HabitService(AppDatabase db) => _db = db;

    // ---------- Water ----------
    public Task AddWaterAsync(int ml) =>
        _db.Save(new WaterLog { UserId = Svc.Uid, Date = DateTime.Now, Ml = ml });

    public async Task UndoWaterAsync()
    {
        var last = (await _db.ForUser<WaterLog>(Svc.Uid)).Where(w => w.Date.Date == DateTime.Today).OrderByDescending(w => w.Id).FirstOrDefault();
        if (last != null) await _db.Delete(last);
    }

    public async Task<int> WaterTodayAsync() =>
        (await _db.ForUser<WaterLog>(Svc.Uid)).Where(w => w.Date.Date == DateTime.Today).Sum(w => w.Ml);

    /// <summary>Total ml per day for the last <paramref name="days"/> days (oldest first, zeros included).</summary>
    public async Task<Dictionary<DateTime, int>> WaterByDayAsync(int days)
    {
        var logs = await _db.ForUser<WaterLog>(Svc.Uid);
        var res = new Dictionary<DateTime, int>();
        for (int i = days - 1; i >= 0; i--)
        {
            var d = DateTime.Today.AddDays(-i);
            res[d] = logs.Where(w => w.Date.Date == d).Sum(w => w.Ml);
        }
        return res;
    }

    // ---------- Rest days ----------
    public async Task<bool> IsRestDayAsync(DateTime d) =>
        (await _db.ForUser<RestDay>(Svc.Uid)).Any(r => r.Date.Date == d.Date);

    /// <summary>Toggles a rest day and returns the new state (true = rest day).</summary>
    public async Task<bool> ToggleRestAsync(DateTime d)
    {
        var existing = (await _db.ForUser<RestDay>(Svc.Uid)).FirstOrDefault(r => r.Date.Date == d.Date);
        if (existing != null) { await _db.Delete(existing); await Svc.Reminders.RescheduleAsync(); return false; }
        await _db.Save(new RestDay { UserId = Svc.Uid, Date = d.Date });
        return true;
    }

    // ---------- Schedule & consistency ----------
    public async Task<Dictionary<DateTime, DayState>> StatusAsync(DateTime from, DateTime to)
    {
        var plans = await _db.ForUser<WorkoutPlan>(Svc.Uid);
        var sessions = (await _db.ForUser<WorkoutSession>(Svc.Uid)).Where(s => s.Completed).ToList();
        var rests = (await _db.ForUser<RestDay>(Svc.Uid)).Select(r => r.Date.Date).ToHashSet();
        var done = sessions.Select(s => s.StartedAt.Date).ToHashSet();
        var today = DateTime.Today;
        var res = new Dictionary<DateTime, DayState>();

        for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
        {
            bool planned = plans.Any(p => p.CreatedAt.Date <= d && p.HasDay(d.DayOfWeek));
            bool did = done.Contains(d);
            DayState st;
            if (rests.Contains(d) && !did) st = DayState.Rest;
            else if (planned) st = did ? DayState.Completed : (d < today ? DayState.Missed : DayState.Scheduled);
            else st = did ? DayState.Extra : DayState.None;
            res[d] = st;
        }
        return res;
    }

    /// <summary>
    /// Consistency = completed scheduled workouts / scheduled workouts that are already due,
    /// over the last 28 days (or since the account was created). Rest days and today's pending workout are excluded.
    /// </summary>
    public async Task<Consistency> ConsistencyAsync()
    {
        var today = DateTime.Today;
        var from = new[] { today.AddDays(-27), Svc.User.CreatedAt.Date }.Max();
        var st = await StatusAsync(from, today);
        int completed = st.Values.Count(v => v == DayState.Completed);
        int missed = st.Values.Count(v => v == DayState.Missed);
        int sched = completed + missed;
        return new Consistency
        {
            Completed = completed, Scheduled = sched, WindowDays = (today - from).Days + 1,
            Percent = sched == 0 ? null : (int)Math.Round(100.0 * completed / sched)
        };
    }
}
