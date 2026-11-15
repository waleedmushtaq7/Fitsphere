using FitSphere.Pages;
using FitSphere.UI;

namespace FitSphere;

/// <summary>Bottom navigation: Home, Workouts, Nutrition, Progress, Profile.</summary>
public class AppShell : Shell
{
    public AppShell()
    {
        BackgroundColor = Ds.Bg;
        Shell.SetNavBarIsVisible(this, false);
        Shell.SetTabBarBackgroundColor(this, Colors.White);
        Shell.SetTabBarForegroundColor(this, Ds.Primary);
        Shell.SetTabBarUnselectedColor(this, Ds.Muted);
        Shell.SetTabBarTitleColor(this, Ds.Primary);

        var tabs = new TabBar();
        tabs.Items.Add(Tab("Home", "ic_home.png", () => new HomePage()));
        tabs.Items.Add(Tab("Workouts", "ic_workouts.png", () => new WorkoutsPage()));
        tabs.Items.Add(Tab("Nutrition", "ic_nutrition.png", () => new NutritionPage()));
        tabs.Items.Add(Tab("Progress", "ic_progress.png", () => new ProgressPage()));
        tabs.Items.Add(Tab("Profile", "ic_profile.png", () => new ProfilePage()));
        Items.Add(tabs);
    }

    static ShellContent Tab(string title, string icon, Func<Page> create) => new ShellContent
    {
        Title = title,
        Icon = icon,
        ContentTemplate = new DataTemplate(create)
    };
}
