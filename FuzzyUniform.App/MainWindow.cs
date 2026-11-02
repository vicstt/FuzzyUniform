using System.Globalization;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace FuzzyUniform.App;

public class MainWindow : Window
{
    FuzzySet set = new();
    ClusterResult? res;
    readonly MembershipPlot membershipPlot = new() { Height = 260 };

    readonly TextBox[] tbA = { Tb("1", 70), Tb("3", 70) };
    readonly TextBox[] tbB = { Tb("2", 70), Tb("5", 70) };
    readonly TextBox calcOut = new() { IsReadOnly = true, AcceptsReturn = true, Height = 150 };
    readonly RowsPlot calcPlot = new() { Height = 200 };
    readonly TextBox tbAlpha = Tb("0.9", 60);

    readonly Dictionary<string, TextBox> prm = new();
    readonly TextBox pathBox = Tb("set.json", 220);
    readonly TextBox tbLevel = Tb("0.25", 60), tbPoint = Tb("", 80);
    readonly ListBox list = new() { FontFamily = new FontFamily("DejaVu Sans Mono, Consolas, Menlo, monospace") };
    readonly TextBlock setInfo = new() { Margin = new Avalonia.Thickness(6) };

    readonly ComboBox cbMethod = new() { Width = 260 };
    readonly ComboBox cbLink = new() { Width = 120 };
    readonly TextBox tbK = Tb("4", 50), tbTheta = Tb("8", 60);
    readonly TextBox cluOut = new() { IsReadOnly = true, AcceptsReturn = true, Height = 140 };
    readonly RowsPlot cluPlot = new();

    readonly TextBox logBox = new() { IsReadOnly = true, AcceptsReturn = true };
    readonly TextBlock status = new() { Margin = new Avalonia.Thickness(6), Foreground = Brushes.Firebrick };

    static TextBox Tb(string text, double w) => new() { Text = text, Width = w };
    static TextBlock L(string t) => new() { Text = t, VerticalAlignment = VerticalAlignment.Center };
    static string F(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);
    static double P(string? s) => double.Parse((s ?? "").Trim().Replace(',', '.'), CultureInfo.InvariantCulture);

    static Button Btn(string t, Action a, double w = 120)
    {
        var b = new Button { Content = t, Width = w };
        b.Click += (_, _) => a();
        return b;
    }

    static WrapPanel Row(params Control[] items)
    {
        var p = new WrapPanel { Margin = new Avalonia.Thickness(6) };
        foreach (var c in items) { c.Margin = new Avalonia.Thickness(3); p.Children.Add(c); }
        return p;
    }

    public MainWindow()
    {
        Title = "Нечёткие величины: равномерное распределение"; Width = 1000; Height = 760;
        Log.ToFile("fuzzy.log");
        Log.Message += s => Dispatcher.UIThread.Post(() => logBox.Text += s + "\n");

        var tabs = new TabControl();
        tabs.Items.Add(new TabItem { Header = "Калькулятор", Content = BuildCalc() });
        tabs.Items.Add(new TabItem { Header = "Множество", Content = BuildSet() });
        tabs.Items.Add(new TabItem { Header = "Кластеры", Content = BuildClu() });
        tabs.Items.Add(new TabItem { Header = "Журнал", Content = logBox });

        var root = new DockPanel();
        DockPanel.SetDock(status, Dock.Bottom);
        root.Children.Add(status);
        root.Children.Add(tabs);
        Content = root;
    }

    void Fail(Exception ex) { status.Text = "Ошибка: " + ex.Message; Log.Info("ОШИБКА: " + ex.Message); }

    Control BuildCalc()
    {
        var top = Row(L("A: a, b ="), tbA[0], tbA[1], L("   B: c, d ="), tbB[0], tbB[1], L("   α ="), tbAlpha,
            Btn("A + B", () => Calc('+'), 70), Btn("A − B", () => Calc('-'), 70),
            Btn("A × B", () => Calc('*'), 70), Btn("A ÷ B", () => Calc('/'), 70));
        var dock = new DockPanel();
        DockPanel.SetDock(top, Dock.Top); 
        DockPanel.SetDock(calcOut, Dock.Top);
        DockPanel.SetDock(membershipPlot, Dock.Top);

        dock.Children.Add(top);
        dock.Children.Add(calcOut);
        dock.Children.Add(membershipPlot);
        dock.Children.Add(calcPlot);
        return dock;
    }

    static void AddSingleCut(List<(double a, double b, int color, string t)> rows, FuzzyNumber f, int color, string name, double alpha)
    {
        var c = f.AlphaCut(alpha);

        rows.Add((c.Lo, c.Hi, color, $"{name}, α={alpha:0.##}"));
    }

