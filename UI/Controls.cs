using FitSphere.Services;
using Microsoft.Maui.Layouts;

namespace FitSphere.UI;

/// <summary>Label + text field + inline error message.</summary>
public class FormInput : VerticalStackLayout
{
    public Entry Entry { get; }
    readonly Label _error;

    public string Text
    {
        get => Entry.Text ?? "";
        set => Entry.Text = value;
    }

    public FormInput(string label, string placeholder = "", Keyboard keyboard = null, bool password = false, string text = null)
    {
        Spacing = 6;
        Entry = new Entry
        {
            Placeholder = placeholder, PlaceholderColor = Ds.Muted, TextColor = Ds.Text, FontSize = 16,
            Keyboard = keyboard ?? Keyboard.Text, IsPassword = password, BackgroundColor = Colors.Transparent, Text = text
        };
        var box = new Border
        {
            Content = Entry, BackgroundColor = Ds.Field, Stroke = Ds.Line, StrokeThickness = 1,
            StrokeShape = Ui.Round(Ds.ControlRadius), Padding = new Thickness(14, 0), HeightRequest = Ds.ControlHeight
        };
        _error = Ui.Txt("", 12, false, Ds.Danger);
        _error.IsVisible = false;
        Children.Add(Ui.Txt(label, 13, true, Ds.Muted));
        Children.Add(box);
        Children.Add(_error);
    }

    public void SetError(string message)
    {
        _error.Text = message;
        _error.IsVisible = !string.IsNullOrEmpty(message);
    }

    /// <summary>Validates a numeric range, shows the error under the field, and returns true when valid.</summary>
    public bool Validate(double min, double max, string name, out double value, bool required = true)
    {
        var err = Check.Range(Text, min, max, name, out value, required);
        SetError(err);
        return err == null;
    }
}

/// <summary>Row of selectable chips (single or multi select).</summary>
public class ChipGroup : ContentView
{
    readonly List<string> _items;
    readonly bool _multi;
    readonly Layout _host;
    public HashSet<string> Selection { get; } = new();
    public event Action Changed;

    public string Selected => Selection.FirstOrDefault();

    public ChipGroup(IEnumerable<string> items, bool multi = false, IEnumerable<string> selected = null, bool scroll = false)
    {
        _items = items.ToList();
        _multi = multi;
        if (selected != null) foreach (var s in selected) Selection.Add(s);

        if (scroll)
        {
            _host = new HorizontalStackLayout { Spacing = 0 };
            Content = new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = _host };
        }
        else
        {
            _host = new FlexLayout { Wrap = FlexWrap.Wrap, Direction = FlexDirection.Row };
            Content = _host;
        }
        Render();
    }

    public void SetSelection(IEnumerable<string> items)
    {
        Selection.Clear();
        foreach (var i in items) Selection.Add(i);
        Render();
    }

    public void Select(string item)
    {
        Selection.Clear();
        Selection.Add(item);
        Render();
    }

    void Render()
    {
        _host.Children.Clear();
        foreach (var it in _items)
        {
            var item = it;
            bool on = Selection.Contains(item);
            var chip = new Border
            {
                BackgroundColor = on ? Ds.Primary : Ds.Field, Stroke = on ? Ds.Primary : Ds.Line, StrokeThickness = 1,
                StrokeShape = Ui.Round(Ds.ControlRadius), Padding = new Thickness(14, 0), HeightRequest = 38,
                Margin = new Thickness(0, 0, 8, 8),
                Content = new Label { Text = item, FontSize = 14, TextColor = on ? Colors.White : Ds.Text, VerticalOptions = LayoutOptions.Center }
            };
            chip.OnTap(() =>
            {
                if (_multi) { if (!Selection.Remove(item)) Selection.Add(item); }
                else { Selection.Clear(); Selection.Add(item); }
                Render();
                Changed?.Invoke();
                return Task.CompletedTask;
            });
            _host.Children.Add(chip);
        }
    }
}

/// <summary>Compact "- value +" stepper for sets, reps, rest time etc.</summary>
public class Counter : HorizontalStackLayout
{
    readonly int _min, _max, _step;
    readonly string _suffix;
    readonly Label _label;
    public int Value { get; private set; }
    public event Action<int> Changed;

    public Counter(int value, int min, int max, int step = 1, string suffix = "")
    {
        _min = min; _max = max; _step = step; _suffix = suffix;
        Value = Math.Clamp(value, min, max);
        Spacing = 6;
        _label = Ui.Txt("", 15, true, align: TextAlignment.Center);
        _label.WidthRequest = suffix.Length > 0 ? 44 : 30;
        _label.VerticalOptions = LayoutOptions.Center;
        Children.Add(Make("-", -step));
        Children.Add(_label);
        Children.Add(Make("+", step));
        Refresh();
    }

    View Make(string text, int delta)
    {
        var b = new Border
        {
            WidthRequest = 34, HeightRequest = 34, BackgroundColor = Ds.Tint, StrokeThickness = 0,
            StrokeShape = Ui.Round(Ds.ControlRadius),
            Content = new Label { Text = text, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Ds.Soft, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };
        b.OnTap(() =>
        {
            Value = Math.Clamp(Value + delta, _min, _max);
            Refresh();
            Changed?.Invoke(Value);
            return Task.CompletedTask;
        });
        return b;
    }

    void Refresh() => _label.Text = Value + _suffix;
}
