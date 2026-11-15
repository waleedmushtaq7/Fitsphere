using FitSphere.Pages;
using FitSphere.Services;
using FitSphere.UI;

namespace FitSphere;

public class App : Application
{
    protected override Window CreateWindow(IActivationState activationState) => new Window(new SplashPage());

    /// <summary>Swap the whole window content (used after login / logout).</summary>
    public static void SetRoot(Page page) => Current.Windows[0].Page = page;

    public static void ShowMain() => SetRoot(new AppShell());

    public static void ShowWelcome() => SetRoot(new NavigationPage(new WelcomePage()) { BarBackgroundColor = Ds.Bg });

    /// <summary>Decide where a signed-in user goes next.</summary>
    public static void AfterSignIn()
    {
        if (Svc.User.ProfileComplete) ShowMain();
        else SetRoot(new NavigationPage(new ProfileFormPage(true)));
    }
}
