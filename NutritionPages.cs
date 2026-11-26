using FitSphere.Models;
using FitSphere.Services;
using FitSphere.UI;

namespace FitSphere.Pages;

public class NutritionPage : BasePage
{
    DateTime _day = DateTime.Today;
    readonly VerticalStackLayout _col = Column(18);

    public NutritionPage() : base(true) => SetBody(new ScrollView { Content = _col });

    protected override async Task Refresh()
    {
        var u = Svc.User;
        var entries = await Svc.Nutrition.DayAsync(_day);
        var t = NutritionService.Totals(entries);
        double rem = u.CalorieTarget - t.Cal;
        bool over = rem < 0;

        _col.Children.Clear();
        _col.Add(Head("Nutrition", false));

        // day navigator
        bool canNext = _day.Date < DateTime.Today;
        var prev = Ui.IconButton("ic_back", async () => { _day = _day.AddDays(-1); await Busy(Refresh); }, 38);
        var next = Ui.IconButton("ic_chevron", async () => { _day = _day.AddDays(1); await Busy(Refresh); }, 38);
        next.Opacity = canNext ? 1 : 0.25;
        next.InputTransparent = !canNext;
        var nav = new Grid { ColumnSpacing = 12 };
        nav.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        nav.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        nav.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        nav.Add(prev, 0, 0);
        nav.Add(Ui.Txt(Dates.Friendly(_day), 17, true, align: TextAlignment.Center), 1, 0);
        nav.Add(next, 2, 0);
        ((Label)nav.Children[1]).VerticalOptions = LayoutOptions.Center;
        _col.Add(nav);

        // calories summary
        var ring = Ui.RingView(u.CalorieTarget > 0 ? t.Cal / u.CalorieTarget : 0, $"{t.Cal:0}", "kcal", 124, over ? Ds.Magenta : Ds.Amber, over ? Ds.Rose : Ds.Orange);
        var stats = Ui.Stack(10,
            StatLine("Target", $"{u.CalorieTarget} kcal"),
            StatLine("Consumed", $"{t.Cal:0} kcal"),
            StatLine(over ? "Over by" : "Remaining", $"{Math.Abs(rem):0} kcal", over ? Ds.Danger : Ds.Emerald));
        stats.VerticalOptions = LayoutOptions.Center;
        var top = new Grid { ColumnSpacing = 20 };
        top.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        top.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        top.Add(ring, 0, 0);
        top.Add(stats, 1, 0);
        _col.Add(Ui.Card(top, pad: 18));

        // macros
        _col.Add(Ui.Card(Ui.Stack(12,
            Widgets.MacroLine("Protein", t.P, u.ProteinTarget),
            Widgets.MacroLine("Carbohydrates", t.C, u.CarbTarget),
            Widgets.MacroLine("Fat", t.F, u.FatTarget))));

        // meals
        foreach (var meal in NutritionService.Meals)
        {
            var m = meal;
            var items = entries.Where(e => e.Meal == m).ToList();
            double kcal = items.Sum(e => e.Calories);
            _col.Add(Ui.LeftRight(
                Ui.Stack(1, Ui.H2(m), Ui.Sub($"{kcal:0} kcal", 13)),
                Ui.IconButton("ic_plus", () => Navigation.PushAsync(new AddFoodPage(m, _day)), 38)));

            if (items.Count == 0)
                _col.Add(Ui.Card(Ui.Sub("Nothing logged yet. Tap + to add food.", 14), Ds.Card, pad: 14)
                    .OnTap(() => Navigation.PushAsync(new AddFoodPage(m, _day))));
            else
            {
                var rows = new VerticalStackLayout { Spacing = 14 };
                foreach (var e in items)
                {
                    var entry = e;
                    var row = Ui.LeftRight(
                        Ui.Stack(2, Ui.Txt(entry.Name, 15, true), Ui.Sub($"P {entry.Protein:0} g · C {entry.Carbs:0} g · F {entry.Fat:0} g", 12)),
                        Ui.Txt($"{entry.Calories:0}", 15, true));
                    rows.Add(row.OnTap(() => Navigation.PushAsync(new AddFoodPage(m, _day, entry))));
                }
                _col.Add(Ui.Card(rows));
            }
        }
    }

    static View StatLine(string label, string value, Color valueColor = null) =>
        Ui.Stack(0, Ui.Sub(label, 12), Ui.Txt(value, 17, true, valueColor ?? Ds.Text));
}

public class AddFoodPage : BasePage
{
    readonly FoodEntry _entry;
    readonly DateTime _date;
    readonly FormInput _name = new("Food name", "e.g. Chicken breast");
    readonly FormInput _cal = new("Calories (kcal)", "Auto-calculated if left empty", Keyboard.Numeric);
    readonly FormInput _protein = new("Protein (g)", "0", Keyboard.Numeric);
    readonly FormInput _carbs = new("Carbs (g)", "0", Keyboard.Numeric);
    readonly FormInput _fat = new("Fat (g)", "0", Keyboard.Numeric);
    readonly ChipGroup _meal;
    readonly VerticalStackLayout _quick = new() { Spacing = 8 };

