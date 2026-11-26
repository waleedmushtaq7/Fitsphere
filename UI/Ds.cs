namespace FitSphere.UI;

/// <summary>Design tokens (dark "graphite" theme). Every colour, radius and spacing comes from here.</summary>
public static class Ds
{
    // ---- Surfaces ----
    public static readonly Color Bg = Color.FromArgb("#121214");      // graphite page background
    public static readonly Color Card = Color.FromArgb("#1C1C21");    // cards
    public static readonly Color Field = Color.FromArgb("#26262C");   // inputs, chips, icon buttons
    public static readonly Color Track = Color.FromArgb("#34343A");   // bar / ring tracks
    public static readonly Color Line = Color.FromArgb("#2C2C33");    // soft borders
    public static readonly Color Tint = Color.FromArgb("#2A2447");    // violet-tinted surface (icon badges, soft buttons)

    // ---- Text ----
    public static readonly Color Text = Color.FromArgb("#F5F5F7");
    public static readonly Color Muted = Color.FromArgb("#A4A4B0");   // soft grey
    public static readonly Color HeroSub = Color.FromRgba(1f, 1f, 1f, 0.78f);

    // ---- Brand / accents ----
    public static readonly Color Primary = Color.FromArgb("#6D4AFF"); // violet (buttons, selected chips)
    public static readonly Color Deep = Color.FromArgb("#4B2FD6");    // text on white pill buttons
    public static readonly Color Soft = Color.FromArgb("#C4B5FD");    // light violet (accent text, chart line)
    public static readonly Color Magenta = Color.FromArgb("#C44DFF");
    public static readonly Color Pink = Color.FromArgb("#FF6FB5");
    public static readonly Color Emerald = Color.FromArgb("#00E676");
    public static readonly Color Lime = Color.FromArgb("#9DEB4A");
    public static readonly Color Teal = Color.FromArgb("#14E3C2");
    public static readonly Color Amber = Color.FromArgb("#FFC107");
    public static readonly Color Orange = Color.FromArgb("#FF7A1A");
    public static readonly Color Cyan = Color.FromArgb("#29C7FF");
    public static readonly Color Blue = Color.FromArgb("#3B7BFF");
    public static readonly Color Rose = Color.FromArgb("#FF6B81");    // used for errors / destructive actions
    public static readonly Color Danger = Rose;
    public static readonly Color BarIdle = Color.FromArgb("#3A3365");

    /// <summary>Blue-to-violet gradient used for hero cards.</summary>
    public static Brush HeroBrush => Gradient(Color.FromArgb("#3F5BFF"), Color.FromArgb("#9A3FE6"), true);

    public static Brush Gradient(Color from, Color to, bool diagonal = false) =>
        new LinearGradientBrush(
            new GradientStopCollection { new GradientStop(from, 0f), new GradientStop(to, 1f) },
            new Point(0, 0), diagonal ? new Point(1, 1) : new Point(1, 0));

    // ---- Radii: one value per role ----
    public const double ControlRadius = 14;   // buttons, inputs, chips
    public const double CardRadius = 18;      // cards
    public const double HeroRadius = 22;      // the prominent card on a screen
    public const double PillRadius = 22;      // pill buttons (44 px high)

    public const double PageMargin = 20;
    public const double ControlHeight = 52;
}
