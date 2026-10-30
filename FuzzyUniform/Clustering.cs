namespace FuzzyUniform;

public class ClusterResult
{
    public int[] Labels { get; init; } = Array.Empty<int>();
    public UniformFuzzy[] Centers { get; init; } = Array.Empty<UniformFuzzy>();
    public string Method { get; init; } = "";
    public double Inertia { get; init; }
    public int K => Centers.Length;

    public List<int>[] Groups()
    {
        var g = Enumerable.Range(0, K).Select(_ => new List<int>()).ToArray();
        for (int i = 0; i < Labels.Length; i++) g[Labels[i]].Add(i);
        return g;
    }
}

public static class Clustering
{
    public enum Linkage { Single, Complete, Average }

    private static double D(double[] p, double[] q) => Math.Max(Math.Abs(p[0] - q[0]), Math.Abs(p[1] - q[1]));
    private static double[][] Points(IReadOnlyList<UniformFuzzy> items) =>
        items.Select(x => new[] { x.A, x.B }).ToArray();

    public static double[,] DistanceMatrix(IReadOnlyList<UniformFuzzy> items)
    {
        int n = items.Count; var m = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++) m[i, j] = items[i].Distance(items[j]);
        return m;
    }

    private static ClusterResult Finalize(double[][] P, int[] labels, string method)
    {
        var ids = labels.Distinct().Select(l =>
        {
            var m = Enumerable.Range(0, P.Length).Where(i => labels[i] == l).ToList();
            return (l, a: m.Average(i => P[i][0]), b: m.Average(i => P[i][1]));
        }).OrderBy(t => t.a).ThenBy(t => t.b).ToList();
        var remap = ids.Select((t, idx) => (t.l, idx)).ToDictionary(t => t.l, t => t.idx);
        var lab = labels.Select(l => remap[l]).ToArray();
        var centers = new double[ids.Count][];
        for (int j = 0; j < ids.Count; j++)
        {
            var m = Enumerable.Range(0, P.Length).Where(i => lab[i] == j).ToList();
            centers[j] = new[] { m.Average(i => P[i][0]), m.Average(i => P[i][1]) };
        }
        double inertia = 0;
        for (int i = 0; i < P.Length; i++) inertia += Math.Pow(D(P[i], centers[lab[i]]), 2);
        return new ClusterResult
        {
            Labels = lab, Method = method, Inertia = inertia,
            Centers = centers.Select(c => new UniformFuzzy(c[0], c[1])).ToArray()
        };
    }

    public static ClusterResult KMeans(IReadOnlyList<UniformFuzzy> items, int k, int? seed = null, int maxIter = 100)
    {
        var P = Points(items); int n = P.Length;
        if (k < 1 || k > n) throw new ArgumentException("1 <= k <= n");
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();
        var C = new List<double[]> { (double[])P[rng.Next(n)].Clone() };
        while (C.Count < k)
        {
            var d2 = P.Select(p => Math.Pow(C.Min(c => D(p, c)), 2)).ToArray();
            double s = d2.Sum(); int idx = n - 1;
            if (s == 0) idx = rng.Next(n);
            else { double r = rng.NextDouble() * s, acc = 0; 
            for (int i = 0; i < n; i++) { acc += d2[i]; if (acc >= r) { idx = i; break; } } }
            C.Add((double[])P[idx].Clone());
        }
        var labels = Enumerable.Repeat(-1, n).ToArray(); int it;
        for (it = 1; it <= maxIter; it++)
        {
            var nw = new int[n]; var dmin = new double[n];
            for (int i = 0; i < n; i++)
            {
                double best = double.MaxValue; int bj = 0;
                for (int j = 0; j < k; j++) { double d = D(P[i], C[j]); if (d < best) { best = d; bj = j; } }
                nw[i] = bj; dmin[i] = best;
            }
            if (nw.SequenceEqual(labels)) break;
            labels = nw;
            for (int j = 0; j < k; j++)
            {
                var m = Enumerable.Range(0, n).Where(i => labels[i] == j).ToList();
                if (m.Count > 0) C[j] = new[] { m.Average(i => P[i][0]), m.Average(i => P[i][1]) };
                else C[j] = (double[])P[Array.IndexOf(dmin, dmin.Max())].Clone();
            }
        }
        var res = Finalize(P, labels, $"k-means(k={k})");
        Log.Info($"k-means: k={res.K}, итераций={it}, inertia={res.Inertia:F4}");
        return res;
    }

    public static ClusterResult Agglomerative(IReadOnlyList<UniformFuzzy> items, int? k = null, double? theta = null,
                                              Linkage linkage = Linkage.Complete)
    {
        if (k.HasValue == theta.HasValue) throw new ArgumentException("задайте ровно один из параметров: k или theta");
        var P = Points(items); int n = P.Length;
        if (k.HasValue && (k < 1 || k > n)) throw new ArgumentException("1 <= k <= n");
        var D2 = DistanceMatrix(items);
        for (int i = 0; i < n; i++) D2[i, i] = double.PositiveInfinity;
        var members = Enumerable.Range(0, n).Select(i => new List<int> { i }).ToArray();
        var size = Enumerable.Repeat(1.0, n).ToArray(); var active = Enumerable.Repeat(true, n).ToArray();
        int cnt = n;
        while (cnt > 1)
        {
            int bi = 0, bj = 1; double dmin = double.PositiveInfinity;
            for (int i = 0; i < n; i++) if (active[i])
                for (int j = i + 1; j < n; j++) if (active[j] && D2[i, j] < dmin) { dmin = D2[i, j]; bi = i; bj = j; }
            if ((k.HasValue && cnt <= k) || (theta.HasValue && dmin > theta)) break;
            active[bj] = false;
            for (int m = 0; m < n; m++)
            {
                if (!active[m] || m == bi) continue;
                double v = linkage switch
                {
                    Linkage.Single => Math.Min(D2[bi, m], D2[bj, m]),
                    Linkage.Complete => Math.Max(D2[bi, m], D2[bj, m]),
                    _ => (size[bi] * D2[bi, m] + size[bj] * D2[bj, m]) / (size[bi] + size[bj])
                };
                D2[bi, m] = D2[m, bi] = v;
            }
            members[bi].AddRange(members[bj]); size[bi] += size[bj]; cnt--;
        }
        var labels = new int[n]; int lbl = 0;
        for (int i = 0; i < n; i++) if (active[i]) { foreach (var m in members[i]) labels[m] = lbl; lbl++; }
        string tag = k.HasValue ? $"k={k}" : $"theta={theta}";
        var res = Finalize(P, labels, $"agglomerative({linkage}, {tag})");
        Log.Info($"agglomerative: {tag} -> {res.K} кластеров");
        return res;
    }

    public static double Silhouette(IReadOnlyList<UniformFuzzy> items, int[] labels)
    {
        var ks = labels.Distinct().ToList(); if (ks.Count < 2) return 0;
        var D2 = DistanceMatrix(items); int n = labels.Length; double total = 0;
        for (int i = 0; i < n; i++)
        {
            var own = Enumerable.Range(0, n).Where(j => j != i && labels[j] == labels[i]).ToList();
            if (own.Count == 0) continue;
            double a = own.Average(j => D2[i, j]);
            double b = ks.Where(l => l != labels[i]).Min(l => Enumerable.Range(0, n).Where(j => labels[j] == l).Average(j => D2[i, j]));
            double mx = Math.Max(a, b); total += mx > 0 ? (b - a) / mx : 0;
        }
        return total / n;
    }
}
