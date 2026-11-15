using FitSphere.Models;
using FitSphere.Services;
using FitSphere.UI;

namespace FitSphere.Pages;

public class HomePage : BasePage
{
    readonly VerticalStackLayout _col = Column(20);

    public HomePage() : base(true) => SetBody(new ScrollView { Content = _col });

    protected override async Task Refresh()
    {
        var u = Svc.User;
        var today = DateTime.Today;
        var plans = await Svc.Workouts.TodaysPlansAsync();
        var active = await Svc.Workouts.ActiveSessionAsync();
        var rest = await Svc.Habits.IsRestDayAsync(today);
        var tot = NutritionService.Totals(await Svc.Nutrition.DayAsync(today));
        int water = await Svc.Habits.WaterTodayAsync();
        var ws = Dates.StartOfWeek(today);
        var week = await Svc.Habits.StatusAsync(ws, ws.AddDays(6));
        var cons = await Svc.Habits.ConsistencyAsync();
        var insights = await Svc.Insights.BuildAsync();
        var items = plans.Count > 0 ? await Svc.Workouts.ItemsAsync(plans[0].Id) : new List<PlanItem>();
        var todayState = week[today];

        // daily goals
        var goals = new List<bool>();
        if (todayState == DayState.Completed || todayState == DayState.Scheduled) goals.Add(todayState == DayState.Completed);
        goals.Add(water >= u.WaterTargetMl);
        if (u.ProteinTarget > 0) goals.Add(tot.P >= u.ProteinTarget * 0.9);

        _col.Children.Clear();
        _col.Add(BuildHeader(u, goals));
        _col.Add(BuildHero(plans, items, active, rest, todayState));
        _col.Add(BuildQuickActions());
        _col.Add(BuildNutrition(u, tot));
        _col.Add(BuildWater(u, water));
        _col.Add(BuildWeek(ws, week, cons, u));
        if (insights.Count > 0) _col.Add(BuildInsight(insights[0]));
    }

