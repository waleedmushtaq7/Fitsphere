using FitSphere.Models;
using FitSphere.Services;
using FitSphere.UI;

namespace FitSphere.Pages;

public class HistoryPage : BasePage
{
    readonly VerticalStackLayout _col = Column(18);

    public HistoryPage() : base(false) => SetBody(new ScrollView { Content = _col });

    protected override async Task Refresh()
    {
        var records = await Svc.Workouts.RecordsAsync();
        var history = await Svc.Workouts.HistoryAsync();

        _col.Children.Clear();
        _col.Add(Head("History & records"));

        _col.Add(Ui.SectionHeader("Personal records"));
        if (records.Count == 0)
            _col.Add(Ui.Card(Ui.Empty("ic_trophy", "No records yet", "Finish a workout and your best lifts will appear here.")));
        else
        {
            var rows = new VerticalStackLayout { Spacing = 14 };
            foreach (var r in records.Take(10))
            {
                string best = r.WeightKg > 0 ? $"{Num.F(r.WeightKg)} kg x {r.Reps}" : $"{r.Reps} reps";
                rows.Add(Ui.LeftRight(Ui.Stack(2, Ui.Txt(r.ExerciseName, 15, true), Ui.Sub(r.Date.ToString("d MMM yyyy"), 12)),
                    Ui.Txt(best, 15, true, Ds.Primary)));
            }
            _col.Add(Ui.Card(rows));
        }

        _col.Add(Ui.SectionHeader("Past workouts"));
        if (history.Count == 0)
            _col.Add(Ui.Card(Ui.Empty("ic_clock", "No workouts yet", "Completed sessions are listed here so you can review them.")));
        foreach (var h in history)
        {
            var s = h;
            var left = Ui.Stack(2, Ui.Txt(s.PlanName, 16, true), Ui.Sub(s.StartedAt.ToString("ddd, d MMM yyyy · HH:mm"), 12));
            var right = Ui.Stack(2, Ui.Txt($"{Math.Max(1, s.DurationSeconds / 60)} min", 14, true, align: TextAlignment.End),
                Ui.Sub($"{s.TotalVolume:0} kg", 12));
            right.HorizontalOptions = LayoutOptions.End;
            _col.Add(Ui.Card(Ui.LeftRight(left, right)).OnTap(() => Navigation.PushAsync(new SessionDetailPage(s))));
        }
    }
}

public class SessionDetailPage : BasePage
{
    readonly WorkoutSession _s;
    readonly VerticalStackLayout _col = Column(16);

    public SessionDetailPage(WorkoutSession s) : base(false)
    {
        _s = s;
        SetBody(new ScrollView { Content = _col });
    }

    protected override async Task Refresh()
    {
        var sets = await Svc.Workouts.SetsAsync(_s.Id);
        _col.Children.Clear();
        _col.Add(Head(_s.PlanName, true, Ui.IconButton("ic_trash", DeleteAsync)));

        var stats = Ui.Cols(3, 8);
        stats.Add(Stat("Date", _s.StartedAt.ToString("d MMM")), 0, 0);
        stats.Add(Stat("Time", $"{Math.Max(1, _s.DurationSeconds / 60)} min"), 1, 0);
        stats.Add(Stat("Volume", $"{_s.TotalVolume:0} kg"), 2, 0);
        _col.Add(Ui.Card(stats));

        foreach (var g in sets.GroupBy(x => x.ExerciseId))
        {
            var rows = new VerticalStackLayout { Spacing = 10 };
            rows.Add(Ui.Txt(g.First().ExerciseName, 16, true));
            foreach (var x in g)
            {
                string text = x.WeightKg > 0 ? $"{Num.F(x.WeightKg)} kg x {x.Reps}" : $"{x.Reps} reps";
                rows.Add(Ui.LeftRight(Ui.Txt($"Set {x.SetNumber}", 14, false, Ds.Muted),
                    Ui.Txt(x.Completed ? text : text + "  (skipped)", 14, x.Completed, x.Completed ? Ds.Text : Ds.Muted)));
            }
            _col.Add(Ui.Card(rows));
        }
    }

    static View Stat(string label, string value) => Ui.Stack(2, Ui.Sub(label, 12), Ui.Txt(value, 16, true));

    async Task DeleteAsync()
    {
        if (!await Ui.Confirm("Delete workout", "Remove this session from your history? This also affects your records.")) return;
        await Busy(() => Svc.Workouts.DeleteSessionAsync(_s.Id));
        await Navigation.PopAsync();
    }
}
