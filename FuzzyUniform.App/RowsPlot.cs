using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace FuzzyUniform.App;

public class RowsPlot : Control
{
    List<(double a, double b, int color, string t)> rows = new();
    int rowH = 40; bool labels;

    public static readonly IBrush[] Palette =
    {
        new SolidColorBrush(Color.FromRgb(31,119,180)), new SolidColorBrush(Color.FromRgb(214,39,40)),
        new SolidColorBrush(Color.FromRgb(44,160,44)),  new SolidColorBrush(Color.FromRgb(255,127,14)),
        new SolidColorBrush(Color.FromRgb(148,103,189)),new SolidColorBrush(Color.FromRgb(140,86,75)),
        new SolidColorBrush(Color.FromRgb(227,119,194)),new SolidColorBrush(Color.FromRgb(23,190,207)),
        new SolidColorBrush(Color.FromRgb(188,189,34)), new SolidColorBrush(Color.FromRgb(127,127,127)),
        new SolidColorBrush(Color.FromRgb(57,59,121)),  new SolidColorBrush(Color.FromRgb(173,73,74))
    };

    public void Set(List<(double a, double b, int color, string t)> r, int rowHeight, bool showLabels)
    {
        rows = r; rowH = rowHeight; labels = showLabels;
        Height = 15 + rows.Count * rowH + 50;
        InvalidateVisual();
    }

    static string F(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);

    static void Text(DrawingContext g, string s, double x, double y)
    {
        var ft = new FormattedText(s, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                                   Typeface.Default, 11, Brushes.Black);
        g.DrawText(ft, new Point(x, y));
    }

    public override void Render(DrawingContext g)
    {
        double w = Bounds.Width;
        g.FillRectangle(Brushes.White, new Rect(0, 0, w, Math.Max(Bounds.Height, 1)));
        if (rows.Count == 0 || w < 100) return;
        const double pad = 40;
        double lo = rows.Min(r => r.a), hi = rows.Max(r => r.b), span = hi - lo == 0 ? 1 : hi - lo;
        double sx = (w - 2 * pad) / span;
        double X(double v) => pad + (v - lo) * sx;
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i]; double y = 15 + i * rowH;
            g.FillRectangle(Palette[r.color % Palette.Length],
                new Rect(X(r.a), y, Math.Max(2, X(r.b) - X(r.a)), Math.Max(1, rowH - 2)));
            if (labels) Text(g, r.t, 2, y + rowH / 2.0 - 7);
        }
        double ya = 15 + rows.Count * rowH + 5;
        var pen = new Pen(Brushes.Black, 1);
        g.DrawLine(pen, new Point(pad, ya), new Point(w - pad, ya));
        for (int t = 0; t <= 5; t++)
        {
            double v = lo + span * t / 5;
            g.DrawLine(pen, new Point(X(v), ya), new Point(X(v), ya + 4));
            Text(g, F(v), X(v) - 14, ya + 6);
        }
    }
}