    void Calc(char op)
    {
        try
        {
            status.Text = "";
            var A = new UniformFuzzy(P(tbA[0].Text), P(tbA[1].Text));
            var B = new UniformFuzzy(P(tbB[0].Text), P(tbB[1].Text));
            double al = P(tbAlpha.Text);
            FuzzyNumber R = op switch { '+' => (FuzzyNumber)(A + B), '-' => A - B, '*' => A * B, _ => A / B };
            string Cut(FuzzyNumber x) { var c = x.AlphaCut(al); return $"[{F(c.Lo)}; {F(c.Hi)}]"; }
            Log.Info($"A={A} B={B}  A{op}B: {R}; α={F(al)}: A_α={Cut(A)} B_α={Cut(B)} R_α={Cut(R)}");
            string cmp = new[] { "A<B", "A~B", "A>B" }[A.Compare(B) + 1];
            calcOut.Text =
                $"A = {A}\nB = {B}\nA {op} B: {R}\n\n" +
                $"На уровне α = {F(al)}:\n  A_α = {Cut(A)}\n  B_α = {Cut(B)}\n  (A {op} B)_α = {Cut(R)}\n\n" +
                $"Сравнение A и B:\n  Pos(A≥B)={A.PossibilityGe(B):0.000}  Nec(A≥B)={A.NecessityGe(B):0.000}\n" +
                $"  d(A≥B)={A.DegreeGe(B):0.0000}  d(B≥A)={B.DegreeGe(A):0.0000}  результат: {cmp}\n" +
                $"Расстояние Хаусдорфа d(A,B)={F(A.Distance(B))}, сходство Жаккара={A.Similarity(B):0.0000}";
            var rows = new List<(double a, double b, int color, string t)>();
            AddSingleCut(rows, A, 0, "A", al);
            AddSingleCut(rows, B, 1, "B", al);
            AddSingleCut(rows, R, 2, "R", al);
            membershipPlot.Set(new List<(FuzzyNumber Value, int Color, string Name)>
            {((FuzzyNumber)A, 0, "A"), ((FuzzyNumber)B, 1, "B"), (R, 2, "R")}, al);
            calcPlot.Set(rows, 12, true);
        }
        catch (Exception ex) { Fail(ex); }
    }

    static void AddLadder(List<(double a, double b, int color, string t)> rows, FuzzyNumber f, int color, string name)
    {
        for (int k = 10; k >= 0; k--)
        {
            var c = f.AlphaCut(k / 10.0);
            rows.Add((c.Lo, c.Hi, color, k == 10 ? name : ""));
        }
    }

    Control BuildSet()
    {
        var pr = new List<Control>();
        foreach (var (n, d) in new[] { ("n", "200"), ("lo", "0"), ("hi", "100"), ("wmin", "1"), ("wmax", "8"),
                                       ("groups", "4"), ("spread", "4"), ("seed", "1") })
        {
            var tb = Tb(d, 60); prm[n] = tb; pr.Add(L(n)); pr.Add(tb);
        }
        var top = Row(pr.ToArray());
        var bar = Row(Btn("Сгенерировать", Gen, 140), Btn("Нечёткое среднее", MeanSet, 150),
                      L("файл:"), pathBox, Btn("Загрузить", LoadSet, 100), Btn("Сохранить", SaveSet, 100));
        var lvlBar = Row(L("уровень α"), tbLevel, L("точка x"), tbPoint,
                         Btn("Показать на уровне α", ShowLevel, 200));
        var dock = new DockPanel();
        DockPanel.SetDock(top, Dock.Top); DockPanel.SetDock(bar, Dock.Top); DockPanel.SetDock(lvlBar, Dock.Top);
        DockPanel.SetDock(setInfo, Dock.Bottom);
        dock.Children.Add(top); dock.Children.Add(bar); dock.Children.Add(lvlBar); dock.Children.Add(setInfo); dock.Children.Add(list);
        return dock;
    }

    void Gen()
    {
        try
        {
            status.Text = "";
            double G(string k) => P(prm[k].Text);
            set = FuzzySet.Generate((int)G("n"), G("lo"), G("hi"), G("wmin"), G("wmax"), (int)G("groups"), G("spread"), (int)G("seed"));
            res = null; ShowSet();
        }
        catch (Exception ex) { Fail(ex); }
    }

    void LoadSet() { try { status.Text = ""; set = FuzzySet.Load(pathBox.Text ?? ""); res = null; ShowSet(); } catch (Exception ex) { Fail(ex); } }
    void SaveSet() { try { status.Text = ""; set.Save(pathBox.Text ?? ""); setInfo.Text = $"Сохранено: {pathBox.Text}"; } catch (Exception ex) { Fail(ex); } }

