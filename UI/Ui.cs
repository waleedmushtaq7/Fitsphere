using FitSphere.Models;
using Microsoft.Maui.Controls.Shapes;

namespace FitSphere.UI;

/// <summary>Small factory methods that build the app's components with the design tokens applied.</summary>
public static class Ui
{
    // ---------- Text ----------
    public static Label Txt(string text, double size = 15, bool bold = false, Color color = null, TextAlignment align = TextAlignment.Start) => new Label
    {
        Text = text, FontSize = size, TextColor = color ?? Ds.Text,
        FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
        HorizontalTextAlignment = align, LineBreakMode = LineBreakMode.WordWrap
    };

    public static Label Title(string t) => Txt(t, 26, true);
    public static Label H2(string t) => Txt(t, 18, true);
    public static Label Sub(string t, double size = 13) => Txt(t, size, false, Ds.Muted);

    // ---------- Containers ----------
    public static RoundRectangle Round(double r) => new RoundRectangle { CornerRadius = new CornerRadius(r) };

    public static Border Card(View content, Color bg = null, double radius = Ds.CardRadius, double pad = 16, bool shadow = true)
    {
        var b = new Border
        {
            Content = content, BackgroundColor = bg ?? Ds.Card, StrokeThickness = 0, Stroke = Brush.Transparent,
            StrokeShape = Round(radius), Padding = new Thickness(pad)
        };
        if (shadow)
            b.Shadow = new Shadow { Brush = new SolidColorBrush(Colors.Black), Opacity = 0.06f, Radius = 14, Offset = new Point(0, 4) };
        return b;
    }

    public static VerticalStackLayout Stack(double spacing = 12, params View[] children)
    {
        var s = new VerticalStackLayout { Spacing = spacing };
        foreach (var c in children) s.Add(c);
        return s;
    }

    public static HorizontalStackLayout Row(double spacing = 8, params View[] children)
    {
        var s = new HorizontalStackLayout { Spacing = spacing };
        foreach (var c in children) s.Add(c);
        return s;
    }

    public static Grid Cols(int n, double spacing = 12)
    {
        var g = new Grid { ColumnSpacing = spacing };
        for (int i = 0; i < n; i++) g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        return g;
    }

    /// <summary>Left content stretches, right content hugs the edge.</summary>
    public static Grid LeftRight(View left, View right, double spacing = 12)
    {
        var g = new Grid { ColumnSpacing = spacing };
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        left.VerticalOptions = LayoutOptions.Center;
        right.VerticalOptions = LayoutOptions.Center;
        g.Add(left, 0, 0);
        g.Add(right, 1, 0);
        return g;
    }

    // ---------- Icons ----------
    public static Image Icon(string name, double size = 22) =>
        new Image { Source = name + ".png", WidthRequest = size, HeightRequest = size, VerticalOptions = LayoutOptions.Center };

    public static Border IconBadge(string icon, double size = 44, Color bg = null, double iconSize = 22)
    {
        var img = Icon(icon, iconSize);
        img.HorizontalOptions = LayoutOptions.Center;
        return new Border
        {
            WidthRequest = size, HeightRequest = size, BackgroundColor = bg ?? Ds.Mint, StrokeThickness = 0,
            StrokeShape = Round(Ds.ControlRadius), Content = img, VerticalOptions = LayoutOptions.Center
        };
    }

    public static Border IconButton(string icon, Func<Task> tap, double size = 40)
    {
        var b = IconBadge(icon, size, Colors.White, 20);
        b.Shadow = new Shadow { Brush = new SolidColorBrush(Colors.Black), Opacity = 0.06f, Radius = 10, Offset = new Point(0, 3) };
        return b.OnTap(tap);
    }

    // ---------- Buttons ----------
    static EventHandler Safe(Func<Task> f) => async (s, e) =>
    {
        var v = s as VisualElement;
        if (v != null) v.IsEnabled = false;
        try { await f(); }
        catch (Exception ex) { await Alert("Something went wrong", ex.Message); }
        finally { if (v != null) v.IsEnabled = true; }
    };

    static Button Btn(string text, Func<Task> tap, Color bg, Color fg)
    {
        var b = new Button
        {
            Text = text, BackgroundColor = bg, TextColor = fg, CornerRadius = (int)Ds.ControlRadius,
            HeightRequest = Ds.ControlHeight, FontSize = 16, FontAttributes = FontAttributes.Bold, Padding = new Thickness(18, 0)
        };
        b.Clicked += Safe(tap);
        return b;
    }

    public static Button PrimaryBtn(string text, Func<Task> tap, Color bg = null, Color fg = null) => Btn(text, tap, bg ?? Ds.Primary, fg ?? Colors.White);
    public static Button SoftBtn(string text, Func<Task> tap) => Btn(text, tap, Ds.Mint, Ds.Primary);
    public static Button DangerBtn(string text, Func<Task> tap) => Btn(text, tap, Colors.White, Ds.Danger);

    public static Button SmallBtn(string text, Func<Task> tap, Color bg = null, Color fg = null)
    {
        var b = Btn(text, tap, bg ?? Ds.Mint, fg ?? Ds.Primary);
        b.HeightRequest = 38; b.FontSize = 14; b.Padding = new Thickness(14, 0);
        return b;
    }

    public static Button LinkBtn(string text, Func<Task> tap, Color color = null)
    {
        var b = Btn(text, tap, Colors.Transparent, color ?? Ds.Primary);
        b.HeightRequest = 40; b.FontSize = 14; b.Padding = new Thickness(6, 0);
        return b;
    }

