namespace FitSphere.UI;

public class RingDrawable : IDrawable
{
    public double Progress;
    public Color Track = Ds.Mint;
    public Color Fill = Ds.Primary;
    public float Thickness = 12;

    public void Draw(ICanvas c, RectF r)
    {
        float cx = r.Center.X, cy = r.Center.Y;
        float rad = Math.Min(r.Width, r.Height) / 2 - Thickness / 2;
        c.StrokeSize = Thickness;
        c.StrokeColor = Track;
        c.DrawCircle(cx, cy, rad);

        double p = Math.Clamp(Progress, 0, 1);
        if (p <= 0.001) return;
        c.StrokeColor = Fill;
        c.StrokeLineCap = LineCap.Round;
        if (p >= 0.999) { c.DrawCircle(cx, cy, rad); return; }

        var path = new PathF();
        int steps = (int)Math.Max(4, 120 * p);
        for (int i = 0; i <= steps; i++)
        {
            double ang = -Math.PI / 2 + 2 * Math.PI * p * i / steps;
            float x = cx + rad * (float)Math.Cos(ang), y = cy + rad * (float)Math.Sin(ang);
            if (i == 0) path.MoveTo(x, y); else path.LineTo(x, y);
        }
        c.DrawPath(path);
    }
}

/// <summary>Minimal line chart (3 faint gridlines). Tap or drag to read a value.</summary>
public class LineChartDrawable : IDrawable
{
    public List<(string label, double value)> Points = new();
    public string Unit = "";
    public int Selected = -1;
    const float Left = 38, Right = 14, Top = 30, Bottom = 26;

    public int Nearest(float x, float width)
    {
        if (Points.Count == 0) return -1;
        if (Points.Count == 1) return 0;
        float w = width - Left - Right;
        return Math.Clamp((int)Math.Round((x - Left) / w * (Points.Count - 1)), 0, Points.Count - 1);
    }

    public void Draw(ICanvas c, RectF r)
    {
        if (Points.Count == 0) return;
        float w = r.Width - Left - Right, h = r.Height - Top - Bottom;
        double min = Points.Min(p => p.value), max = Points.Max(p => p.value);
        if (max - min < 1) { min -= 1; max += 1; }
        else { double pad = (max - min) * 0.2; min -= pad; max += pad; }

        c.StrokeSize = 1; c.StrokeColor = Ds.Line; c.FontColor = Ds.Muted; c.FontSize = 10;
        for (int i = 0; i < 3; i++)
        {
            float y = Top + h * i / 2f;
            c.DrawLine(Left, y, Left + w, y);
            c.DrawString((max - (max - min) * i / 2).ToString("0.#"), 0, y - 8, Left - 6, 16, HorizontalAlignment.Right, VerticalAlignment.Center);
        }

        float X(int i) => Points.Count == 1 ? Left + w / 2 : Left + w * i / (Points.Count - 1);
        float Y(double v) => Top + h * (float)((max - v) / (max - min));

        if (Points.Count > 1)
        {
            var path = new PathF();
            for (int i = 0; i < Points.Count; i++)
            {
                if (i == 0) path.MoveTo(X(i), Y(Points[i].value)); else path.LineTo(X(i), Y(Points[i].value));
            }
            c.StrokeColor = Ds.Primary; c.StrokeSize = 2.5f; c.StrokeLineCap = LineCap.Round; c.StrokeLineJoin = LineJoin.Round;
            c.DrawPath(path);
        }

        int sel = Selected >= 0 && Selected < Points.Count ? Selected : Points.Count - 1;
        for (int i = 0; i < Points.Count; i++)
        {
            if (Points.Count > 16 && i != sel) continue;
            c.FillColor = i == sel ? Ds.Primary : Colors.White;
            c.StrokeColor = Ds.Primary; c.StrokeSize = 2;
            c.FillCircle(X(i), Y(Points[i].value), i == sel ? 5 : 3.5f);
            c.DrawCircle(X(i), Y(Points[i].value), i == sel ? 5 : 3.5f);
        }

        // x labels: first and last
        c.FontColor = Ds.Muted; c.FontSize = 10;
        c.DrawString(Points.First().label, Left - 10, r.Height - Bottom + 6, 70, 16, HorizontalAlignment.Left, VerticalAlignment.Center);
        if (Points.Count > 1)
            c.DrawString(Points.Last().label, r.Width - Right - 60, r.Height - Bottom + 6, 70, 16, HorizontalAlignment.Right, VerticalAlignment.Center);

        // tooltip for the selected point
        var pt = Points[sel];
        string text = $"{pt.label}: {pt.value:0.#} {Unit}".Trim();
        float tx = Math.Clamp(X(sel) - 60, 0, r.Width - 120);
        c.FillColor = Ds.Primary;
        c.FillRoundedRectangle(tx, 2, 120, 22, (float)(Ds.ControlRadius / 2));
        c.FontColor = Colors.White; c.FontSize = 11;
        c.DrawString(text, tx, 2, 120, 22, HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}

/// <summary>Rounded bar chart; the selected (or latest) bar is highlighted.</summary>
public class BarChartDrawable : IDrawable
{
    public List<(string label, double value)> Bars = new();
    public int Selected = -1;

    public int Nearest(float x, float width)
    {
        if (Bars.Count == 0) return -1;
        return Math.Clamp((int)(x / width * Bars.Count), 0, Bars.Count - 1);
    }

    public void Draw(ICanvas c, RectF r)
    {
        if (Bars.Count == 0) return;
        float top = 22, bottom = 22;
        float h = r.Height - top - bottom;
        float slot = r.Width / Bars.Count, bw = Math.Min(slot * 0.55f, 34);
        double max = Math.Max(1, Bars.Max(b => b.value));
        int sel = Selected >= 0 && Selected < Bars.Count ? Selected : Bars.Count - 1;

        c.StrokeSize = 1; c.StrokeColor = Ds.Line;
        c.DrawLine(0, top + h, r.Width, top + h);

        for (int i = 0; i < Bars.Count; i++)
        {
            float bh = (float)(Bars[i].value / max) * h;
            if (Bars[i].value > 0) bh = Math.Max(bh, 6);
            float x = slot * i + (slot - bw) / 2, y = top + h - bh;
            c.FillColor = i == sel ? Ds.Primary : Ds.BarIdle;
            if (bh > 0) c.FillRoundedRectangle(x, y, bw, bh, 6);

            c.FontSize = 10; c.FontColor = i == sel ? Ds.Text : Ds.Muted;
            c.DrawString(Bars[i].value.ToString("0.#"), slot * i, y - 18, slot, 16, HorizontalAlignment.Center, VerticalAlignment.Center);
            c.FontColor = Ds.Muted;
            c.DrawString(Bars[i].label, slot * i - 6, top + h + 4, slot + 12, 16, HorizontalAlignment.Center, VerticalAlignment.Center);
        }
    }
}
