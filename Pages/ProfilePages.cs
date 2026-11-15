using FitSphere.Models;
using FitSphere.Services;
using FitSphere.UI;

namespace FitSphere.Pages;

public class ProfilePage : BasePage
{
    readonly VerticalStackLayout _col = Column(18);

    public ProfilePage() : base(true) => SetBody(new ScrollView { Content = _col });

    protected override Task Refresh()
    {
        var u = Svc.User;
        _col.Children.Clear();
        _col.Add(Head("Profile", false));

        // identity
        string initial = (u.FullName ?? "F")[..1].ToUpper();
        var avatar = new Border
        {
            WidthRequest = 64, HeightRequest = 64, BackgroundColor = Ds.Mint, StrokeThickness = 0, StrokeShape = Ui.Round(32),
            Content = new Label { Text = initial, FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = Ds.Primary, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };
        var who = new Grid { ColumnSpacing = 16 };
        who.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        who.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        who.Add(avatar, 0, 0);
        var names = Ui.Stack(2, Ui.Txt(u.FullName, 20, true), Ui.Sub(u.Email, 14));
        names.VerticalOptions = LayoutOptions.Center;
        who.Add(names, 1, 0);
        _col.Add(who);

        // body stats
        var stats = Ui.Cols(4, 6);
        stats.Add(Stat(u.Age.ToString(), "Age"), 0, 0);
        stats.Add(Stat(Num.F(u.HeightCm), "Height cm"), 1, 0);
        stats.Add(Stat(Num.F(u.WeightKg), "Weight kg"), 2, 0);
        stats.Add(Stat(u.Bmi > 0 ? u.Bmi.ToString("0.0") : "-", "BMI"), 3, 0);
        _col.Add(Ui.Card(stats));

        // fitness profile
        _col.Add(Ui.Card(Ui.Stack(12,
            Line("Sex", string.IsNullOrEmpty(u.Sex) ? "Not set" : u.Sex),
            Line("Experience", u.Experience),
            Line("Goal", u.Goal),
            Line("Equipment", string.Join(", ", u.EquipmentList)),
            Line("Weekly workouts", u.WeeklyWorkoutGoal.ToString()))));

        _col.Add(Ui.SectionHeader("Settings"));
        _col.Add(Ui.MenuRow("ic_user_edit", "Edit profile", "Personal info, experience, goal, equipment", () => Navigation.PushAsync(new ProfileFormPage())));
        _col.Add(Ui.MenuRow("ic_target", "Goals & targets", $"{u.CalorieTarget} kcal · {u.ProteinTarget} g protein", () => Navigation.PushAsync(new TargetsPage())));
        _col.Add(Ui.MenuRow("ic_water", "Habits & reminders", "Water, reminders, rest days", () => Navigation.PushAsync(new HabitsPage())));

        // notifications switch
        var sw = new Switch { IsToggled = u.NotificationsEnabled, OnColor = Ds.Primary, ThumbColor = Colors.White };
        sw.Toggled += async (s, e) =>
        {
            try
            {
                u.NotificationsEnabled = e.Value;
                await Svc.Auth.SaveUserAsync();
                await Svc.Reminders.RescheduleAsync();
            }
            catch (Exception ex) { await Ui.Alert("Something went wrong", ex.Message); }
        };
        var notif = new Grid { ColumnSpacing = 14 };
        notif.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        notif.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        notif.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        notif.Add(Ui.IconBadge("ic_bell", 40, Ds.Mint, 20), 0, 0);
        var nt = Ui.Stack(2, Ui.Txt("Notifications", 16, true), Ui.Sub("Allow workout reminders", 13));
        nt.VerticalOptions = LayoutOptions.Center;
        notif.Add(nt, 1, 0);
        sw.VerticalOptions = LayoutOptions.Center;
        notif.Add(sw, 2, 0);
        _col.Add(Ui.Card(notif, pad: 14));

        _col.Add(Ui.SectionHeader("Account"));
        _col.Add(Ui.MenuRow("ic_lock", "Change password", null, () => Navigation.PushAsync(new ChangePasswordPage())));
        _col.Add(Ui.MenuRow("ic_logout", "Log out", null, LogoutAsync));
        _col.Add(Ui.MenuRow("ic_trash", "Delete account", "Removes all your data from this device", DeleteAccountAsync, true));
        return Task.CompletedTask;
    }

    static View Stat(string big, string small) =>
        Ui.Stack(2, Ui.Txt(big, 20, true, Ds.Primary, TextAlignment.Center), Ui.Txt(small, 11, false, Ds.Muted, TextAlignment.Center));

    static View Line(string label, string value) =>
        Ui.LeftRight(Ui.Txt(label, 14, false, Ds.Muted), Ui.Txt(value, 14, true, align: TextAlignment.End));

    async Task LogoutAsync()
    {
        if (!await Ui.Confirm("Log out", "You can log back in any time.", "Log out")) return;
        await Svc.Auth.LogoutAsync();
        App.ShowWelcome();
    }

    async Task DeleteAccountAsync()
    {
        if (!await Ui.Confirm("Delete account", "This permanently deletes your profile, plans, workouts, meals and progress from this device. It cannot be undone.")) return;
        await Busy(() => Svc.Auth.DeleteAccountAsync());
        App.ShowWelcome();
    }
}

public class TargetsPage : BasePage
{
    readonly FormInput _cal, _prot, _carb, _fat, _water, _weekly, _target;
    readonly Label _hint = Ui.Sub("", 12);

