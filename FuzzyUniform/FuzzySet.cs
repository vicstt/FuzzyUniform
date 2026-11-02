using System.Globalization;
using System.Text.Json;

namespace FuzzyUniform;

public class FuzzySet
{
    public List<UniformFuzzy> Items { get; } = new();
    public int Count => Items.Count;
    public UniformFuzzy this[int i] => Items[i];

    public FuzzySet() { }
    public FuzzySet(IEnumerable<UniformFuzzy> items) => Items.AddRange(items);
    public void Add(UniformFuzzy x) => Items.Add(x);

    public static FuzzySet Generate(int n = 200, double lo = 0, double hi = 100, double wMin = 1, double wMax = 10,
                                    int groups = 0, double spread = 5, int? seed = null)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();
        var centers = new double[groups];
        for (int i = 0; i < groups; i++) centers[i] = (hi - lo) * rng.NextDouble() + lo;
        var set = new FuzzySet();
        for (int i = 0; i < n; i++)
        {
            double a = groups > 0
                ? centers[rng.Next(groups)] + (2 * rng.NextDouble() - 1) * spread
                : (hi - lo) * rng.NextDouble() + lo;
            double w = (wMax - wMin) * rng.NextDouble() + wMin;
            set.Add(new UniformFuzzy(a, a + w));
        }
        Log.Info($"Сгенерировано {n} величин (lo={lo} hi={hi} w=[{wMin},{wMax}] groups={groups} seed={seed})");
        return set;
    }

    public UniformFuzzy Sum()
    {
        UniformFuzzy s = 0.0;
        foreach (var x in Items) s += x;
        return s;
    }

    public UniformFuzzy Mean()
    {
        if (Items.Count == 0) throw new InvalidOperationException("пустое множество");
        var sum = Sum();  
        return new UniformFuzzy(sum.A / Items.Count, sum.B / Items.Count);
    }

    public FuzzyNumber FMin() => Items.Skip(1).Aggregate((FuzzyNumber)Items[0], (r, x) => FuzzyNumber.Min(r, x));
    public FuzzyNumber FMax() => Items.Skip(1).Aggregate((FuzzyNumber)Items[0], (r, x) => FuzzyNumber.Max(r, x));

    public List<UniformFuzzy> SortedByComparison()
    {
        var l = new List<UniformFuzzy>(Items);
        l.Sort((p, q) => p.Compare(q));
        return l;
    }

    public List<(int Index, UniformFuzzy Value, double Lo, double Hi)> AtLevel(double alpha)
    {
        var r = new List<(int, UniformFuzzy, double, double)>();
        for (int i = 0; i < Items.Count; i++) { var c = Items[i].AlphaCut(alpha); r.Add((i, Items[i], c.Lo, c.Hi)); }
        Log.Info($"Срезы {Count} величин на уровне α = {alpha}");
        return r;
    }

    public List<int> ContainingPoint(double x, double alpha)
    {
        var hits = new List<int>();
        for (int i = 0; i < Items.Count; i++)
        {
            var c = Items[i].AlphaCut(alpha);
            if (x >= c.Lo && x <= c.Hi) hits.Add(i);
        }
        string ids = string.Join(", ", hits.Take(100).Select(i => i + 1)) + (hits.Count > 100 ? ", …" : "");
        Log.Info($"Уровень α = {alpha}: точку x = {x} содержат {hits.Count} из {Count} величин (№ {ids})");
        return hits;
    }

    private record Dto(double a, double b);

    public void Save(string path)
    {
        if (path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            var lines = new List<string> { "a,b" };
            lines.AddRange(Items.Select(x => string.Create(CultureInfo.InvariantCulture, $"{x.A:R},{x.B:R}")));
            File.WriteAllLines(path, lines);
        }
        else
            File.WriteAllText(path, JsonSerializer.Serialize(Items.Select(x => new Dto(x.A, x.B)),
                new JsonSerializerOptions { WriteIndented = true }));
        Log.Info($"Записано {Count} величин в {path}");
    }

    public static FuzzySet Load(string path)
    {
        var set = new FuzzySet();
        if (path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            foreach (var line in File.ReadLines(path).Skip(1).Where(l => l.Trim().Length > 0))
            {
                var p = line.Split(',');
                set.Add(new UniformFuzzy(double.Parse(p[0], CultureInfo.InvariantCulture),
                                         double.Parse(p[1], CultureInfo.InvariantCulture)));
            }
        else
            foreach (var d in JsonSerializer.Deserialize<List<Dto>>(File.ReadAllText(path))!)
                set.Add(new UniformFuzzy(d.a, d.b));
        Log.Info($"Загружено {set.Count} величин из {path}");
        return set;
    }
}