    public AddFoodPage(string meal, DateTime date, FoodEntry entry = null) : base(false)
    {
        _entry = entry;
        _date = date;
        _meal = new ChipGroup(NutritionService.Meals, false, new[] { entry?.Meal ?? meal });

        if (entry != null)
        {
            _name.Text = entry.Name;
            _cal.Text = Num.F(entry.Calories, "0");
            _protein.Text = Num.F(entry.Protein); _carbs.Text = Num.F(entry.Carbs); _fat.Text = Num.F(entry.Fat);
        }

        var col = Column(16);
        col.Add(Head(entry == null ? "Add food" : "Edit food", true, entry != null ? Ui.IconButton("ic_trash", DeleteAsync) : null));
        col.Add(Ui.Stack(8, Ui.Txt("Meal", 13, true, Ds.Muted), _meal));
        if (entry == null) col.Add(_quick);
        col.Add(_name);
        col.Add(_cal);
        var macros = Ui.Cols(3, 10);
        macros.Add(_protein, 0, 0); macros.Add(_carbs, 1, 0); macros.Add(_fat, 2, 0);
        col.Add(macros);
        col.Add(Ui.PrimaryBtn(entry == null ? "Add to " + (meal ?? "meal").ToLower() : "Save changes", SaveAsync));
        SetBody(new ScrollView { Content = col });
    }

    protected override async Task Refresh()
    {
        if (_entry != null) return;
        var recent = await Svc.Nutrition.RecentAsync();
        _quick.Children.Clear();
        if (recent.Count > 0)
            _quick.Add(Strip("Recent", recent.Select(r => (r.Name, (Func<Task>)(() => Fill(r.Name, r.Calories, r.Protein, r.Carbs, r.Fat)))).ToList()));
        _quick.Add(Strip("Common foods", NutritionService.Common.Select(c => (c.Name, (Func<Task>)(() => Fill(c.Name, c.Cal, c.P, c.C, c.F)))).ToList()));
    }

    Task Fill(string name, double cal, double p, double c, double f)
    {
        _name.Text = name; _cal.Text = Num.F(cal, "0");
        _protein.Text = Num.F(p); _carbs.Text = Num.F(c); _fat.Text = Num.F(f);
        return Task.CompletedTask;
    }

    static View Strip(string title, List<(string name, Func<Task> tap)> items)
    {
        var row = new HorizontalStackLayout { Spacing = 8 };
        foreach (var (name, tap) in items)
        {
            var chip = new Border
            {
                BackgroundColor = Ds.Tint, StrokeThickness = 0, StrokeShape = Ui.Round(Ds.ControlRadius), Padding = new Thickness(14, 0), HeightRequest = 38,
                Content = new Label { Text = name, FontSize = 14, TextColor = Ds.Soft, VerticalOptions = LayoutOptions.Center }
            };
            chip.OnTap(tap);
            row.Add(chip);
        }
        return Ui.Stack(6, Ui.Txt(title, 13, true, Ds.Muted),
            new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = row });
    }

    async Task SaveAsync()
    {
        bool ok = true;
        if (string.IsNullOrWhiteSpace(_name.Text)) { _name.SetError("Enter a food name"); ok = false; } else _name.SetError(null);
        ok &= _protein.Validate(0, 500, "Protein", out var p, false);
        ok &= _carbs.Validate(0, 1000, "Carbs", out var cb, false);
        ok &= _fat.Validate(0, 500, "Fat", out var f, false);
        ok &= _cal.Validate(0, 5000, "Calories", out var cal, false);
        if (!ok) return;

        if (string.IsNullOrWhiteSpace(_cal.Text))
        {
            if (p + cb + f <= 0) { _cal.SetError("Enter calories or at least one macro"); return; }
            cal = Math.Round(p * 4 + cb * 4 + f * 9);
        }

        var e = _entry ?? new FoodEntry();
        e.Name = _name.Text.Trim(); e.Calories = cal; e.Protein = p; e.Carbs = cb; e.Fat = f;
        e.Meal = _meal.Selected ?? "Snacks";
        if (_entry == null) e.Date = _date;
        await Busy(() => Svc.Nutrition.SaveAsync(e));
        Ui.Toast(_entry == null ? "Food added" : "Food updated");
        await Navigation.PopAsync();
    }

    async Task DeleteAsync()
    {
        if (!await Ui.Confirm("Delete food", $"Remove \"{_entry.Name}\" from your log?")) return;
        await Busy(() => Svc.Nutrition.DeleteAsync(_entry));
        await Navigation.PopAsync();
    }
}
