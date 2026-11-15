using FitSphere.Models;
using FitSphere.Services;
using FitSphere.UI;

namespace FitSphere.Pages;

/// <summary>Shown while the database opens and a saved session is restored.</summary>
public class SplashPage : ContentPage
{
    public SplashPage()
    {
        BackgroundColor = Ds.Primary;
        Content = new Image { Source = "logo_mark.png", WidthRequest = 96, HeightRequest = 96, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await Svc.Db.EnsureAsync();
            if (await Svc.Auth.RestoreAsync())
            {
                await Svc.Reminders.RescheduleAsync();
                App.AfterSignIn();
            }
            else App.ShowWelcome();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Startup error", ex.Message, "OK");
        }
    }
}

public class WelcomePage : ContentPage
{
    public WelcomePage()
    {
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Ds.Primary;

        var hero = new VerticalStackLayout { Spacing = 14, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
        hero.Add(new Image { Source = "logo_mark.png", WidthRequest = 88, HeightRequest = 88, HorizontalOptions = LayoutOptions.Center });
        hero.Add(Ui.Txt("FitSphere", 38, true, Colors.White, TextAlignment.Center));
        hero.Add(Ui.Txt("Plan your workouts, track your meals and watch your progress, all in one place.", 16, false, Ds.Mint, TextAlignment.Center));

        var actions = new VerticalStackLayout { Spacing = 8 };
        actions.Add(Ui.PrimaryBtn("Create account", () => Navigation.PushAsync(new RegisterPage()), Ds.Lime, Ds.Primary));
        var login = Ui.LinkBtn("I already have an account", () => Navigation.PushAsync(new LoginPage()), Colors.White);
        actions.Add(login);

        var g = new Grid { Padding = new Thickness(28, 60, 28, 28) };
        g.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        g.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        g.Add(hero, 0, 0);
        g.Add(actions, 0, 1);
        Content = g;
    }
}

public class LoginPage : BasePage
{
    readonly FormInput _email = new("Email", "you@example.com", Keyboard.Email);
    readonly FormInput _password = new("Password", "Your password", password: true);

    public LoginPage()
    {
        var col = Column(18);
        col.Add(Head("Welcome back"));
        col.Add(Ui.Sub("Log in to continue your training.", 15));
        col.Add(_email);
        col.Add(_password);
        col.Add(Ui.PrimaryBtn("Log in", LoginAsync));
        col.Add(Ui.LinkBtn("New here? Create an account", () => Navigation.PushAsync(new RegisterPage())));
        SetBody(new ScrollView { Content = col });
    }

    async Task LoginAsync()
    {
        _email.SetError(null); _password.SetError(null);
        bool ok = true;
        if (!Check.IsEmail(_email.Text)) { _email.SetError("Enter a valid email address"); ok = false; }
        if (string.IsNullOrEmpty(_password.Text)) { _password.SetError("Enter your password"); ok = false; }
        if (!ok) return;

        OpResult res = null;
        await Busy(async () => res = await Svc.Auth.LoginAsync(_email.Text, _password.Text));
        if (res == null) return;
        if (!res.Ok) { _password.SetError(res.Error); return; }
        await Svc.Reminders.RescheduleAsync();
        App.AfterSignIn();
    }
}

public class RegisterPage : BasePage
{
    readonly FormInput _name = new("Full name", "Your name");
    readonly FormInput _email = new("Email", "you@example.com", Keyboard.Email);
    readonly FormInput _password = new("Password", "At least 6 characters", password: true);
    readonly FormInput _confirm = new("Confirm password", "Repeat password", password: true);

    public RegisterPage()
    {
        var col = Column(16);
        col.Add(Head("Create account"));
        col.Add(Ui.Sub("Your data stays on this device.", 15));
        col.Add(_name);
        col.Add(_email);
        col.Add(_password);
        col.Add(_confirm);
        col.Add(Ui.PrimaryBtn("Continue", RegisterAsync));
        col.Add(Ui.LinkBtn("I already have an account", () => Navigation.PushAsync(new LoginPage())));
        SetBody(new ScrollView { Content = col });
    }

    async Task RegisterAsync()
    {
        foreach (var f in new[] { _name, _email, _password, _confirm }) f.SetError(null);
        bool ok = true;
        if (string.IsNullOrWhiteSpace(_name.Text)) { _name.SetError("Please enter your name"); ok = false; }
        if (!Check.IsEmail(_email.Text)) { _email.SetError("Enter a valid email address"); ok = false; }
        if (_password.Text.Length < 6) { _password.SetError("Use at least 6 characters"); ok = false; }
        if (_confirm.Text != _password.Text) { _confirm.SetError("Passwords do not match"); ok = false; }
        if (!ok) return;

        OpResult res = null;
        await Busy(async () => res = await Svc.Auth.RegisterAsync(_name.Text, _email.Text, _password.Text));
        if (res == null) return;
        if (!res.Ok) { _email.SetError(res.Error); return; }
        App.AfterSignIn();
    }
}

/// <summary>Used for first-time setup (after registration) and for editing the profile later.</summary>
public class ProfileFormPage : BasePage
{
    readonly bool _setup;
    readonly FormInput _name, _age, _height, _weight, _target;
    readonly ChipGroup _sex, _experience, _goal, _equipment;
    readonly Label _sexError = Ui.Txt("Please select your sex", 12, false, Ds.Danger);

