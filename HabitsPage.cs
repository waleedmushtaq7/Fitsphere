using FitSphere.Models;
using FitSphere.Services;
using FitSphere.UI;

namespace FitSphere.Pages;

/// <summary>Water tracking, workout reminders, daily/weekly goals and rest-day management.</summary>
public class HabitsPage : BasePage
{
    readonly VerticalStackLayout _col = Column(20);

    public HabitsPage() : base(false) => SetBody(new ScrollView { Content = _col });

    static async void Safe(Func<Task> f)
    {
        try { await f(); } catch (Exception ex) { await Ui.Alert("Something went wrong", ex.Message); }
    }

    protected override async Task Refresh()
    {
        var u = Svc.User;
        int water = await Svc.Habits.WaterTodayAsync();
        var byDay = await Svc.Habits.WaterByDayAsync(7);
        var plans = await Svc.Workouts.PlansAsync();
        var status = await Svc.Habits.StatusAsync(DateTime.Today, DateTime.Today.AddDays(6));
        var cons = await Svc.Habits.ConsistencyAsync();

        _col.Children.Clear();
        _col.Add(Head("Habits"));

        // ---------- Water ----------
        var ring = Ui.RingView(u.WaterTargetMl > 0 ? (double)water / u.WaterTargetMl : 0, $"{water / 1000.0:0.0} L", $"of {u.WaterTargetMl / 1000.0:0.0} L", 120, Ds.Cyan, Ds.Blue);
        var btns = new VerticalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
        btns.Add(Ui.SmallBtn("+ 250 ml", async () => { await Svc.Habits.AddWaterAsync(250); await Refresh(); }));
        btns.Add(Ui.SmallBtn("+ 500 ml", async () => { await Svc.Habits.AddWaterAsync(500); await Refresh(); }));
        btns.Add(Ui.SmallBtn("+ Custom", AddCustomWater));
        btns.Add(Ui.LinkBtn("Undo last", async () => { await Svc.Habits.UndoWaterAsync(); await Refresh(); }, Ds.Muted));
        var top = new Grid { ColumnSpacing = 24 };
        top.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        top.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        top.Add(ring, 0, 0);
        top.Add(btns, 1, 0);
        var chart = Ui.BarChart(byDay.Select(kv => (kv.Key.ToString("ddd"), Math.Round(kv.Value / 1000.0, 1))).ToList(), 140);
        _col.Add(Ui.Stack(10, Ui.SectionHeader("Water today"),
            Ui.Card(Ui.Stack(16, top, Ui.Sub("Last 7 days (litres)", 12), chart))));

        // ---------- Goals ----------
        var wGoal = new Counter(u.WaterTargetMl, 500, 8000, 250, "");
        wGoal.Changed += v => Safe(async () => { u.WaterTargetMl = v; await Svc.Auth.SaveUserAsync(); });
        var weekly = new Counter(u.WeeklyWorkoutGoal, 1, 7);
        weekly.Changed += v => Safe(async () => { u.WeeklyWorkoutGoal = v; await Svc.Auth.SaveUserAsync(); });
        var goals = new VerticalStackLayout { Spacing = 16 };
        goals.Add(Ui.LeftRight(Ui.Stack(1, Ui.Txt("Daily water goal", 15, true), Ui.Sub("millilitres per day", 12)), wGoal));
        goals.Add(Ui.LeftRight(Ui.Stack(1, Ui.Txt("Weekly workout goal", 15, true), Ui.Sub("workouts per week", 12)), weekly));
        _col.Add(Ui.Stack(10, Ui.SectionHeader("Daily & weekly goals"), Ui.Card(goals)));

        // ---------- Reminders ----------
        var sw = new Switch { IsToggled = u.ReminderEnabled, OnColor = Ds.Primary, ThumbColor = Colors.White };
        sw.Toggled += (s, e) => Safe(async () =>
        {
            u.ReminderEnabled = e.Value;
            await Svc.Auth.SaveUserAsync();
            await Svc.Reminders.RescheduleAsync();
            if (e.Value && !u.NotificationsEnabled) Ui.Toast("Notifications are turned off in Profile");
        });
        TimeSpan.TryParse(u.ReminderTime, out var t);
        var tp = new TimePicker { Time = t == TimeSpan.Zero ? new TimeSpan(18, 0, 0) : t, Format = "HH:mm", TextColor = Ds.Text, FontSize = 16, HorizontalOptions = LayoutOptions.End };
        tp.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != TimePicker.TimeProperty.PropertyName) return;
            Safe(async () =>
            {
                u.ReminderTime = (tp.Time ?? new TimeSpan(18, 0, 0)).ToString(@"hh\:mm");
                await Svc.Auth.SaveUserAsync();
                await Svc.Reminders.RescheduleAsync();
            });
        };
        var rem = new VerticalStackLayout { Spacing = 14 };
        rem.Add(Ui.LeftRight(Ui.Stack(1, Ui.Txt("Workout reminders", 15, true), Ui.Sub("On days you have a workout planned", 12)), sw));
        rem.Add(Ui.LeftRight(Ui.Txt("Reminder time", 15), tp));
        if (!u.NotificationsEnabled) rem.Add(Ui.Txt("Notifications are turned off in Profile, so reminders won't be sent.", 12, false, Ds.Danger));
        _col.Add(Ui.Stack(10, Ui.SectionHeader("Reminders"), Ui.Card(rem)));

        // ---------- Schedule & rest days ----------
        var sched = new VerticalStackLayout { Spacing = 14 };
        foreach (var kv in status.OrderBy(k => k.Key))
        {
            var d = kv.Key;
            var names = plans.Where(p => p.CreatedAt.Date <= d && p.HasDay(d.DayOfWeek)).Select(p => p.Name).ToList();
            var left = Ui.Stack(1, Ui.Txt(Dates.Friendly(d), 15, true), Ui.Sub(names.Count > 0 ? string.Join(", ", names) : "No workout planned", 12));
            View right;
            if (names.Count > 0 && kv.Value != DayState.Completed)
            {
                var rest = new Switch { IsToggled = kv.Value == DayState.Rest, OnColor = Ds.Primary, ThumbColor = Colors.White };
                rest.Toggled += (s, e) => Safe(async () =>
                {
                    bool now = await Svc.Habits.IsRestDayAsync(d);
                    if (now != e.Value) await Svc.Habits.ToggleRestAsync(d);
                    await Svc.Reminders.RescheduleAsync();
                    await Refresh();
                });
                right = Ui.Row(8, Ui.Sub("Rest", 12), rest);
                ((HorizontalStackLayout)right).VerticalOptions = LayoutOptions.Center;
            }
            else right = Ui.Txt(kv.Value == DayState.Completed ? "Done" : "", 12, true, Ds.Soft);
            sched.Add(Ui.LeftRight(left, right));
        }
        _col.Add(Ui.Stack(10, Ui.SectionHeader("Next 7 days"), Ui.Card(sched),
            Ui.Sub("Marking a planned day as a rest day removes it from your consistency score.", 12)));

        // ---------- Consistency ----------
        string pct = cons.Percent.HasValue ? $"{cons.Percent}%" : "-";
        string how = cons.Percent.HasValue
            ? $"{cons.Completed} of {cons.Scheduled} scheduled workouts completed in the last {cons.WindowDays} days."
            : "Create a plan with training days. Your score appears after your first scheduled day passes.";
        _col.Add(Ui.Stack(10, Ui.SectionHeader("Consistency score"),
            Ui.Card(Ui.LeftRight(Ui.Txt(how, 14, false, Ds.Muted), Ui.Txt(pct, 28, true, Ds.Emerald)))));
    }

    async Task AddCustomWater()
    {
        var input = await DisplayPromptAsync("Add water", "Amount in ml", "Add", "Cancel", "e.g. 330", 5, Keyboard.Numeric);
        if (input == null) return;
        if (!Num.TryParse(input, out var ml) || ml < 50 || ml > 3000)
        {
            await Ui.Alert("Invalid amount", "Enter an amount between 50 and 3000 ml.");
            return;
        }
        await Svc.Habits.AddWaterAsync((int)ml);
        await Refresh();
    }
}
