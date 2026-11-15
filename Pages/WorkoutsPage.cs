using FitSphere.Models;
using FitSphere.Services;
using FitSphere.UI;

namespace FitSphere.Pages;

public class WorkoutsPage : BasePage
{
    readonly VerticalStackLayout _col = Column(20);

    public WorkoutsPage() : base(true) => SetBody(new ScrollView { Content = _col });

    protected override async Task Refresh()
    {
        var plans = await Svc.Workouts.PlansAsync();
        var ws = Dates.StartOfWeek(DateTime.Today);
        var week = await Svc.Habits.StatusAsync(ws, ws.AddDays(6));
        var suggestions = await Svc.Insights.PlanSuggestionsAsync();
        var counts = new Dictionary<int, int>();
        foreach (var p in plans) counts[p.Id] = (await Svc.Workouts.ItemsAsync(p.Id)).Count;

        _col.Children.Clear();
        _col.Add(Head("Workouts", false, Ui.SmallBtn("+ New plan", () => Navigation.PushAsync(new PlanEditorPage()))));

        var quick = Ui.Cols(2, 12);
        quick.Add(Ui.MenuRow("ic_library", "Library", "Exercises", () => Navigation.PushAsync(new ExerciseLibraryPage())), 0, 0);
        quick.Add(Ui.MenuRow("ic_trophy", "History", "Records", () => Navigation.PushAsync(new HistoryPage())), 1, 0);
        _col.Add(quick);

        // ---- My plans ----
        _col.Add(Ui.SectionHeader("My plans"));
        if (plans.Count == 0)
            _col.Add(Ui.Card(Ui.Empty("ic_workouts", "No plans yet", "Create a plan, choose your training days and add exercises.", "Create a plan", () => Navigation.PushAsync(new PlanEditorPage()))));
        foreach (var plan in plans)
        {
            var p = plan;
            var info = Ui.Stack(3, Ui.Txt(p.Name, 17, true),
                Ui.Sub($"{counts[p.Id]} exercises · {(p.DayList.Count > 0 ? string.Join(" ", p.DayList) : "No days")}"));
            var actions = Ui.Row(8, Ui.SmallBtn("Start", () => Flow.StartOrResume(Navigation, p), Ds.Primary, Colors.White),
                Ui.IconButton("ic_edit", () => Navigation.PushAsync(new PlanEditorPage(p.Id)), 38));
            _col.Add(Ui.Card(Ui.LeftRight(info, actions)).OnTap(() => Navigation.PushAsync(new PlanEditorPage(p.Id))));
        }

        // ---- Schedule ----
        _col.Add(Ui.SectionHeader("This week's schedule"));
        var sched = new VerticalStackLayout { Spacing = 12 };
        for (int i = 0; i < 7; i++)
        {
            var d = ws.AddDays(i);
            var names = plans.Where(p => p.HasDay(d.DayOfWeek)).Select(p => p.Name).ToList();
            var st = week[d];
            string tag = st switch { DayState.Completed => "Done", DayState.Missed => "Missed", DayState.Rest => "Rest day", DayState.Scheduled => "Planned", DayState.Extra => "Bonus", _ => "" };
            Color tagColor = st == DayState.Missed ? Ds.Danger : st == DayState.Completed ? Ds.Primary : Ds.Muted;
            var day = Ui.Txt(Dates.Days[i], 14, true, d == DateTime.Today ? Ds.Primary : Ds.Text);
            day.WidthRequest = 40;
            var line = new Grid { ColumnSpacing = 8 };
            line.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(40)));
            line.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            line.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            line.Add(day, 0, 0);
            line.Add(Ui.Txt(names.Count > 0 ? string.Join(", ", names) : "Free day", 14, false, names.Count > 0 ? Ds.Text : Ds.Muted), 1, 0);
            line.Add(Ui.Txt(tag, 12, true, tagColor), 2, 0);
            sched.Add(line);
        }
        _col.Add(Ui.Card(sched));

        // ---- Smart suggestions ----
        _col.Add(Ui.SectionHeader("Progression suggestions"));
        if (suggestions.Count == 0)
            _col.Add(Ui.Card(Ui.Empty("ic_bulb", "No suggestions yet", "Complete a workout and I'll recommend weight or rep changes based on how it went.")));
        else
            foreach (var sg in suggestions)
            {
                var tagColor = sg.Kind == "Increase" ? Ds.Primary : sg.Kind == "Reduce" ? Ds.Danger : Ds.Muted;
                _col.Add(Ui.Card(Ui.Stack(6,
                    Ui.LeftRight(Ui.Txt(sg.ExerciseName, 15, true), Ui.Txt(sg.Headline, 14, true, tagColor)),
                    Ui.Txt("Why: " + sg.Reason, 13, false, Ds.Muted))));
            }
    }
}

public class PlanEditorPage : BasePage
{
    readonly int _id;
    readonly WorkoutPlan _plan = new();
    readonly List<PlanItem> _items = new();
    readonly FormInput _name = new("Plan name", "e.g. Push day");
    readonly ChipGroup _days = new(Dates.Days, true);
    readonly VerticalStackLayout _list = new() { Spacing = 12 };
    readonly Label _daysError = Ui.Txt("", 12, false, Ds.Danger);
    bool _loaded;

