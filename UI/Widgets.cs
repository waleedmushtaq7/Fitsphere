using FitSphere.Models;
using FitSphere.Pages;
using FitSphere.Services;

namespace FitSphere.UI;

public static class Widgets
{
    /// <summary>Gradient colours per macro: protein = magenta, carbs = amber, fat = emerald.</summary>
    public static (Color from, Color to) MacroColors(string name) =>
        name.StartsWith("Protein") ? (Ds.Magenta, Ds.Pink)
        : name.StartsWith("Carb") ? (Ds.Amber, Color.FromArgb("#FFE066"))
        : (Ds.Emerald, Ds.Teal);

    /// <summary>Label with "x / target g" and a gradient bar underneath (used on the Nutrition screen).</summary>
    public static View MacroLine(string name, double value, double target)
    {
        var (a, b) = MacroColors(name);
        var head = Ui.LeftRight(Ui.Txt(name, 14, true),
            Ui.Sub(target > 0 ? $"{value:0} / {target:0} g" : $"{value:0} g", 12));
        return Ui.Stack(6, head, Ui.GradBar(value, target, a, b));
    }

    /// <summary>Compact one-line macro row: label | gradient bar | grams (used on Home).</summary>
    public static View MacroRow(string name, double value, double target)
    {
        var (a, b) = MacroColors(name);
        var g = new Grid { ColumnSpacing = 12 };
        g.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(64)));
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        g.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(54)));
        var label = Ui.Txt(name, 14, true);
        label.VerticalOptions = LayoutOptions.Center;
        var bar = Ui.GradBar(value, target, a, b, 8);
        bar.VerticalOptions = LayoutOptions.Center;
        var val = Ui.Txt($"{value:0} g", 14, true, align: TextAlignment.End);
        val.VerticalOptions = LayoutOptions.Center;
        g.Add(label, 0, 0); g.Add(bar, 1, 0); g.Add(val, 2, 0);
        return g;
    }

    public static View DayCircle(DayState st, bool today, string dayNumber)
    {
        Color bg = Ds.Card, stroke = Ds.Line, fg = Ds.Muted;
        string text = "";
        switch (st)
        {
            case DayState.Completed: bg = Ds.Emerald; stroke = Ds.Emerald; fg = Ds.Bg; text = "✓"; break;
            case DayState.Extra: bg = Ds.Tint; stroke = Ds.Tint; fg = Ds.Emerald; text = "✓"; break;
            case DayState.Scheduled: bg = Ds.Tint; stroke = Ds.Tint; fg = Ds.Soft; text = dayNumber; break;
            case DayState.Missed: bg = Ds.Field; text = "–"; break;
            case DayState.Rest: bg = Ds.Tint; stroke = Ds.Tint; fg = Ds.Amber; text = "R"; break;
        }
        return new Border
        {
            WidthRequest = 36, HeightRequest = 36, BackgroundColor = bg, Stroke = today ? Ds.Soft : stroke,
            StrokeThickness = today ? 2 : 1, StrokeShape = Ui.Round(Ds.ControlRadius), HorizontalOptions = LayoutOptions.Center,
            Content = new Label { Text = text, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = fg, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };
    }
}

/// <summary>Shared navigation flows.</summary>
public static class Flow
{
    public static async Task StartOrResume(INavigation nav, WorkoutPlan plan)
    {
        var active = await Svc.Workouts.ActiveSessionAsync();
        if (active != null)
        {
            if (await Ui.Confirm("Workout in progress", $"You have an unfinished \"{active.PlanName}\" workout. Resume it?", "Resume"))
                await nav.PushAsync(new ActiveWorkoutPage(active.Id));
            return;
        }
        var items = await Svc.Workouts.ItemsAsync(plan.Id);
        if (items.Count == 0) { await Ui.Alert("Empty plan", "Add at least one exercise to this plan first."); return; }
        var id = await Svc.Workouts.StartAsync(plan);
        await nav.PushAsync(new ActiveWorkoutPage(id));
    }

    public static string DefaultMeal() => DateTime.Now.Hour switch { < 11 => "Breakfast", < 16 => "Lunch", < 21 => "Dinner", _ => "Snacks" };
}
