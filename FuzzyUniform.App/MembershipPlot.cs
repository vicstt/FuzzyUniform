
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System.Globalization;

namespace FuzzyUniform.App;

public class MembershipPlot : Control
{
    private List<(FuzzyNumber Value, int Color, string Name)> items = new();
    private double alpha;

    public void Set(
        List<(FuzzyNumber Value, int Color, string Name)> values,
        double level)
    {
        items = values;
        alpha = level;
        InvalidateVisual();
    }

    public override void Render(DrawingContext g)
    {
        double w = Bounds.Width;
        double h = Bounds.Height;

        g.FillRectangle(Brushes.White, new Rect(0, 0, w, h));

        if (items.Count == 0 || w < 100 || h < 100)
            return;

        double minX = items.Min(x => x.Value.Support.Lo);
        double maxX = items.Max(x => x.Value.Support.Hi);

        double span = maxX - minX;
        if (span == 0) span = 1;

        double left = 45, right = w - 20;
        double top = 20, bottom = h - 35;

        double X(double x) => left + (x - minX) / span * (right - left);

        double Y(double mu) => bottom - mu * (bottom - top);

        var axis = new Pen(Brushes.Black, 1);

        g.DrawLine(axis, new Point(left, top), new Point(left, bottom));
        g.DrawLine(axis, new Point(left, bottom), new Point(right, bottom));

        DrawText(g, "μ(x)", 5, top);
        DrawText(g, "x", right, bottom + 5);

        for (int i = 0; i <= 5; i++)
        {
            double mu = i / 5.0;
            double y = Y(mu);
            g.DrawLine(axis, new Point(left - 4, y), new Point(left, y));
            DrawText(g, mu.ToString("0.0"), left - 35, y - 7);
        }

        for (int i = 0; i <= 7; i++)
        {
            double value = minX + span * i / 7.0;
            double x = X(value);
            g.DrawLine(axis, new Point(x, bottom), new Point(x, bottom + 4));
            DrawText(g, value.ToString("0.##"), x - 10, bottom + 7);
        }

        var alphaPen = new Pen(Brushes.Gray, 1.5, dashStyle: DashStyle.Dash);
        g.DrawLine(alphaPen, new Point(left, Y(alpha)), new Point(right, Y(alpha)));
        DrawText(g, $"α = {alpha:0.##}", left + 5, Y(alpha) - 17);

        foreach (var item in items)
        {
            var support = item.Value.Support;
            var cut = item.Value.AlphaCut(alpha);
            var pen = new Pen(RowsPlot.Palette[item.Color], 2.5);
            int steps = 300;

            Point previous = new(X(support.Lo), Y(item.Value.Mu(support.Lo)));

            for (int i = 1; i <= steps; i++)
            {
                double x = support.Lo + (support.Hi - support.Lo) * i / steps;
                Point current = new(X(x), Y(item.Value.Mu(x)));
                g.DrawLine(pen, previous, current);
                previous = current;
            }

            var cutPen = new Pen(new SolidColorBrush(Color.Parse("#333333")), 1.5, dashStyle: DashStyle.Dash);
            g.DrawLine(cutPen, new Point(X(cut.Lo), Y(0)), new Point(X(cut.Lo), Y(alpha)));
            g.DrawLine(cutPen, new Point(X(cut.Hi), Y(0)), new Point(X(cut.Hi), Y(alpha)));
            DrawText(g, item.Name, X(support.Lo) + 4, Y(item.Value.Mu(support.Lo)) - 20);
        }

        DrawText(g, "0", 20, bottom - 5);
        DrawText(g, "1", 20, top - 5);
    }

    private static void DrawText(DrawingContext g, string text, double x, double y)
    {
        var ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            Typeface.Default, 11, Brushes.Black);

        g.DrawText(ft, new Point(x, y));
    }
}
