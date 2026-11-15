using FitSphere.Models;
using FitSphere.Pages;
using FitSphere.Services;

namespace FitSphere.UI;

public static class Widgets
{
    public static View MacroLine(string name, double value, double target, Color fill = null)
    {
        var head = Ui.LeftRight(Ui.Txt(name, 13, true),
            Ui.Sub(target > 0 ? $"{value:0} / {target:0} g" : $"{value:0} g", 12));
        return Ui.Stack(4, head, Ui.Bar(value, target, fill));
    }

    public static View DayCircle(DayState st, bool today, string dayNumber)
    {
        Color bg = Colors.White, stroke = Ds.Line, fg = Ds.Muted;
        string text = "";
        switch (st)
        {
            case DayState.Completed: bg = Ds.Primary; stroke = Ds.Primary; fg = Colors.White; text = "✓"; break;
            case DayState.Extra: bg = Ds.Mint; stroke = Ds.Mint; fg = Ds.Primary; text = "✓"; break;
            case DayState.Scheduled: bg = Ds.Mint; stroke = Ds.Mint; fg = Ds.Primary; text = dayNumber; break;
            case DayState.Missed: text = "–"; break;
            case DayState.Rest: bg = Ds.Lime; stroke = Ds.Lime; fg = Ds.Primary; text = "R"; break;
        }
        return new Border
        {
            WidthRequest = 36, HeightRequest = 36, BackgroundColor = bg, Stroke = today ? Ds.Primary : stroke,
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
