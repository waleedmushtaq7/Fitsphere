using System.Text.RegularExpressions;
using FitSphere.Models;

namespace FitSphere.UI;

/// <summary>
/// Shows an exercise's START and FINISH photos and cross-fades between them in a loop, so the user can see
/// the movement. Photos are bundled with the app (Resources/Raw/ex_{name}_0.jpg and _1.jpg).
/// If an exercise has no photos, the muscle-group illustration is shown instead.
/// </summary>
public class ExerciseAnimation : ContentView
{
    readonly Exercise _ex;
    readonly string _slug;
    readonly Image _start = new() { Aspect = Aspect.AspectFill };
    readonly Image _end = new() { Aspect = Aspect.AspectFill, Opacity = 0 };
    readonly Label _tag = Ui.Txt("START", 11, true, Colors.White);
    bool _has, _running, _loaded;
    int _runId;

    public ExerciseAnimation(Exercise ex)
    {
        _ex = ex;
        _slug = Slug(ex.Name);
        var art = Ui.Icon("mg_" + ex.MuscleGroup.ToLowerInvariant(), 96);
        art.HorizontalOptions = LayoutOptions.Center;
        Content = Ui.Card(art, Ds.Tint, Ds.HeroRadius, 28, false);   // fallback until photos are found
    }

    public static string Slug(string name) => Regex.Replace(name.ToLowerInvariant(), "[^a-z0-9]+", "_").Trim('_');

    static ImageSource Frame(string slug, int index)
    {
        var file = $"ex_{slug}_{index}.jpg";
        return new StreamImageSource { Stream = ct => FileSystem.OpenAppPackageFileAsync(file) };
    }

    /// <summary>Checks for bundled photos and builds the animated view when they exist.</summary>
    public async Task LoadAsync()
    {
        if (_loaded) return;
        _loaded = true;
        try
        {
            _has = await FileSystem.AppPackageFileExistsAsync($"ex_{_slug}_0.jpg")
                && await FileSystem.AppPackageFileExistsAsync($"ex_{_slug}_1.jpg");
        }
        catch { _has = false; }
        if (!_has) return;

        _start.Source = Frame(_slug, 0);
        _end.Source = Frame(_slug, 1);

        var layers = new Grid { HeightRequest = 230 };
        layers.Add(_start);
        layers.Add(_end);
        layers.Add(new Border
        {
            BackgroundColor = Color.FromRgba(0f, 0f, 0f, 0.6f), StrokeThickness = 0, StrokeShape = Ui.Round(Ds.ControlRadius),
            Padding = new Thickness(10, 4), Content = _tag, HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.End, Margin = new Thickness(12)
        });
        var frame = new Border
        {
            StrokeShape = Ui.Round(Ds.HeroRadius), StrokeThickness = 0, BackgroundColor = Ds.Card, Content = layers
        };
        Content = Ui.Stack(8, frame, Ui.Sub("Photos: free-exercise-db (public domain)", 11));
    }

    public void Start()
    {
        if (!_has || _running) return;
        _running = true;
        _ = Loop(++_runId);
    }

    public void Stop()
    {
        _running = false;
        _runId++;
    }

    async Task Loop(int id)
    {
        try
        {
            _end.Opacity = 0;
            _tag.Text = "START";
            while (_running && id == _runId)
            {
                await Task.Delay(1000);
                if (!_running || id != _runId) break;
                await _end.FadeTo(1, 500);
                _tag.Text = "FINISH";
                await Task.Delay(1000);
                if (!_running || id != _runId) break;
                await _end.FadeTo(0, 500);
                _tag.Text = "START";
            }
        }
        catch { /* page closed while animating */ }
    }
}
