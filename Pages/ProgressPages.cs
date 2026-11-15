using FitSphere.Models;
using FitSphere.Services;
using FitSphere.UI;

namespace FitSphere.Pages;

public class ProgressPage : BasePage
{
    readonly VerticalStackLayout _col = Column(20);
    readonly ChipGroup _range = new(new[] { "30 days", "90 days", "1 year" }, false, new[] { "30 days" });
    readonly ChipGroup _period = new(new[] { "Weekly", "Monthly" }, false, new[] { "Weekly" });

    public ProgressPage() : base(true)
    {
        _range.Changed += async () => await Busy(Refresh);
        _period.Changed += async () => await Busy(Refresh);
        SetBody(new ScrollView { Content = _col });
    }

    protected override async Task Refresh()
    {
        var goals = await Svc.Progress.GoalsAsync();
        var cons = await Svc.Habits.ConsistencyAsync();
        int days = _range.Selected == "90 days" ? 90 : _range.Selected == "1 year" ? 365 : 30;
        var weights = await Svc.Progress.WeightSeriesAsync(days);
        var bars = _period.Selected == "Monthly" ? await Svc.Progress.WorkoutsPerMonthAsync(6) : await Svc.Progress.WorkoutsPerWeekAsync(8);
        var measures = await Svc.Progress.MeasurementsAsync();
        var records = (await Svc.Progress.RecordsAsync()).OrderByDescending(r => r.Date).Take(6).ToList();
        var insights = await Svc.Insights.BuildAsync();
        var hist = await Svc.Workouts.HistoryAsync();
        int monthWorkouts = hist.Count(s => s.StartedAt >= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
        var allWeights = await Svc.Progress.RecordsAsync("Weight");
        var recent = allWeights.Where(r => r.Date.Date >= DateTime.Today.AddDays(-30)).ToList();
        double? change = recent.Count >= 2 ? recent.Last().Value - recent.First().Value : null;

        _col.Children.Clear();
        _col.Add(Head("Progress", false, Ui.SmallBtn("+ Log", () => Navigation.PushAsync(new LogBodyPage()))));

        // ---- overview ----
        var ov = Ui.Cols(3, 8);
        ov.Add(Stat(cons.Percent.HasValue ? $"{cons.Percent}%" : "-", "Consistency"), 0, 0);
        ov.Add(Stat(monthWorkouts.ToString(), "Workouts this month"), 1, 0);
        ov.Add(Stat(change.HasValue ? $"{change:+0.0;-0.0;0.0} kg" : "-", "Weight, 30 days"), 2, 0);
        _col.Add(Ui.Card(ov, Ds.Primary, Ds.HeroRadius, 20));

        // ---- goals ----
        var g = new VerticalStackLayout { Spacing = 16 };
        if (goals.WeightProgress.HasValue)
            g.Add(GoalRow("Weight goal", $"{Num.F(goals.CurrentWeight)} → {Num.F(goals.TargetWeight)} kg", goals.WeightProgress.Value, 1));
        else
            g.Add(Ui.Sub("Set a target weight in Profile > Goals & targets to track it here.", 13));
        g.Add(GoalRow("Workouts this week", $"{goals.WeekWorkouts} of {goals.WeekGoal}", goals.WeekWorkouts, goals.WeekGoal));
        g.Add(GoalRow("Water goal hit (last 7 days)", $"{goals.WaterDaysHit} of 7 days", goals.WaterDaysHit, 7));
        _col.Add(Ui.Stack(10, Ui.SectionHeader("Goals"), Ui.Card(g)));

        // ---- weight chart ----
        var wBox = new VerticalStackLayout { Spacing = 12 };
        wBox.Add(_range);
        if (weights.Count >= 2) wBox.Add(Ui.LineChart(weights, "kg"));
        else wBox.Add(Ui.Empty("ic_scale", "Not enough data", "Log your weight at least twice to see a trend.", "Log weight", () => Navigation.PushAsync(new LogBodyPage())));
        _col.Add(Ui.Stack(10, Ui.SectionHeader("Body weight"), Ui.Card(wBox)));

        // ---- workouts chart ----
        var bBox = new VerticalStackLayout { Spacing = 12 };
        bBox.Add(_period);
        bBox.Add(bars.Any(b => b.value > 0) ? Ui.BarChart(bars) : Ui.Empty("ic_workouts", "No workouts yet", "Completed workouts will be charted here."));
        _col.Add(Ui.Stack(10, Ui.SectionHeader("Workouts completed"), Ui.Card(bBox)));

        // ---- measurements ----
        _col.Add(Ui.SectionHeader("Measurements", "Add", () => Navigation.PushAsync(new LogBodyPage(null, "Waist"))));
        if (measures.Count == 0)
            _col.Add(Ui.Card(Ui.Empty("ic_target", "No measurements", "Track chest, waist, hips, arms or thighs over time.")));
        else
        {
            var rows = new VerticalStackLayout { Spacing = 14 };
            foreach (var m in measures)
            {
                string ch = Math.Abs(m.Change) < 0.05 ? "no change" : $"{m.Change:+0.0;-0.0} cm";
                rows.Add(Ui.LeftRight(Ui.Stack(2, Ui.Txt(m.Type, 15, true), Ui.Sub($"{ch} since first record", 12)), Ui.Txt($"{Num.F(m.Latest)} cm", 16, true, Ds.Primary)));
            }
            _col.Add(Ui.Card(rows));
        }

        // ---- recent records (edit / delete) ----
        if (records.Count > 0)
        {
            _col.Add(Ui.SectionHeader("Recent entries"));
            var rows = new VerticalStackLayout { Spacing = 14 };
            foreach (var r in records)
            {
                var rec = r;
                string val = rec.Type == "Weight" ? $"{Num.F(rec.Value)} kg" : $"{Num.F(rec.Value)} cm";
                rows.Add(Ui.LeftRight(Ui.Stack(2, Ui.Txt(rec.Type, 15, true), Ui.Sub(rec.Date.ToString("d MMM yyyy"), 12)),
                    Ui.Txt(val, 15, true)).OnTap(() => Navigation.PushAsync(new LogBodyPage(rec))));
            }
            _col.Add(Ui.Card(rows));
            _col.Add(Ui.Sub("Tap an entry to edit or delete it.", 12));
        }

        // ---- insights ----
        _col.Add(Ui.SectionHeader("Insights"));
        if (insights.Count == 0)
            _col.Add(Ui.Card(Ui.Empty("ic_bulb", "Insights need data", "Log workouts, meals and weight for a few days. Every insight shows the numbers behind it.")));
        foreach (var i in insights)
        {
            Color c = i.Tone == "Warn" ? Ds.Danger : i.Tone == "Good" ? Ds.Primary : Ds.Muted;
            _col.Add(Ui.Card(Ui.Stack(4, Ui.Txt(i.Title, 15, true, c), Ui.Txt(i.Body, 13, false, Ds.Text))));
        }
    }

    static View Stat(string big, string small) =>
        Ui.Stack(2, Ui.Txt(big, 22, true, Colors.White), Ui.Txt(small, 11, false, Ds.Mint));

    static View GoalRow(string title, string value, double progress, double max) =>
        Ui.Stack(6, Ui.LeftRight(Ui.Txt(title, 14, true), Ui.Sub(value, 13)), Ui.Bar(progress, max));
}

/// <summary>Add, edit or delete a body weight / measurement entry.</summary>
public class LogBodyPage : BasePage
{
    readonly BodyRecord _record;
    readonly ChipGroup _type;
    readonly FormInput _value = new("Value", "kg", Keyboard.Numeric);