    View BuildHeader(User u, List<bool> goals)
    {
        string first = (u.FullName ?? "").Split(' ')[0];
        var avatar = new Border
        {
            WidthRequest = 46, HeightRequest = 46, BackgroundColor = Ds.Mint, StrokeThickness = 0, StrokeShape = Ui.Round(23),
            Content = new Label { Text = first.Length > 0 ? first[..1].ToUpper() : "F", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Ds.Primary, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };
        var texts = Ui.Stack(2,
            Ui.Sub(DateTime.Now.ToString("dddd, d MMMM")),
            Ui.Txt($"{Ui.Greeting()}, {first}", 24, true),
            Ui.Sub($"{goals.Count(g => g)} of {goals.Count} daily goals done", 14));
        return Ui.LeftRight(texts, avatar);
    }

    View BuildHero(List<WorkoutPlan> plans, List<PlanItem> items, WorkoutSession active, bool rest, DayState state)
    {
        var s = new VerticalStackLayout { Spacing = 8 };
        s.Add(Ui.Txt("TODAY'S WORKOUT", 12, true, Ds.Lime));
        string title, sub = null, btn = null;
        Func<Task> tap = null;

        if (active != null)
        {
            title = "Workout in progress";
            sub = $"{active.PlanName}, started {active.StartedAt:HH:mm}";
            btn = "Resume workout";
            tap = () => Navigation.PushAsync(new ActiveWorkoutPage(active.Id));
        }
        else if (state == DayState.Completed)
        {
            title = "Workout completed";
            sub = "Great job. Recover well and keep your protein up.";
        }
        else if (rest)
        {
            title = "Rest day";
            sub = "Recovery is part of the plan.";
            btn = "Undo rest day";
            tap = async () => { await Svc.Habits.ToggleRestAsync(DateTime.Today); await Refresh(); };
        }
        else if (plans.Count > 0)
        {
            title = plans[0].Name + (plans.Count > 1 ? $"  +{plans.Count - 1} more" : "");
            var groups = string.Join(", ", items.Select(i => i.Ex.MuscleGroup).Distinct().Take(3));
            sub = $"{items.Count} exercises · ~{WorkoutService.EstimateMinutes(items)} min" + (groups.Length > 0 ? " · " + groups : "");
            btn = "Start workout";
            var plan = plans[0];
            tap = () => Flow.StartOrResume(Navigation, plan);
        }
        else
        {
            title = "No workout scheduled";
            sub = "Create a plan and choose your training days.";
            btn = "Create a plan";
            tap = () => Navigation.PushAsync(new PlanEditorPage());
        }

        s.Add(Ui.Txt(title, 24, true, Colors.White));
        if (sub != null) s.Add(Ui.Txt(sub, 14, false, Ds.Mint));
        if (btn != null)
        {
            var b = Ui.PrimaryBtn(btn, tap, Ds.Lime, Ds.Primary);
            b.Margin = new Thickness(0, 8, 0, 0);
            s.Add(b);
        }
        return Ui.Card(s, Ds.Primary, Ds.HeroRadius, 20);
    }

    View Tile(string icon, string label, Func<Task> tap)
    {
        var s = new VerticalStackLayout { Spacing = 8, HorizontalOptions = LayoutOptions.Center };
        var badge = Ui.IconBadge(icon, 44);
        badge.HorizontalOptions = LayoutOptions.Center;
        s.Add(badge);
        s.Add(Ui.Txt(label, 12, true, align: TextAlignment.Center));
        return Ui.Card(s, pad: 12).OnTap(tap);
    }

    View BuildQuickActions()
    {
        var g = Ui.Cols(4, 10);
        g.Add(Tile("ic_nutrition", "Log meal", () => Navigation.PushAsync(new AddFoodPage(Flow.DefaultMeal(), DateTime.Today))), 0, 0);
        g.Add(Tile("ic_scale", "Log weight", () => Navigation.PushAsync(new LogBodyPage())), 1, 0);
        g.Add(Tile("ic_water", "Habits", () => Navigation.PushAsync(new HabitsPage())), 2, 0);
        g.Add(Tile("ic_library", "Exercises", () => Navigation.PushAsync(new ExerciseLibraryPage())), 3, 0);
        return g;
    }

    View BuildNutrition(User u, Macros t)
    {
        double rem = u.CalorieTarget - t.Cal;
        bool over = rem < 0;
        var ring = Ui.RingView(u.CalorieTarget > 0 ? t.Cal / u.CalorieTarget : 0, $"{t.Cal:0}",
            over ? $"{-rem:0} over" : $"{rem:0} left", 128, over ? Ds.Danger : Ds.Primary);
        var macros = Ui.Stack(10,
            Widgets.MacroLine("Protein", t.P, u.ProteinTarget),
            Widgets.MacroLine("Carbs", t.C, u.CarbTarget),
            Widgets.MacroLine("Fat", t.F, u.FatTarget));
        macros.VerticalOptions = LayoutOptions.Center;
        var g = new Grid { ColumnSpacing = 18 };
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        g.Add(ring, 0, 0);
        g.Add(macros, 1, 0);
        return Ui.Stack(10,
            Ui.SectionHeader("Today's nutrition", "Add meal", () => Navigation.PushAsync(new AddFoodPage(Flow.DefaultMeal(), DateTime.Today))),
            Ui.Card(g, pad: 18));
    }

    View BuildWater(User u, int ml)
    {
        var s = new VerticalStackLayout { Spacing = 12 };
        s.Add(Ui.LeftRight(Ui.Row(10, Ui.IconBadge("ic_water", 36, Ds.Mint, 18), Ui.Txt("Water", 17, true)),
            Ui.Sub($"{ml / 1000.0:0.0} / {u.WaterTargetMl / 1000.0:0.0} L", 14)));
        s.Add(Ui.Bar(ml, u.WaterTargetMl));
        var row = Ui.Cols(2, 10);
        row.Add(Ui.SmallBtn("+ 250 ml", async () => { await Svc.Habits.AddWaterAsync(250); await Refresh(); }), 0, 0);
        row.Add(Ui.SmallBtn("+ 500 ml", async () => { await Svc.Habits.AddWaterAsync(500); await Refresh(); }), 1, 0);
        s.Add(row);
        return Ui.Card(s);
    }

    View BuildWeek(DateTime ws, Dictionary<DateTime, DayState> week, Consistency cons, User u)
    {
        var days = Ui.Cols(7, 4);
        for (int i = 0; i < 7; i++)
        {
            var d = ws.AddDays(i);
            var cell = new VerticalStackLayout { Spacing = 6, HorizontalOptions = LayoutOptions.Center };
            cell.Add(Ui.Txt(Dates.Days[i][..1], 11, true, Ds.Muted, TextAlignment.Center));
            cell.Add(Widgets.DayCircle(week[d], d == DateTime.Today, d.Day.ToString()));
            days.Add(cell, i, 0);
        }
        int done = week.Values.Count(v => v == DayState.Completed || v == DayState.Extra);
        string consText = cons.Percent.HasValue ? $"{cons.Percent}%" : "-";
        string consSub = cons.Percent.HasValue
            ? $"{cons.Completed} of {cons.Scheduled} scheduled workouts, last {cons.WindowDays} days"
            : "Schedule workouts in a plan to start tracking";
        var footer = Ui.LeftRight(
            Ui.Stack(2, Ui.Txt("Consistency score", 15, true), Ui.Sub(consSub, 12)),
            Ui.Txt(consText, 24, true, Ds.Primary));

        return Ui.Stack(10,
            Ui.LeftRight(Ui.H2("This week"), Ui.Sub($"{done} of {u.WeeklyWorkoutGoal} workouts", 14)),
            Ui.Card(Ui.Stack(16, days, new BoxView { HeightRequest = 1, Color = Ds.Line }, footer)));
    }

    View BuildInsight(Insight i)
    {
        var texts = Ui.Stack(4, Ui.Txt(i.Title, 15, true), Ui.Txt(i.Body, 13, false, Ds.Muted));
        var g = new Grid { ColumnSpacing = 14 };
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        var badge = Ui.IconBadge("ic_bulb", 40, Ds.Mint, 20);
        badge.VerticalOptions = LayoutOptions.Start;
        g.Add(badge, 0, 0);
        g.Add(texts, 1, 0);
        return Ui.Card(g);
    }
}
