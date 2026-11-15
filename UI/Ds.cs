namespace FitSphere.UI;

/// <summary>Design tokens. Every colour, radius and spacing comes from here so the app stays consistent.</summary>
public static class Ds
{
    public static readonly Color Primary = Color.FromArgb("#174C3B");
    public static readonly Color Mint = Color.FromArgb("#DCEFE5");
    public static readonly Color Bg = Color.FromArgb("#F8FAF8");
    public static readonly Color Card = Colors.White;
    public static readonly Color Text = Color.FromArgb("#202923");
    public static readonly Color Muted = Color.FromArgb("#89938D");
    public static readonly Color Lime = Color.FromArgb("#B9D98C");
    public static readonly Color Line = Color.FromArgb("#E6ECE8");
    public static readonly Color Danger = Color.FromArgb("#B94A3E");
    public static readonly Color BarIdle = Color.FromArgb("#BFDCCB");

    // Radii: one value per role, never ad-hoc.
    public const double ControlRadius = 14;   // buttons, inputs, chips
    public const double CardRadius = 16;      // cards
    public const double HeroRadius = 20;      // the single prominent card on a screen

    public const double PageMargin = 20;
    public const double ControlHeight = 52;
}