    public static T OnTap<T>(this T view, Func<Task> action) where T : View
    {
        var g = new TapGestureRecognizer();
        g.Tapped += async (s, e) =>
        {
            try { await action(); }
            catch (Exception ex) { await Alert("Something went wrong", ex.Message); }
        };
        view.GestureRecognizers.Add(g);
        return view;
    }

    // ---------- Dialogs ----------
    static Page CurrentPage => Application.Current.Windows[0].Page;

    public static Task Alert(string title, string message) => CurrentPage.DisplayAlert(title, message, "OK");

    public static Task<bool> Confirm(string title, string message, string yes = "Delete") =>
        CurrentPage.DisplayAlert(title, message, yes, "Cancel");

    public static void Toast(string message)
    {
#if ANDROID
        Android.Widget.Toast.MakeText(Platform.AppContext, message, Android.Widget.ToastLength.Short).Show();
#endif
    }

    // ---------- Composite widgets ----------
    public static View SectionHeader(string title, string actionText = null, Func<Task> action = null)
    {
        if (actionText == null) return H2(title);
        return LeftRight(H2(title), LinkBtn(actionText, action));
    }

    public static View Empty(string icon, string title, string sub, string buttonText = null, Func<Task> tap = null)
    {
        var s = new VerticalStackLayout { Spacing = 10, HorizontalOptions = LayoutOptions.Center, Padding = new Thickness(12, 18) };
        var badge = IconBadge(icon, 64, Ds.Mint, 30);
        badge.HorizontalOptions = LayoutOptions.Center;
        s.Add(badge);
        s.Add(Txt(title, 17, true, align: TextAlignment.Center));
        s.Add(Txt(sub, 14, false, Ds.Muted, TextAlignment.Center));
        if (buttonText != null)
        {
            var b = SoftBtn(buttonText, tap);
            b.HorizontalOptions = LayoutOptions.Center;
            s.Add(b);
        }
        return s;
    }

    public static View Bar(double value, double max, Color fill = null, double h = 8)
    {
        double r = max <= 0 ? 0 : Math.Clamp(value / max, 0, 1);
        var g = new Grid { HeightRequest = h, ColumnSpacing = 0 };
        g.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(Math.Max(r, 0.0001), GridUnitType.Star)));
        g.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(Math.Max(1 - r, 0.0001), GridUnitType.Star)));
        var track = new Border { BackgroundColor = Ds.Mint, StrokeThickness = 0, StrokeShape = Round(h / 2), HeightRequest = h };
        g.Add(track, 0, 0);
        Grid.SetColumnSpan(track, 2);
        if (r > 0)
            g.Add(new Border { BackgroundColor = fill ?? Ds.Primary, StrokeThickness = 0, StrokeShape = Round(h / 2), HeightRequest = h }, 0, 0);
        return g;
    }

    /// <summary>Progress ring with a label in the middle.</summary>
    public static View RingView(double progress, string big, string small = null, double size = 130, Color fill = null)
    {
        var drawable = new RingDrawable { Progress = progress, Fill = fill ?? Ds.Primary, Thickness = size > 100 ? 12 : 8 };
        var g = new Grid { WidthRequest = size, HeightRequest = size };
        g.Add(new GraphicsView { Drawable = drawable });
        var s = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center, Spacing = 0 };
        s.Add(Txt(big, size > 100 ? 24 : 16, true, align: TextAlignment.Center));
        if (small != null) s.Add(Txt(small, 11, false, Ds.Muted, TextAlignment.Center));
        g.Add(s);
        return g;
    }

    public static GraphicsView LineChart(List<(string label, double value)> pts, string unit, double height = 190)
    {
        var d = new LineChartDrawable { Points = pts, Unit = unit };
        var gv = new GraphicsView { Drawable = d, HeightRequest = height };
        void Pick(object s, TouchEventArgs e)
        {
            if (e.Touches.Length == 0) return;
            d.Selected = d.Nearest(e.Touches[0].X, (float)gv.Width);
            gv.Invalidate();
        }
        gv.StartInteraction += Pick;
        gv.MoveInteraction += Pick;
        return gv;
    }

    public static GraphicsView BarChart(List<(string label, double value)> bars, double height = 170)
    {
        var d = new BarChartDrawable { Bars = bars };
        var gv = new GraphicsView { Drawable = d, HeightRequest = height };
        void Pick(object s, TouchEventArgs e)
        {
            if (e.Touches.Length == 0) return;
            d.Selected = d.Nearest(e.Touches[0].X, (float)gv.Width);
            gv.Invalidate();
        }
        gv.StartInteraction += Pick;
        return gv;
    }

    /// <summary>Tappable settings-style row: icon, title, optional subtitle, chevron.</summary>
    public static View MenuRow(string icon, string title, string subtitle, Func<Task> tap, bool danger = false)
    {
        var texts = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
        texts.Add(Txt(title, 16, true, danger ? Ds.Danger : Ds.Text));
        if (!string.IsNullOrEmpty(subtitle)) texts.Add(Sub(subtitle));
        var g = new Grid { ColumnSpacing = 14 };
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        g.Add(IconBadge(icon, 40, danger ? Color.FromArgb("#F5E3E0") : Ds.Mint, 20), 0, 0);
        g.Add(texts, 1, 0);
        var chev = Icon("ic_chevron", 18);
        g.Add(chev, 2, 0);
        return Card(g, pad: 14).OnTap(tap);
    }

    public static string Greeting() => DateTime.Now.Hour switch { < 12 => "Good morning", < 18 => "Good afternoon", _ => "Good evening" };
}
