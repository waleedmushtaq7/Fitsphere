using FitSphere.Models;
using FitSphere.Services;
using FitSphere.UI;

namespace FitSphere.Pages;

/// <summary>Live workout screen: log sets, see the session timer and an automatic rest countdown.</summary>
public class ActiveWorkoutPage : BasePage
{
    readonly int _sid;
    WorkoutSession _session;
    List<SetLog> _sets = new();
    readonly Dictionary<int, Suggestion> _suggestions = new();
    readonly Label _planName = Ui.Sub("", 13);
    readonly Label _timer = Ui.Txt("00:00", 24, true);
    readonly Label _progress = Ui.Sub("", 14);
    readonly VerticalStackLayout _list = new() { Spacing = 16 };
    readonly Border _restBar;
    readonly Label _restLabel = Ui.Txt("", 17, true, Colors.White);
    IDispatcherTimer _tick;
    int _restLeft;
    bool _loaded;

    public ActiveWorkoutPage(int sessionId) : base(false)
    {
        _sid = sessionId;

        _restBar = Ui.Card(Ui.LeftRight(_restLabel, Ui.SmallBtn("Skip", () => { EndRest(); return Task.CompletedTask; }, Colors.White, Ds.Deep)),
            null, Ds.CardRadius, 14, false, Ds.HeroBrush);
        _restBar.IsVisible = false;
        _restBar.Margin = new Thickness(Ds.PageMargin, 0, Ds.PageMargin, 12);

        var header = new Grid { ColumnSpacing = 12, Padding = new Thickness(Ds.PageMargin, 16, Ds.PageMargin, 8) };
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        header.Add(Ui.IconButton("ic_close", DiscardAsync), 0, 0);
        var mid = Ui.Stack(0, _planName, _timer);
        mid.VerticalOptions = LayoutOptions.Center;
        header.Add(mid, 1, 0);
        var finish = Ui.SmallBtn("Finish", FinishAsync, Ds.Primary, Colors.White);
        finish.VerticalOptions = LayoutOptions.Center;
        header.Add(finish, 2, 0);

        var body = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(Ds.PageMargin, 8, Ds.PageMargin, 24) };
        body.Add(_progress);
        body.Add(_list);

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        root.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        root.Add(header, 0, 0);
        root.Add(new ScrollView { Content = body }, 0, 1);
        root.Add(_restBar, 0, 2);
        SetBody(root);
    }

    protected override async Task Refresh()
    {
        if (!_loaded)
        {
            _session = await Svc.Workouts.SessionAsync(_sid);
            if (_session == null || _session.Completed) { await Navigation.PopAsync(); return; }
            _sets = await Svc.Workouts.SetsAsync(_sid);
            var all = await Svc.Exercises.AllAsync();
            foreach (var g in _sets.GroupBy(x => x.ExerciseId))
            {
                var ex = all.FirstOrDefault(e => e.Id == g.Key);
                if (ex != null) _suggestions[g.Key] = await Svc.Insights.SuggestAsync(ex, g.Count(), g.First().TargetReps);
            }
            _planName.Text = _session.PlanName;
            _loaded = true;
        }
        Rebuild();
        StartTimer();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _tick?.Stop();
    }

    void StartTimer()
    {
        if (_tick == null)
        {
            _tick = Dispatcher.CreateTimer();
            _tick.Interval = TimeSpan.FromSeconds(1);
            _tick.Tick += (s, e) => Tick();
        }
        if (!_tick.IsRunning) _tick.Start();
        Tick();
    }

    void Tick()
    {
        if (_session == null) return;
        var el = DateTime.Now - _session.StartedAt;
        _timer.Text = el.TotalHours >= 1 ? $"{(int)el.TotalHours}:{el.Minutes:00}:{el.Seconds:00}" : $"{el.Minutes:00}:{el.Seconds:00}";
        if (_restLeft > 0)
        {
            _restLeft--;
            if (_restLeft == 0) { _restBar.IsVisible = false; Ui.Toast("Rest over, next set"); }
            else _restLabel.Text = $"Rest {_restLeft / 60}:{_restLeft % 60:00}";
        }
    }

    void StartRest(int seconds)
    {
        if (seconds <= 0) return;
        _restLeft = seconds;
        _restLabel.Text = $"Rest {seconds / 60}:{seconds % 60:00}";
        _restBar.IsVisible = true;
    }

    void EndRest() { _restLeft = 0; _restBar.IsVisible = false; }

    void UpdateProgress() => _progress.Text = $"{_sets.Count(x => x.Completed)} of {_sets.Count} sets completed";

    void Rebuild()
    {
        _list.Children.Clear();
        foreach (var g in _sets.GroupBy(x => x.ExerciseId))
        {
            var sets = g.ToList();
            var card = new VerticalStackLayout { Spacing = 10 };
            card.Add(Ui.Txt(sets[0].ExerciseName, 18, true));
            if (_suggestions.TryGetValue(g.Key, out var sg)) card.Add(SuggestionPill(sg));
            card.Add(HeaderRow());
            foreach (var s in sets) card.Add(SetRow(s));
            card.Add(Ui.LinkBtn("+ Add set", async () =>
            {
                var added = await Svc.Workouts.AddSetAsync(sets.Last());
                _sets.Add(added);
                Rebuild();
            }));
            _list.Add(Ui.Card(card));
        }
        UpdateProgress();
    }

    static View SuggestionPill(Suggestion sg)
    {
        var g = new Grid { ColumnSpacing = 10 };
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        var icon = Ui.Icon("ic_bulb", 20);
        icon.VerticalOptions = LayoutOptions.Start;
        g.Add(icon, 0, 0);
        g.Add(Ui.Stack(2,
            Ui.Txt((sg.Kind == "Start" ? "Tip: " : "Suggested: ") + sg.Headline, 13, true, Ds.Soft),
            Ui.Txt(sg.Reason, 12, false, Ds.Text)), 1, 0);
        return Ui.Card(g, Ds.Tint, Ds.ControlRadius, 12, false);
    }

    static Grid SetGrid()
    {
        var g = new Grid { ColumnSpacing = 10 };
        g.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(34)));
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        g.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(44)));
        return g;
    }

    static View HeaderRow()
    {
        var g = SetGrid();
        g.Add(Ui.Txt("SET", 11, true, Ds.Muted), 0, 0);
        g.Add(Ui.Txt("KG", 11, true, Ds.Muted, TextAlignment.Center), 1, 0);
        g.Add(Ui.Txt("REPS", 11, true, Ds.Muted, TextAlignment.Center), 2, 0);
        return g;
    }

    static (Border box, Entry entry) Cell(string text, string placeholder)
    {
        var e = new Entry
        {
            Text = text, Placeholder = placeholder, PlaceholderColor = Ds.Muted, Keyboard = Keyboard.Numeric, FontSize = 17,
            TextColor = Ds.Text, HorizontalTextAlignment = TextAlignment.Center, BackgroundColor = Colors.Transparent, VerticalOptions = LayoutOptions.Center
        };
        var b = new Border
        {
            Content = e, BackgroundColor = Ds.Bg, Stroke = Ds.Line, StrokeThickness = 1,
            StrokeShape = Ui.Round(Ds.ControlRadius), HeightRequest = 44, Padding = new Thickness(6, 0)
        };
        return (b, e);
    }

    View SetRow(SetLog s)
    {
        var g = SetGrid();
        var (wBox, wEntry) = Cell(s.WeightKg > 0 ? Num.F(s.WeightKg) : "", "0");
        var (rBox, rEntry) = Cell(s.Reps > 0 ? s.Reps.ToString() : "", "0");
        wEntry.TextChanged += (a, b) => s.WeightKg = Num.TryParse(wEntry.Text, out var v) && v >= 0 ? v : 0;
        rEntry.TextChanged += (a, b) => s.Reps = int.TryParse(rEntry.Text, out var v) && v >= 0 ? v : 0;
        async void Persist(object a, FocusEventArgs b) { try { await Svc.Workouts.SaveSetAsync(s); } catch { /* saved again on completion */ } }
        wEntry.Unfocused += Persist;
        rEntry.Unfocused += Persist;

        var tick = new Label { Text = "✓", FontSize = 20, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        var chk = new Border { WidthRequest = 44, HeightRequest = 44, StrokeThickness = 0, StrokeShape = Ui.Round(Ds.ControlRadius), Content = tick };
        void Paint()
        {
            chk.BackgroundColor = s.Completed ? Ds.Emerald : Ds.Tint;
            tick.TextColor = s.Completed ? Ds.Bg : Ds.Muted;
        }
        Paint();
        chk.OnTap(async () =>
        {
            if (!s.Completed && s.Reps <= 0)
            {
                await Ui.Alert("Add reps", "Enter the number of reps you completed for this set.");
                return;
            }
            s.Completed = !s.Completed;
            Paint();
            await Svc.Workouts.SaveSetAsync(s);
            UpdateProgress();
            if (s.Completed) StartRest(s.RestSeconds);
        });

        var num = Ui.Txt(s.SetNumber.ToString(), 16, true, Ds.Soft);
        num.VerticalOptions = LayoutOptions.Center;
        g.Add(num, 0, 0);
        g.Add(wBox, 1, 0);
        g.Add(rBox, 2, 0);
        g.Add(chk, 3, 0);
        return g;
    }

    async Task FinishAsync()
    {
        int done = _sets.Count(x => x.Completed);
        if (done == 0)
        {
            await Ui.Alert("Nothing logged yet", "Complete at least one set, or discard this workout with the X button.");
            return;
        }
        int left = _sets.Count - done;
        string msg = left > 0 ? $"{left} set(s) are not completed. Finish anyway?" : "Finish this workout and save it to your history?";
        if (!await Ui.Confirm("Finish workout", msg, "Finish")) return;

        _tick?.Stop();
        FinishResult res = null;
        await Busy(async () =>
        {
            foreach (var s in _sets) await Svc.Workouts.SaveSetAsync(s);
            res = await Svc.Workouts.FinishAsync(_session);
        });
        if (res == null) return;

        string summary = $"Time: {res.Minutes} min\nSets completed: {res.SetsDone}\nVolume lifted: {res.Volume:0} kg";
        if (res.NewRecords.Count > 0) summary += "\n\nNew personal records:\n" + string.Join("\n", res.NewRecords);
        await Ui.Alert("Workout complete", summary);
        await Navigation.PopAsync();
    }

    async Task DiscardAsync()
    {
        if (!await Ui.Confirm("Discard workout", "This deletes everything you logged in this session.", "Discard")) return;
        _tick?.Stop();
        await Busy(() => Svc.Workouts.DiscardAsync(_sid));
        await Navigation.PopAsync();
    }
}