    public LogBodyPage(BodyRecord record = null, string type = "Weight") : base(false)
    {
        _record = record;
        _type = new ChipGroup(ProgressService.BodyTypes, false, new[] { record?.Type ?? type });
        if (record != null) _value.Text = Num.F(record.Value);
        _type.Changed += UpdateUnit;
        UpdateUnit();

        var col = Column(18);
        col.Add(Head(record == null ? "Log measurement" : "Edit entry", true, record != null ? Ui.IconButton("ic_trash", DeleteAsync) : null));
        col.Add(Ui.Sub(record == null ? "Recorded for today." : "Recorded on " + record.Date.ToString("d MMM yyyy"), 14));
        col.Add(Ui.Stack(8, Ui.Txt("What are you recording?", 13, true, Ds.Muted), _type));
        col.Add(_value);
        col.Add(Ui.PrimaryBtn("Save", SaveAsync));
        SetBody(new ScrollView { Content = col });
    }

    void UpdateUnit() => _value.Entry.Placeholder = _type.Selected == "Weight" ? "Weight in kg" : "Size in cm";

    async Task SaveAsync()
    {
        bool weight = _type.Selected == "Weight";
        if (!_value.Validate(weight ? 20 : 5, weight ? 400 : 300, weight ? "Weight" : "Measurement", out var v)) return;

        var r = _record ?? new BodyRecord { Date = DateTime.Now };
        bool typeChanged = _record != null && _record.Type != _type.Selected;
        var oldType = _record?.Type;
        r.Type = _type.Selected; r.Value = v;
        await Busy(async () =>
        {
            await Svc.Progress.SaveRecordAsync(r);
            if (typeChanged && oldType == "Weight") await Svc.Progress.SyncWeightAsync(); // keep profile weight in sync
        });
        Ui.Toast("Saved");
        await Navigation.PopAsync();
    }

    async Task DeleteAsync()
    {
        if (!await Ui.Confirm("Delete entry", "Remove this record permanently?")) return;
        await Busy(() => Svc.Progress.DeleteRecordAsync(_record));
        await Navigation.PopAsync();
    }
}