    public TargetsPage() : base(false)
    {
        var u = Svc.User;
        _cal = new FormInput("Daily calories (kcal)", "", Keyboard.Numeric, text: u.CalorieTarget.ToString());
        _prot = new FormInput("Protein (g)", "", Keyboard.Numeric, text: u.ProteinTarget.ToString());
        _carb = new FormInput("Carbs (g)", "", Keyboard.Numeric, text: u.CarbTarget.ToString());
        _fat = new FormInput("Fat (g)", "", Keyboard.Numeric, text: u.FatTarget.ToString());
        _water = new FormInput("Daily water (ml)", "", Keyboard.Numeric, text: u.WaterTargetMl.ToString());
        _weekly = new FormInput("Workouts per week", "", Keyboard.Numeric, text: u.WeeklyWorkoutGoal.ToString());
        _target = new FormInput("Target weight (kg), optional", "Leave empty if none", Keyboard.Numeric, text: u.TargetWeightKg > 0 ? Num.F(u.TargetWeightKg) : null);
        foreach (var f in new[] { _prot, _carb, _fat }) f.Entry.TextChanged += (s, e) => UpdateHint();

        var macros = Ui.Cols(3, 10);
        macros.Add(_prot, 0, 0); macros.Add(_carb, 1, 0); macros.Add(_fat, 2, 0);

        var col = Column(16);
        col.Add(Head("Goals & targets"));
        col.Add(Ui.Sub("These were estimated from your profile (including sex, age, height, weight and goal). Adjust them to match your coach, dietitian or preference.", 14));
        col.Add(Ui.SoftBtn("Recalculate from my profile", RecalculateAsync));
        col.Add(_cal);
        col.Add(macros);
        col.Add(_hint);
        col.Add(_water);
        col.Add(_weekly);
        col.Add(_target);
        col.Add(Ui.PrimaryBtn("Save targets", SaveAsync));
        SetBody(new ScrollView { Content = col });
        UpdateHint();
    }

    void UpdateHint()
    {
        if (Num.TryParse(_prot.Text, out var p) && Num.TryParse(_carb.Text, out var c) && Num.TryParse(_fat.Text, out var f))
            _hint.Text = $"Macros add up to about {p * 4 + c * 4 + f * 9:0} kcal.";
        else _hint.Text = "";
    }

    async Task RecalculateAsync()
    {
        var u = Svc.User;
        if (!await Ui.Confirm("Recalculate targets", "Replace the values below with estimates based on your current weight, goal and experience?", "Recalculate")) return;
        var tmp = new User { Sex = u.Sex, Age = u.Age, HeightCm = u.HeightCm, WeightKg = u.WeightKg, Experience = u.Experience, Goal = u.Goal };
        Svc.Nutrition.ApplyRecommended(tmp);
        _cal.Text = tmp.CalorieTarget.ToString(); _prot.Text = tmp.ProteinTarget.ToString();
        _carb.Text = tmp.CarbTarget.ToString(); _fat.Text = tmp.FatTarget.ToString(); _water.Text = tmp.WaterTargetMl.ToString();
    }

    async Task SaveAsync()
    {
        bool ok = true;
        ok &= _cal.Validate(800, 6000, "Calories", out var cal);
        ok &= _prot.Validate(0, 400, "Protein", out var p);
        ok &= _carb.Validate(0, 800, "Carbs", out var c);
        ok &= _fat.Validate(0, 300, "Fat", out var f);
        ok &= _water.Validate(500, 8000, "Water", out var water);
        ok &= _weekly.Validate(1, 7, "Workouts per week", out var weekly);
        ok &= _target.Validate(30, 300, "Target weight", out var target, false);
        if (!ok) return;

        var u = Svc.User;
        u.CalorieTarget = (int)cal; u.ProteinTarget = (int)p; u.CarbTarget = (int)c; u.FatTarget = (int)f;
        u.WaterTargetMl = (int)water; u.WeeklyWorkoutGoal = (int)weekly; u.TargetWeightKg = target;
        await Busy(() => Svc.Auth.SaveUserAsync());
        Ui.Toast("Targets saved");
        await Navigation.PopAsync();
    }
}

public class ChangePasswordPage : BasePage
{
    readonly FormInput _old = new("Current password", "", password: true);
    readonly FormInput _new = new("New password", "At least 6 characters", password: true);
    readonly FormInput _confirm = new("Confirm new password", "", password: true);

    public ChangePasswordPage() : base(false)
    {
        var col = Column(16);
        col.Add(Head("Change password"));
        col.Add(_old); col.Add(_new); col.Add(_confirm);
        col.Add(Ui.PrimaryBtn("Update password", SaveAsync));
        SetBody(new ScrollView { Content = col });
    }

    async Task SaveAsync()
    {
        foreach (var f in new[] { _old, _new, _confirm }) f.SetError(null);
        bool ok = true;
        if (string.IsNullOrEmpty(_old.Text)) { _old.SetError("Enter your current password"); ok = false; }
        if (_new.Text.Length < 6) { _new.SetError("Use at least 6 characters"); ok = false; }
        if (_confirm.Text != _new.Text) { _confirm.SetError("Passwords do not match"); ok = false; }
        if (!ok) return;

        OpResult res = null;
        await Busy(async () => res = await Svc.Auth.ChangePasswordAsync(_old.Text, _new.Text));
        if (res == null) return;
        if (!res.Ok) { _old.SetError(res.Error); return; }
        Ui.Toast("Password updated");
        await Navigation.PopAsync();
    }
}
