using FitSphere.Models;
using FitSphere.Services;
using FitSphere.UI;

namespace FitSphere.Pages;

/// <summary>Browse/search the library. When <c>onPick</c> is supplied the page acts as an exercise picker.</summary>
public class ExerciseLibraryPage : BasePage
{
    readonly Action<Exercise> _onPick;
    readonly Entry _search;
    readonly ChipGroup _group = new(new[] { "All" }.Concat(ExerciseService.MuscleGroups), false, new[] { "All" }, true);
    readonly ChipGroup _equip = new(new[] { "Any" }.Concat(ExerciseService.EquipmentTypes), false, new[] { "Any" }, true);
    readonly VerticalStackLayout _results = new() { Spacing = 10 };
    List<Exercise> _all = new();

    public ExerciseLibraryPage(Action<Exercise> onPick = null) : base(false)
    {
        _onPick = onPick;
        _search = new Entry { Placeholder = "Search exercises", PlaceholderColor = Ds.Muted, TextColor = Ds.Text, FontSize = 16, BackgroundColor = Colors.Transparent, VerticalOptions = LayoutOptions.Center };
        _search.TextChanged += (_, _) => Apply();
        _group.Changed += Apply;
        _equip.Changed += Apply;

        var box = new Border
        {
            BackgroundColor = Ds.Field, Stroke = Ds.Line, StrokeThickness = 1, StrokeShape = Ui.Round(Ds.ControlRadius),
            Padding = new Thickness(14, 0), HeightRequest = Ds.ControlHeight
        };
        var line = new Grid { ColumnSpacing = 10 };
        line.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        line.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        line.Add(Ui.Icon("ic_search", 20), 0, 0);
        line.Add(_search, 1, 0);
        box.Content = line;

        var col = Column(14);
        col.Add(Head(onPick == null ? "Exercise library" : "Choose exercise"));
        col.Add(box);
        col.Add(_group);
        col.Add(_equip);
        col.Add(_results);
        SetBody(new ScrollView { Content = col });
    }

    protected override async Task Refresh()
    {
        _all = await Svc.Exercises.AllAsync();
        Apply();
    }

    void Apply()
    {
        var q = (_search.Text ?? "").Trim().ToLowerInvariant();
        var list = _all.Where(e =>
            (_group.Selected == "All" || e.MuscleGroup == _group.Selected) &&
            (_equip.Selected == "Any" || e.Equipment == _equip.Selected) &&
            (q.Length == 0 || e.Name.ToLowerInvariant().Contains(q))).ToList();

        _results.Children.Clear();
        if (list.Count == 0)
        {
            _results.Add(Ui.Empty("ic_search", "No exercises found", "Try a different search or clear the filters."));
            return;
        }
        foreach (var ex in list)
        {
            var e = ex;
            var g = new Grid { ColumnSpacing = 14 };
            g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            g.Add(Ui.IconBadge("mg_" + e.MuscleGroup.ToLowerInvariant(), 52, Ds.Tint, 30), 0, 0);
            var t = Ui.Stack(2, Ui.Txt(e.Name, 15, true), Ui.Sub($"{e.MuscleGroup} · {e.Equipment} · {(e.IsCompound ? "Compound" : "Isolation")}", 12));
            t.VerticalOptions = LayoutOptions.Center;
            g.Add(t, 1, 0);
            g.Add(Ui.Icon(_onPick != null ? "ic_plus" : "ic_chevron", 18), 2, 0);
            _results.Add(Ui.Card(g, pad: 12).OnTap(async () =>
            {
                if (_onPick != null) { _onPick(e); await Navigation.PopAsync(); }
                else await Navigation.PushAsync(new ExerciseDetailPage(e));
            }));
        }
    }
}

public class ExerciseDetailPage : BasePage
{
    readonly Exercise _ex;
    readonly VerticalStackLayout _alts = new() { Spacing = 10 };

    public ExerciseDetailPage(Exercise ex) : base(false)
    {
        _ex = ex;
        var col = Column(18);
        col.Add(Head(ex.Name));

        var art = Ui.Icon("mg_" + ex.MuscleGroup.ToLowerInvariant(), 96);
        art.HorizontalOptions = LayoutOptions.Center;
        col.Add(Ui.Card(art, Ds.Tint, Ds.HeroRadius, 28, false));

        var tags = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
        foreach (var t in new[] { ex.MuscleGroup, ex.Equipment, ex.IsCompound ? "Compound" : "Isolation" })
        {
            var chip = Ui.Card(Ui.Txt(t, 13, true, Ds.Soft), Ds.Tint, Ds.ControlRadius, 8, false);
            chip.Padding = new Thickness(14, 8);
            chip.Margin = new Thickness(0, 0, 8, 8);
            tags.Add(chip);
        }
        col.Add(tags);

        col.Add(Ui.SectionHeader("How to do it"));
        var steps = new VerticalStackLayout { Spacing = 12 };
        int n = 1;
        foreach (var s in ex.Steps)
        {
            var g = new Grid { ColumnSpacing = 12 };
            g.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(26)));
            g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            g.Add(Ui.Txt((n++).ToString(), 15, true, Ds.Soft), 0, 0);
            g.Add(Ui.Txt(s, 15), 1, 0);
            steps.Add(g);
        }
        col.Add(Ui.Card(steps));

        col.Add(Ui.SectionHeader("Alternatives"));
        col.Add(_alts);
        SetBody(new ScrollView { Content = col });
    }

    protected override async Task Refresh()
    {
        var reps = await Svc.Exercises.ReplacementsAsync(_ex);
        _alts.Children.Clear();
        _alts.Add(Ui.Sub("Same muscle group, using the equipment in your profile.", 13));
        if (reps.Count == 0)
            _alts.Add(Ui.Card(Ui.Empty("ic_swap", "No alternatives", "Add more equipment in Profile > Edit profile to see more options.")));
        foreach (var r in reps) _alts.Add(ReplacementCard(r, () => Navigation.PushAsync(new ExerciseDetailPage(r.Ex))));
    }

    public static View ReplacementCard(Replacement r, Func<Task> tap) =>
        Ui.Card(Ui.Stack(4, Ui.Txt(r.Ex.Name, 15, true), Ui.Sub(r.Ex.Equipment, 12), Ui.Txt("Why: " + r.Reason, 13, false, Ds.Muted)), pad: 14).OnTap(tap);
}

/// <summary>Pick a replacement for an exercise; calls back with the chosen one.</summary>
public class ReplacementPage : BasePage
{
    readonly Exercise _src;
    readonly Action<Exercise> _onPick;
    readonly VerticalStackLayout _list = new() { Spacing = 10 };

    public ReplacementPage(Exercise src, Action<Exercise> onPick) : base(false)
    {
        _src = src; _onPick = onPick;
        var col = Column(16);
        col.Add(Head("Replace exercise"));
        col.Add(Ui.Sub($"Alternatives for {src.Name}, based on the muscle group and your equipment.", 14));
        col.Add(_list);
        SetBody(new ScrollView { Content = col });
    }

    protected override async Task Refresh()
    {
        var reps = await Svc.Exercises.ReplacementsAsync(_src, 8);
        _list.Children.Clear();
        if (reps.Count == 0) _list.Add(Ui.Card(Ui.Empty("ic_swap", "No matches", "No other exercises match your equipment for this muscle group.")));
        foreach (var r in reps)
        {
            var rep = r;
            _list.Add(ExerciseDetailPage.ReplacementCard(rep, async () => { _onPick(rep.Ex); await Navigation.PopAsync(); }));
        }
    }
}
