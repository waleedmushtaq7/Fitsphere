using FitSphere.Models;
using FitSphere.Services;
using FitSphere.UI;

namespace FitSphere.Pages;

/// <summary>
/// One row of the exercise list. Rows are recycled by the CollectionView, so only the rows on screen exist.
/// </summary>
class ExerciseRow : ContentView
{
    readonly Image _icon = Ui.Icon("mg_chest", 30);
    readonly Label _name = Ui.Txt("", 15, true);
    readonly Label _sub = Ui.Sub("", 12);
    readonly Func<Exercise, Task> _tap;

    public ExerciseRow(Func<Exercise, Task> tap, bool pickMode)
    {
        _tap = tap;
        _icon.HorizontalOptions = LayoutOptions.Center;
        var badge = new Border
        {
            WidthRequest = 52, HeightRequest = 52, BackgroundColor = Ds.Tint, StrokeThickness = 0,
            StrokeShape = Ui.Round(Ds.ControlRadius), Content = _icon
        };
        var texts = Ui.Stack(2, _name, _sub);
        texts.VerticalOptions = LayoutOptions.Center;

        var g = new Grid { ColumnSpacing = 14 };
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        g.Add(badge, 0, 0);
        g.Add(texts, 1, 0);
        g.Add(Ui.Icon(pickMode ? "ic_plus" : "ic_chevron", 18), 2, 0);

        Content = Ui.Card(g, pad: 12).OnTap(async () =>
        {
            if (BindingContext is Exercise e) await _tap(e);
        });
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (BindingContext is not Exercise e) return;
        _name.Text = e.Name;
        _sub.Text = $"{e.MuscleGroup} · {e.Equipment} · {(e.IsCompound ? "Compound" : "Isolation")}";
        _icon.Source = "mg_" + e.MuscleGroup.ToLowerInvariant() + ".png";
    }
}

/// <summary>Browse/search the library. When <c>onPick</c> is supplied the page acts as an exercise picker.</summary>
public class ExerciseLibraryPage : BasePage
{
    readonly Action<Exercise> _onPick;
    readonly Entry _search;
    readonly ChipGroup _group = new(new[] { "All" }.Concat(ExerciseService.MuscleGroups), false, new[] { "All" }, true);
    readonly ChipGroup _equip = new(new[] { "Any" }.Concat(ExerciseService.EquipmentTypes), false, new[] { "Any" }, true);
    readonly CollectionView _list;
    List<Exercise> _all = new();

    public ExerciseLibraryPage(Action<Exercise> onPick = null) : base(false)
    {
        _onPick = onPick;
        _search = new Entry
        {
            Placeholder = "Search exercises", PlaceholderColor = Ds.Muted, TextColor = Ds.Text, FontSize = 16,
            BackgroundColor = Colors.Transparent, VerticalOptions = LayoutOptions.Center
        };
        _search.TextChanged += (_, _) => Apply();
        _group.Changed += Apply;
        _equip.Changed += Apply;

        var line = new Grid { ColumnSpacing = 10 };
        line.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        line.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        line.Add(Ui.Icon("ic_search", 20), 0, 0);
        line.Add(_search, 1, 0);
        var box = new Border
        {
            BackgroundColor = Ds.Field, Stroke = Ds.Line, StrokeThickness = 1, StrokeShape = Ui.Round(Ds.ControlRadius),
            Padding = new Thickness(14, 0), HeightRequest = Ds.ControlHeight, Content = line
        };

        var top = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(Ds.PageMargin, 16, Ds.PageMargin, 6) };
        top.Add(Head(onPick == null ? "Exercise library" : "Choose exercise"));
        top.Add(box);
        top.Add(_group);
        top.Add(_equip);

        // CollectionView only creates the rows that are visible, so switching filters is instant.
        _list = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical) { ItemSpacing = 10 },
            ItemTemplate = new DataTemplate(() => new ExerciseRow(OnRowTap, onPick != null)),
            Margin = new Thickness(Ds.PageMargin, 0),
            Footer = new BoxView { HeightRequest = 24, Color = Colors.Transparent },
            EmptyView = Ui.Empty("ic_search", "No exercises found", "Try a different search or clear the filters.")
        };

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        root.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        root.Add(top, 0, 0);
        root.Add(_list, 0, 1);
        SetBody(root);
    }

    async Task OnRowTap(Exercise e)
    {
        if (_onPick != null)
        {
            _onPick(e);
            await Navigation.PopAsync();
        }
        else await Navigation.PushAsync(new ExerciseDetailPage(e));
    }

    protected override async Task Refresh()
    {
        if (_all.Count == 0) _all = await Svc.Exercises.AllAsync();
        Apply();
    }

    void Apply()
    {
        var q = (_search.Text ?? "").Trim().ToLowerInvariant();
        _list.ItemsSource = _all.Where(e =>
            (_group.Selected == "All" || e.MuscleGroup == _group.Selected) &&
            (_equip.Selected == "Any" || e.Equipment == _equip.Selected) &&
            (q.Length == 0 || e.Name.ToLowerInvariant().Contains(q))).ToList();
    }
}

public class ExerciseDetailPage : BasePage
{
    readonly Exercise _ex;
    readonly ExerciseAnimation _anim;
    readonly VerticalStackLayout _alts = new() { Spacing = 10 };

    public ExerciseDetailPage(Exercise ex) : base(false)
    {
        _ex = ex;
        _anim = new ExerciseAnimation(ex);

        var col = Column(18);
        col.Add(Head(ex.Name));
        col.Add(_anim);

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
        await _anim.LoadAsync();
        _anim.Start();

        var reps = await Svc.Exercises.ReplacementsAsync(_ex);
        _alts.Children.Clear();
        _alts.Add(Ui.Sub("Same muscle group, using the equipment in your profile.", 13));
        if (reps.Count == 0)
            _alts.Add(Ui.Card(Ui.Empty("ic_swap", "No alternatives", "Add more equipment in Profile > Edit profile to see more options.")));
        foreach (var r in reps) _alts.Add(ReplacementCard(r, () => Navigation.PushAsync(new ExerciseDetailPage(r.Ex))));
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _anim.Stop();
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
