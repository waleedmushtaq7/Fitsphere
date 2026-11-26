using FitSphere.Models;
using FitSphere.Services;
using FitSphere.UI;

namespace FitSphere.Pages;

public class HomePage : BasePage
{
    readonly VerticalStackLayout _col = Column(18);

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
        int streak = await Svc.Habits.StreakAsync();
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
        _col.Add(BuildCaloriesAndConsistency(u, tot, cons, streak));
        _col.Add(BuildMacros(u, tot));
        _col.Add(BuildWater(u, water));
        _col.Add(BuildQuickActions());
        _col.Add(BuildWeek(ws, week, u));
        if (insights.Count > 0) _col.Add(BuildInsight(insights[0]));
    }

    // ---------------- header ----------------
    View BuildHeader(User u, List<bool> goals)
    {
        var bellIcon = Ui.Icon("ic_bell_w", 22);
        bellIcon.HorizontalOptions = LayoutOptions.Center;
        var bell = new Border
        {
            WidthRequest = 46, HeightRequest = 46, Background = Ds.Gradient(Ds.Blue, Ds.Primary, true), StrokeThickness = 0,
            StrokeShape = Ui.Round(23), Content = bellIcon, VerticalOptions = LayoutOptions.Center
        };
        bell.OnTap(() => Navigation.PushAsync(new HabitsPage()));

        var texts = Ui.Stack(2,
            Ui.Sub(Ui.Greeting(), 15),
            Ui.Txt(u.FullName, 28, true),
            Ui.Sub($"{DateTime.Now:dddd, d MMMM}  ·  {goals.Count(g => g)} of {goals.Count} goals done", 13));
        return Ui.LeftRight(texts, bell);
    }

    // ---------------- hero (gradient) ----------------
    View BuildHero(List<WorkoutPlan> plans, List<PlanItem> items, WorkoutSession active, bool rest, DayState state)
    {
        string title, meta = null, btn = null;
        bool showClock = false;
        Func<Task> tap = null;

        if (active != null)
        {
            title = "Workout in progress";
            meta = $"{active.PlanName}, started {active.StartedAt:HH:mm}";
            btn = "Resume";
            tap = () => Navigation.PushAsync(new ActiveWorkoutPage(active.Id));
        }
        else if (state == DayState.Completed)
        {
            title = "Workout completed";
            meta = "Great job. Recover well and keep your protein up.";
        }
        else if (rest)
        {
            title = "Rest day";
            meta = "Recovery is part of the plan.";
            btn = "Undo";
            tap = async () => { await Svc.Habits.ToggleRestAsync(DateTime.Today); await Refresh(); };
        }
        else if (plans.Count > 0)
        {
            title = plans[0].Name + (plans.Count > 1 ? $"  +{plans.Count - 1}" : "");
            meta = $"{WorkoutService.EstimateMinutes(items)} min · {items.Count} {(items.Count == 1 ? "exercise" : "exercises")}";
            showClock = true;
            btn = "Start";
            var plan = plans[0];
            tap = () => Flow.StartOrResume(Navigation, plan);
        }
        else
        {
            title = "No workout scheduled";
            meta = "Create a plan and choose your training days.";
            btn = "Create";
            tap = () => Navigation.PushAsync(new PlanEditorPage());
        }

        var badge = new Border
        {
            BackgroundColor = Color.FromRgba(1f, 1f, 1f, 0.22f), StrokeThickness = 0, StrokeShape = Ui.Round(Ds.ControlRadius),
            Padding = new Thickness(12, 5),
            Content = Ui.Txt(DateTime.Today.ToString("ddd"), 12, true, Colors.White)
        };
        var top = Ui.LeftRight(Ui.Txt("Today's workout", 15, false, Ds.HeroSub), badge);

        var bottom = new Grid { ColumnSpacing = 12 };
        bottom.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        bottom.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        bottom.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        if (showClock) { var clock = Ui.Icon("ic_timer_w", 18); bottom.Add(clock, 0, 0); }
        var metaLabel = Ui.Txt(meta, 14, false, Colors.White);
        metaLabel.VerticalOptions = LayoutOptions.Center;
        bottom.Add(metaLabel, 1, 0);
        if (btn != null)
        {
            var b = Ui.PillBtn(btn, tap, Colors.White, Ds.Deep);
            b.VerticalOptions = LayoutOptions.Center;
            bottom.Add(b, 2, 0);
        }

        var s = Ui.Stack(12, top, Ui.Txt(title, 26, true, Colors.White), bottom);
        return Ui.Card(s, null, Ds.HeroRadius, 20, false, Ds.HeroBrush);
    }

    // ---------------- calories ring + consistency ----------------
    View BuildCaloriesAndConsistency(User u, Macros t, Consistency cons, int streak)
    {
        bool over = u.CalorieTarget > 0 && t.Cal > u.CalorieTarget;
        var ring = Ui.RingView(u.CalorieTarget > 0 ? t.Cal / u.CalorieTarget : 0, $"{t.Cal:N0}", $"of {u.CalorieTarget:N0}", 118,
            over ? Ds.Magenta : Ds.Amber, over ? Ds.Rose : Ds.Orange);
        ring.HorizontalOptions = LayoutOptions.Center;
        var calCard = Ui.Card(Ui.Stack(12, Ui.Txt("Calories", 16, true), ring), pad: 16)
            .OnTap(() => Navigation.PushAsync(new AddFoodPage(Flow.DefaultMeal(), DateTime.Today)));

        string pct = cons.Percent.HasValue ? $"{cons.Percent}%" : "-";
        var flame = Ui.Icon("ic_flame_a", 18);
        string streakText = streak > 0 ? $"{streak} workout streak" : "No streak yet";
        var streakRow = Ui.Row(6, flame, Ui.Txt(streakText, 13, true, Ds.Amber));
        var consStack = Ui.Stack(10,
            Ui.Txt("Consistency", 16, true),
            Ui.Txt(pct, 36, true, Ds.Emerald),
            Ui.GradBar(cons.Percent ?? 0, 100, Ds.Lime, Ds.Emerald, 8),
            streakRow);
        if (!cons.Percent.HasValue) consStack.Add(Ui.Sub("Schedule workouts to start tracking", 12));
        var consCard = Ui.Card(consStack, pad: 16);

        var g = Ui.Cols(2, 12);
        g.Add(calCard, 0, 0);
        g.Add(consCard, 1, 0);
        return g;
    }

    // ---------------- macros ----------------
    View BuildMacros(User u, Macros t) =>
        Ui.Card(Ui.Stack(14,
            Ui.Txt("Macros", 16, true),
            Widgets.MacroRow("Protein", t.P, u.ProteinTarget),
            Widgets.MacroRow("Carbs", t.C, u.CarbTarget),
            Widgets.MacroRow("Fats", t.F, u.FatTarget)), pad: 18);

    // ---------------- water ----------------
    View BuildWater(User u, int ml)
    {
        var dropIcon = Ui.Icon("ic_water_w", 26);
        dropIcon.HorizontalOptions = LayoutOptions.Center;
        var tile = new Border
        {
            WidthRequest = 54, HeightRequest = 54, Background = Ds.Gradient(Ds.Cyan, Ds.Blue, true), StrokeThickness = 0,
            StrokeShape = Ui.Round(Ds.ControlRadius), Content = dropIcon, VerticalOptions = LayoutOptions.Center
        };
        var texts = Ui.Stack(2, Ui.Txt("Water intake", 17, true), Ui.Sub($"{ml / 1000.0:0.0} of {u.WaterTargetMl / 1000.0:0.0} L", 14));
        texts.VerticalOptions = LayoutOptions.Center;
        var add = Ui.PillBtn("+250 ml", async () => { await Svc.Habits.AddWaterAsync(250); await Refresh(); }, Colors.Transparent, Ds.Cyan, Ds.Cyan);
        add.VerticalOptions = LayoutOptions.Center;

        var g = new Grid { ColumnSpacing = 14 };
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        g.Add(tile, 0, 0);
        g.Add(texts, 1, 0);
        g.Add(add, 2, 0);
        return Ui.Card(g, pad: 14).OnTap(() => Navigation.PushAsync(new HabitsPage()));
    }

    // ---------------- quick actions ----------------
    View Tile(string icon, string label, Func<Task> tap)
    {
        var badge = Ui.IconBadge(icon, 44);
        badge.HorizontalOptions = LayoutOptions.Center;
        var s = new VerticalStackLayout { Spacing = 8, HorizontalOptions = LayoutOptions.Center };
        s.Add(badge);
        s.Add(Ui.Txt(label, 12, true, Ds.Muted, TextAlignment.Center));
        return Ui.Card(s, pad: 12).OnTap(tap);
    }

    View BuildQuickActions()
    {
        var g = Ui.Cols(4, 10);
        g.Add(Tile("ic_nutrition", "Meal", () => Navigation.PushAsync(new AddFoodPage(Flow.DefaultMeal(), DateTime.Today))), 0, 0);
        g.Add(Tile("ic_scale", "Weight", () => Navigation.PushAsync(new LogBodyPage())), 1, 0);
        g.Add(Tile("ic_water", "Habits", () => Navigation.PushAsync(new HabitsPage())), 2, 0);
        g.Add(Tile("ic_library", "Library", () => Navigation.PushAsync(new ExerciseLibraryPage())), 3, 0);
        return g;
    }

    // ---------------- week ----------------
    View BuildWeek(DateTime ws, Dictionary<DateTime, DayState> week, User u)
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
        return Ui.Card(Ui.Stack(14,
            Ui.LeftRight(Ui.Txt("This week", 16, true), Ui.Sub($"{done} of {u.WeeklyWorkoutGoal} workouts", 13)),
            days));
    }

    // ---------------- insight ----------------
    View BuildInsight(Insight i)
    {
        Color accent = i.Tone == "Warn" ? Ds.Amber : i.Tone == "Good" ? Ds.Emerald : Ds.Soft;
        var texts = Ui.Stack(4, Ui.Txt(i.Title, 15, true, accent), Ui.Txt(i.Body, 13, false, Ds.Muted));
        var g = new Grid { ColumnSpacing = 14 };
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        var badge = Ui.IconBadge("ic_bulb", 40, Ds.Tint, 20);
        badge.VerticalOptions = LayoutOptions.Start;
        g.Add(badge, 0, 0);
        g.Add(texts, 1, 0);
        return Ui.Card(g);
    }
}