    public PlanEditorPage(int planId = 0) : base(false)
    {
        _id = planId;
        _daysError.IsVisible = false;
        var col = Column(18);
        View trailing = planId > 0 ? Ui.IconButton("ic_trash", DeleteAsync) : null;
        col.Add(Head(planId > 0 ? "Edit plan" : "New plan", true, trailing));
        col.Add(_name);
        col.Add(Ui.Stack(8, Ui.Txt("Training days", 13, true, Ds.Muted), _days, _daysError));
        col.Add(Ui.SectionHeader("Exercises"));
        col.Add(_list);
        col.Add(Ui.SoftBtn("+ Add exercise", () => Navigation.PushAsync(new ExerciseLibraryPage(OnPicked))));
        col.Add(Ui.PrimaryBtn("Save plan", SaveAsync));
        SetBody(new ScrollView { Content = col });
    }

    protected override async Task Refresh()
    {
        if (!_loaded)
        {
            _loaded = true;
            if (_id > 0)
            {
                var p = (await Svc.Workouts.PlansAsync()).FirstOrDefault(x => x.Id == _id);
                if (p != null)
                {
                    _plan.Id = p.Id; _plan.Name = p.Name; _plan.Days = p.Days; _plan.CreatedAt = p.CreatedAt; _plan.UserId = p.UserId;
                    _name.Text = p.Name;
                    _days.SetSelection(p.DayList);
                    _items.AddRange(await Svc.Workouts.ItemsAsync(_id));
                }
            }
        }
        RenderItems();
    }

    void OnPicked(Exercise ex)
    {
        if (_items.Any(i => i.Ex.Id == ex.Id)) { Ui.Toast("Already in this plan"); return; }
        _items.Add(new PlanItem
        {
            Ex = ex,
            Pe = new PlanExercise { ExerciseId = ex.Id, Sets = ex.MuscleGroup == "Cardio" ? 1 : 3, Reps = ex.MuscleGroup == "Cardio" ? 20 : 10, RestSeconds = ex.MuscleGroup == "Cardio" ? 0 : ex.IsCompound ? 90 : 60 }
        });
    }

    View Labeled(string label, View v) => Ui.Stack(4, Ui.Sub(label, 12), v);

    void RenderItems()
    {
        _list.Children.Clear();
        if (_items.Count == 0)
        {
            _list.Add(Ui.Card(Ui.Empty("ic_workouts", "No exercises yet", "Add exercises from the library, then set sets, reps and rest.")));
            return;
        }
        foreach (var it in _items.ToList())
        {
            var item = it;
            var head = Ui.LeftRight(
                Ui.Stack(2, Ui.Txt(item.Ex.Name, 16, true), Ui.Sub($"{item.Ex.MuscleGroup} · {item.Ex.Equipment}")),
                Ui.Row(8,
                    Ui.IconButton("ic_swap", () => Navigation.PushAsync(new ReplacementPage(item.Ex, ex =>
                    {
                        if (_items.Any(i => i.Ex.Id == ex.Id)) { Ui.Toast("Already in this plan"); return; }
                        item.Ex = ex; item.Pe.ExerciseId = ex.Id;
                    })), 36),
                    Ui.IconButton("ic_trash", () => { _items.Remove(item); RenderItems(); return Task.CompletedTask; }, 36)));

            var sets = new Counter(item.Pe.Sets, 1, 10); sets.Changed += v => item.Pe.Sets = v;
            var reps = new Counter(item.Pe.Reps, 1, 60); reps.Changed += v => item.Pe.Reps = v;
            var rest = new Counter(item.Pe.RestSeconds, 0, 300, 15, "s"); rest.Changed += v => item.Pe.RestSeconds = v;
            var counters = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
            foreach (var (label, view) in new (string, View)[] { ("Sets", sets), ("Reps", reps), ("Rest", rest) })
            {
                var l = Labeled(label, view);
                l.Margin = new Thickness(0, 0, 14, 4);
                counters.Add(l);
            }
            _list.Add(Ui.Card(Ui.Stack(12, head, counters)));
        }
    }

    async Task SaveAsync()
    {
        _name.SetError(null); _daysError.IsVisible = false;
        bool ok = true;
        if (string.IsNullOrWhiteSpace(_name.Text)) { _name.SetError("Give your plan a name"); ok = false; }
        if (_days.Selection.Count == 0) { _daysError.Text = "Select at least one training day"; _daysError.IsVisible = true; ok = false; }
        if (!ok) return;
        if (_items.Count == 0) { await Ui.Alert("No exercises", "Add at least one exercise to the plan."); return; }

        _plan.Name = _name.Text.Trim();
        _plan.Days = string.Join(",", Dates.Days.Where(d => _days.Selection.Contains(d)));
        await Busy(() => Svc.Workouts.SavePlanAsync(_plan, _items));
        Ui.Toast("Plan saved");
        await Navigation.PopAsync();
    }

    async Task DeleteAsync()
    {
        if (!await Ui.Confirm("Delete plan", $"Delete \"{_plan.Name}\"? Your past workout history is kept.")) return;
        await Busy(() => Svc.Workouts.DeletePlanAsync(_id));
        await Navigation.PopAsync();
    }
}
