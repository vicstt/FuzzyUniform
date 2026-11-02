namespace FuzzyUniform;

public readonly record struct UniformFuzzy
{
    public double A { get; }
    public double B { get; }

    public UniformFuzzy(double a, double b)
    {
        if (!double.IsFinite(a) || !double.IsFinite(b))
            throw new ArgumentException("a и b должны быть конечными числами");
        if (a > b) throw new ArgumentException($"требуется a <= b, получено a={a}, b={b}");
        A = a; B = b;
    }

    public double Width => B - A;

    public double Density(double x) =>
        Width == 0 ? (x == A ? double.PositiveInfinity : 0.0) : (x >= A && x <= B ? 1.0 / Width : 0.0);

    public double Mu(double x)
    {
        if (x < A || x > B) return 0.0;
        if (B == A) return 1.0;
        return (x - A) / (B - A);
    }

    public (double Lo, double Hi) AlphaCut(double alpha)
    {
        if (alpha < 0 || alpha > 1) throw new ArgumentOutOfRangeException(nameof(alpha));
        if (B == A) return (A, B);
        return (A + alpha * (B - A), B);
    }

    public static UniformFuzzy Sample(double aMin, double aMax, double wMin, double wMax, Random rng)
    {
        double a = (aMax - aMin) * rng.NextDouble() + aMin;
        double w = (wMax - wMin) * rng.NextDouble() + wMin;
        return new UniformFuzzy(a, a + w);
    }

    public static implicit operator UniformFuzzy(double x) => new(x, x);
    public static implicit operator FuzzyNumber(UniformFuzzy x) => FuzzyNumber.From(x);

    public static UniformFuzzy operator +(UniformFuzzy x, UniformFuzzy y) => new(x.A + y.A, x.B + y.B);
    public static FuzzyNumber operator -(UniformFuzzy x) => -(FuzzyNumber)x;
    public static FuzzyNumber operator -(UniformFuzzy x, UniformFuzzy y) => (FuzzyNumber)x - (FuzzyNumber)y;
    public static FuzzyNumber operator *(UniformFuzzy x, UniformFuzzy y) => (FuzzyNumber)x * (FuzzyNumber)y;
    public static FuzzyNumber operator /(UniformFuzzy x, UniformFuzzy y) => (FuzzyNumber)x / (FuzzyNumber)y;

    public FuzzyNumber FMin(UniformFuzzy o) => FuzzyNumber.Min(this, o);
    public FuzzyNumber FMax(UniformFuzzy o) => FuzzyNumber.Max(this, o);

    public double PossibilityGe(UniformFuzzy o) => B >= o.B ? 1.0 : o.Mu(B);

    public double NecessityGe(UniformFuzzy o) => 1.0 - (o.B >= B ? 1.0 : Mu(o.B));

    private static double Clip01(double x) => x < 0 ? 0 : x > 1 ? 1 : x;

    public double DegreeGe(UniformFuzzy o)
    {
        double a = A, b = B, c = o.A, d = o.B;
        if (a == b && c == d) return a >= c ? 1.0 : 0.0;
        if (a == b) return Clip01((a - c) / (d - c));
        if (c == d) return Clip01((b - c) / (b - a));
        double lo = Math.Max(a, c), hi = Math.Min(b, d);
        double part = hi > lo ? (Math.Pow(hi - c, 2) - Math.Pow(lo - c, 2)) / (2 * (d - c)) : 0.0;
        part += Math.Max(0.0, b - Math.Max(a, d));
        return Clip01(part / (b - a));
    }

    public int Compare(UniformFuzzy o, double eps = 1e-9)
    {
        double g = DegreeGe(o);
        return Math.Abs(g - 0.5) <= eps ? 0 : g > 0.5 ? 1 : -1;
    }

    public double Distance(UniformFuzzy o) => Math.Max(Math.Abs(A - o.A), Math.Abs(B - o.B));

    public double Similarity(UniformFuzzy o)
    {
        double inter = Math.Max(0.0, Math.Min(B, o.B) - Math.Max(A, o.A));
        double union = Width + o.Width - inter;
        return union == 0 ? (A == o.A ? 1.0 : 0.0) : inter / union;
    }

    public override string ToString() =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture, $"U[{A:0.####}; {B:0.####}]");
}