    public ProfileFormPage() : this(false) { }

    public ProfileFormPage(bool setup) : base(false)
    {
        _setup = setup;
        var u = Svc.User;
        _name = new FormInput("Full name", "", text: u.FullName);
        _age = new FormInput("Age", "years", Keyboard.Numeric, text: u.Age > 0 ? u.Age.ToString() : null);
        _height = new FormInput("Height (cm)", "175", Keyboard.Numeric, text: u.HeightCm > 0 ? Num.F(u.HeightCm) : null);
        _weight = new FormInput("Weight (kg)", "70", Keyboard.Numeric, text: u.WeightKg > 0 ? Num.F(u.WeightKg) : null);
        _target = new FormInput("Target weight (kg), optional", "Leave empty if none", Keyboard.Numeric, text: u.TargetWeightKg > 0 ? Num.F(u.TargetWeightKg) : null);
        _sex = new ChipGroup(new[] { "Male", "Female" }, false, string.IsNullOrEmpty(u.Sex) ? null : new[] { u.Sex });
        _sexError.IsVisible = false;
        _experience = new ChipGroup(new[] { "Beginner", "Intermediate", "Advanced" }, false, new[] { u.Experience });
        _goal = new ChipGroup(new[] { "Muscle Building", "Weight Loss", "Strength", "General Fitness" }, false, new[] { u.Goal });
        _equipment = new ChipGroup(ExerciseService.EquipmentTypes, true, u.EquipmentList);

        var col = Column(18);
        if (setup)
        {
            col.Add(Ui.Title("Set up your profile"));
            col.Add(Ui.Sub("A few details so FitSphere can estimate your daily targets and tailor its suggestions. You can change everything later.", 15));
        }
        else col.Add(Head("Edit profile"));

        if (!setup) col.Add(_name);
        col.Add(Ui.Stack(8, Ui.Txt("Sex (used to estimate your calorie needs)", 13, true, Ds.Muted), _sex, _sexError));
        var body = Ui.Cols(3, 10);
        body.Add(_age, 0, 0); body.Add(_height, 1, 0); body.Add(_weight, 2, 0);
        col.Add(body);
        col.Add(_target);
        col.Add(Ui.Stack(8, Ui.Txt("Fitness experience", 13, true, Ds.Muted), _experience));
        col.Add(Ui.Stack(8, Ui.Txt("Fitness goal", 13, true, Ds.Muted), _goal));
        col.Add(Ui.Stack(8, Ui.Txt("Equipment you have access to", 13, true, Ds.Muted), _equipment,
            Ui.Sub("Used to suggest exercise replacements. Bodyweight is always included.", 12)));
        col.Add(Ui.PrimaryBtn(setup ? "Finish setup" : "Save changes", SaveAsync));
        SetBody(new ScrollView { Content = col });
    }

    async Task SaveAsync()
    {
        bool ok = true;
        if (!_setup && string.IsNullOrWhiteSpace(_name.Text)) { _name.SetError("Name is required"); ok = false; } else _name.SetError(null);
        _sexError.IsVisible = _sex.Selected == null;
        if (_sex.Selected == null) ok = false;
        ok &= _age.Validate(13, 100, "Age", out var age);
        ok &= _height.Validate(100, 250, "Height", out var height);
        ok &= _weight.Validate(30, 300, "Weight", out var weight);
        ok &= _target.Validate(30, 300, "Target weight", out var target, required: false);
        if (!ok) return;

        var u = Svc.User;
        string oldKey = $"{u.Age}|{u.HeightCm}|{u.WeightKg}|{u.Sex}|{u.Goal}|{u.Experience}";
        string newKey = $"{(int)age}|{height}|{weight}|{_sex.Selected}|{_goal.Selected ?? u.Goal}|{_experience.Selected ?? u.Experience}";
        bool weightChanged = u.WeightKg <= 0 || Math.Abs(weight - u.WeightKg) > 0.04;
        if (!_setup) u.FullName = _name.Text.Trim();
        u.Sex = _sex.Selected;
        u.Age = (int)age; u.HeightCm = height; u.TargetWeightKg = target;
        u.Experience = _experience.Selected ?? u.Experience;
        u.Goal = _goal.Selected ?? u.Goal;
        var eq = new HashSet<string>(_equipment.Selection) { "Bodyweight" };
        u.Equipment = string.Join(",", eq);

        if (_setup)
        {
            u.WeightKg = weight; u.StartWeightKg = weight;
            Svc.Nutrition.ApplyRecommended(u);
            u.ProfileComplete = true;
        }

        await Busy(async () =>
        {
            await Svc.Auth.SaveUserAsync();
            if (weightChanged)
                await Svc.Progress.SaveRecordAsync(new BodyRecord { Type = "Weight", Value = weight, Date = DateTime.Now });
        });

        if (_setup) { App.ShowMain(); return; }

        // Offer to refresh targets when something that affects calorie needs changed.
        if (oldKey != newKey && await Ui.Confirm("Update daily targets?",
                "Your sex, age, height, weight, experience or goal changed. Recalculate your calorie, macro and water targets to match?", "Update"))
        {
            Svc.Nutrition.ApplyRecommended(u);
            await Busy(() => Svc.Auth.SaveUserAsync());
        }
        Ui.Toast("Profile updated");
        await Navigation.PopAsync();
    }
}