    void MeanSet()
    {
        if (set.Count == 0) return;
        var m = set.Mean(); Log.Info($"Нечёткое среднее = {m}"); setInfo.Text = $"Нечёткое среднее: {m}";
    }

    void ShowLevel()
    {
        try
        {
            status.Text = "";
            if (set.Count == 0) { status.Text = "Сначала создайте множество"; return; }
            double al = P(tbLevel.Text);
            bool hasX = !string.IsNullOrWhiteSpace(tbPoint.Text);
            double x = hasX ? P(tbPoint.Text) : 0;
            var cuts = set.AtLevel(al);
            var hits = hasX ? new HashSet<int>(set.ContainingPoint(x, al)) : null;
            var lines = new List<string> { $"{"№",4} {"a",10} {"b",10} {"срез на уровне α",26}" };
            foreach (var r in cuts)
            {
                if (hits != null && !hits.Contains(r.Index)) continue;
                lines.Add($"{r.Index + 1,4} {F(r.Value.A),10} {F(r.Value.B),10} {"[" + F(r.Lo) + "; " + F(r.Hi) + "]",26}");
            }
            list.ItemsSource = lines;
            setInfo.Text = hits == null
                ? $"Срезы {set.Count} величин на уровне α = {F(al)}"
                : $"На уровне α = {F(al)} точку x = {F(x)} содержат {hits.Count} из {set.Count} величин";
        }
        catch (Exception ex) { Fail(ex); }
    }

    void ShowSet()
    {
        var items = new List<string> { $"{"№",4} {"a",10} {"b",10} {"b−a",8} кластер" };
        for (int i = 0; i < set.Count; i++)
            items.Add($"{i + 1,4} {F(set[i].A),10} {F(set[i].B),10} {F(set[i].Width),8} {(res != null ? (res.Labels[i] + 1).ToString() : "")}");
        list.ItemsSource = items;
        setInfo.Text = $"Величин: {set.Count}";
    }

    Control BuildClu()
    {
        cbMethod.ItemsSource = new[] { "k-средних (k)", "Иерархический (k)", "Иерархический по критерию θ" };
        cbMethod.SelectedIndex = 0;
        cbLink.ItemsSource = new[] { "complete", "single", "average" };
        cbLink.SelectedIndex = 0;
        var top = Row(cbMethod, L("k"), tbK, L("θ"), tbTheta, cbLink, Btn("Выполнить", Cluster, 110));
        var scroll = new ScrollViewer { Content = cluPlot, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        var dock = new DockPanel();
        DockPanel.SetDock(top, Dock.Top); DockPanel.SetDock(cluOut, Dock.Top);
        dock.Children.Add(top); dock.Children.Add(cluOut); dock.Children.Add(scroll);
        return dock;
    }

    void Cluster()
    {
        if (set.Count < 2) { status.Text = "Сначала создайте множество (вкладка «Множество»)"; return; }
        try
        {
            status.Text = "";
            var it = set.Items; int k = (int)P(tbK.Text); string extra = "";
            var lk = cbLink.SelectedIndex switch
            { 0 => Clustering.Linkage.Complete, 1 => Clustering.Linkage.Single, _ => Clustering.Linkage.Average };
            ClusterResult r;
            switch (cbMethod.SelectedIndex)
            {
                case 0:
                    r = Clustering.KMeans(it, k, 0);
                    break;

                case 1:
                    r = Clustering.Agglomerative(it, k: k, linkage: lk);
                    break;

                case 2:
                    r = Clustering.Agglomerative(it, theta: P(tbTheta.Text), linkage: lk);
                    break;

                default:
                    status.Text = "Выберите метод кластеризации";
                    return;
            }
            res = r;
            double s = Clustering.Silhouette(it, r.Labels);
            var t = $"{r.Method}: кластеров = {r.K}, inertia = {r.Inertia:0.000}, силуэт = {s:0.000}\n{extra}";
            var gr = r.Groups();
            for (int j = 0; j < r.K; j++) t += $"  К{j + 1}: центр (нечёткое среднее) {r.Centers[j]}, размер {gr[j].Count}\n";
            cluOut.Text = t; Log.Info(t.Replace("\n", " | "));
            var order = Enumerable.Range(0, it.Count).OrderBy(i => r.Labels[i]).ThenBy(i => it[i].A).ToList();
            int rh = Math.Max(3, Math.Min(14, 3000 / it.Count));
            cluPlot.Set(order.Select(i => (it[i].A, it[i].B, r.Labels[i], (i + 1).ToString())).ToList(), rh, false);
            ShowSet();
        }
        catch (Exception ex) { Fail(ex); }
    }
}
