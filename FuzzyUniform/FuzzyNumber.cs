using System.Globalization;

namespace FuzzyUniform;

public sealed class FuzzyNumber
{
    private readonly Func<double, (double Lo, double Hi)> cut;
    private FuzzyNumber(Func<double, (double Lo, double Hi)> cut) => this.cut = cut;

    public static FuzzyNumber From(UniformFuzzy u) => new(a => u.AlphaCut(a));

    public (double Lo, double Hi) AlphaCut(double alpha)
    {
        if (alpha < 0 || alpha > 1) throw new ArgumentOutOfRangeException(nameof(alpha));
        return cut(alpha);
    }

    public (double Lo, double Hi) Support => cut(0.0); 
    public (double Lo, double Hi) Core => cut(1.0);     

    public double Mu(double x)
    {
        var s = Support;
        if (x < s.Lo || x > s.Hi) return 0.0;
        var k = Core;
        if (x >= k.Lo && x <= k.Hi) return 1.0;
        double lo = 0, hi = 1;
        for (int i = 0; i < 50; i++)
        {
            double mid = (lo + hi) / 2; var c = cut(mid);
            if (x >= c.Lo && x <= c.Hi) lo = mid; else hi = mid;
        }
        return lo;
    }

    public List<(double Alpha, double Lo, double Hi)> Levels(int steps = 10)
    {
        var r = new List<(double, double, double)>();
        for (int k = 0; k <= steps; k++) { double a = (double)k / steps; var c = cut(a); r.Add((a, c.Lo, c.Hi)); }
        return r;
    }

    private static (double Lo, double Hi) Mul((double Lo, double Hi) p, (double Lo, double Hi) q)
    {
        double a = p.Lo * q.Lo, b = p.Lo * q.Hi, c = p.Hi * q.Lo, d = p.Hi * q.Hi;
        return (Math.Min(Math.Min(a, b), Math.Min(c, d)), Math.Max(Math.Max(a, b), Math.Max(c, d)));
    }

    public static FuzzyNumber operator -(FuzzyNumber x) => new(a => { var p = x.cut(a); return (-p.Hi, -p.Lo); });
    public static FuzzyNumber operator +(FuzzyNumber x, FuzzyNumber y) =>
        new(a => { var p = x.cut(a); var q = y.cut(a); return (p.Lo + q.Lo, p.Hi + q.Hi); });
    public static FuzzyNumber operator -(FuzzyNumber x, FuzzyNumber y) =>
        new(a => { var p = x.cut(a); var q = y.cut(a); return (p.Lo - q.Hi, p.Hi - q.Lo); });
    public static FuzzyNumber operator *(FuzzyNumber x, FuzzyNumber y) =>
        new(a => Mul(x.cut(a), y.cut(a)));
    public static FuzzyNumber operator /(FuzzyNumber x, FuzzyNumber y)
    {
        var s = y.Support;
        if (s.Lo <= 0 && 0 <= s.Hi) throw new DivideByZeroException("носитель делителя содержит 0");
        return new(a => { var q = y.cut(a); return Mul(x.cut(a), (1.0 / q.Hi, 1.0 / q.Lo)); });
    }

    public static FuzzyNumber Min(FuzzyNumber x, FuzzyNumber y) =>
        new(a => { var p = x.cut(a); var q = y.cut(a); return (Math.Min(p.Lo, q.Lo), Math.Min(p.Hi, q.Hi)); });
    public static FuzzyNumber Max(FuzzyNumber x, FuzzyNumber y) =>
        new(a => { var p = x.cut(a); var q = y.cut(a); return (Math.Max(p.Lo, q.Lo), Math.Max(p.Hi, q.Hi)); });

    public override string ToString()
    {
        string F(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);
        var s = Support; var k = Core;
        string core = k.Lo == k.Hi ? F(k.Lo) : $"[{F(k.Lo)}; {F(k.Hi)}]";
        return $"носитель [{F(s.Lo)}; {F(s.Hi)}], ядро {core}";
    }
}
