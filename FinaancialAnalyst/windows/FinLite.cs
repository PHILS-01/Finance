using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

static class Analyzer
{
    public static readonly string[][] Fields = {
        new[]{"sales","Net sales"}, new[]{"cogs","Cost of sales"}, new[]{"admin","Admin expenses"},
        new[]{"fin","Finance cost"}, new[]{"other","Other income/(expense), net"}, new[]{"tax","Tax"},
        new[]{"dep","Depreciation + amortisation"}, new[]{"ca","Current assets"}, new[]{"cl","Current liabilities"},
        new[]{"ta","Total assets"}, new[]{"eq","Equity"}, new[]{"debt","Borrowings (debt)"}, new[]{"cash","Cash"},
        new[]{"rec","Trade receivables"}, new[]{"shares","Shares (thousands)"}, new[]{"cfo","Operating cash flow"} };

    public class M
    {
        public string Name; public char Kind; public Func<Dictionary<string, double>, double> F;
        public M(string n, char k, Func<Dictionary<string, double>, double> f) { Name = n; Kind = k; F = f; }
    }

    static double G(Dictionary<string, double> m, string k) { double v; return m.TryGetValue(k, out v) ? v : 0; }
    static double D(double a, double b) { return b == 0 ? double.NaN : a / b; }
    static double Gp(Dictionary<string, double> m) { return G(m, "sales") - G(m, "cogs"); }
    static double Pbt(Dictionary<string, double> m) { return Gp(m) - G(m, "admin") - G(m, "fin") + G(m, "other"); }
    static double Np(Dictionary<string, double> m) { return Pbt(m) - G(m, "tax"); }
    static double Ebitda(Dictionary<string, double> m) { return Pbt(m) + G(m, "fin") + G(m, "dep"); }
    static double Nd(Dictionary<string, double> m) { return G(m, "debt") - G(m, "cash"); }

    public static readonly M[] Metrics = {
        new M("Gross margin", '%', m => D(Gp(m), G(m,"sales")) * 100),
        new M("Net margin", '%', m => D(Np(m), G(m,"sales")) * 100),
        new M("Profit before tax", 'n', m => Pbt(m)),
        new M("EBITDA", 'n', m => Ebitda(m)),
        new M("EBITDA margin", '%', m => D(Ebitda(m), G(m,"sales")) * 100),
        new M("Interest cover (EBIT/finance)", 'x', m => D(Pbt(m) + G(m,"fin"), G(m,"fin"))),
        new M("Current ratio", 'x', m => D(G(m,"ca"), G(m,"cl"))),
        new M("Gearing (net debt/(net debt+equity))", '%', m => D(Nd(m), Nd(m) + G(m,"eq")) * 100),
        new M("Net debt / EBITDA", 'x', m => D(Nd(m), Ebitda(m))),
        new M("Return on equity", '%', m => D(Np(m), G(m,"eq")) * 100),
        new M("Return on assets", '%', m => D(Np(m), G(m,"ta")) * 100),
        new M("EPS", 'e', m => D(Np(m), G(m,"shares"))),
        new M("Receivable days", 'n', m => D(G(m,"rec"), G(m,"sales")) * 365),
        new M("Cash conversion (CFO/EBITDA)", '%', m => D(G(m,"cfo"), Ebitda(m)) * 100) };

    static string Fmt(double v, char k)
    {
        if (double.IsNaN(v) || double.IsInfinity(v)) return "n/a";
        CultureInfo c = CultureInfo.InvariantCulture;
        if (k == '%') return v.ToString("0.0", c) + "%";
        if (k == 'x') return v.ToString("0.00", c) + "x";
        if (k == 'e') return v.ToString("0.00", c);
        return v.ToString("N0", c);
    }

    static double V(string prefix, Dictionary<string, double> m)
    {
        foreach (M x in Metrics) if (x.Name.StartsWith(prefix)) return x.F(m);
        return double.NaN;
    }

    public static string Report(Dictionary<string, double> cur, Dictionary<string, double> pri)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(string.Format("{0,-40}{1,16}{2,16}{3,12}", "Metric", "Current", "Prior", "Change"));
        sb.AppendLine(new string('-', 84));
        foreach (M x in Metrics)
        {
            double a = x.F(cur), b = x.F(pri); string ch;
            if (pri.Count == 0 || double.IsNaN(b)) ch = "n/a";
            else if (x.Kind == '%' || x.Kind == 'x') ch = (a - b >= 0 ? "+" : "") + Fmt(a - b, x.Kind);
            else if (b == 0) ch = "n/a";
            else ch = ((a - b) / Math.Abs(b) * 100).ToString("+0.0;-0.0", CultureInfo.InvariantCulture) + "%";
            sb.AppendLine(string.Format("{0,-40}{1,16}{2,16}{3,12}", x.Name, Fmt(a, x.Kind), pri.Count == 0 ? "-" : Fmt(b, x.Kind), ch));
        }
        sb.AppendLine(); sb.AppendLine("Flags (current period)");
        List<string> f = new List<string>();
        if (V("Current ratio", cur) < 1.1) f.Add("Liquidity thin: current ratio below 1.1x");
        if (V("Interest cover", cur) < 2) f.Add("Interest cover below 2x");
        if (V("Gearing", cur) > 65) f.Add("Gearing above 65%");
        if (V("Receivable days", cur) > 60) f.Add("Receivables above 60 days of sales");
        if (V("Cash conversion", cur) < 50) f.Add("Less than half of EBITDA converts to operating cash");
        if (V("Net debt", cur) > 4) f.Add("Net debt above 4x EBITDA");
        if (f.Count == 0) sb.AppendLine("None raised"); else foreach (string s in f) sb.AppendLine("- " + s);
        return sb.ToString();
    }

    static Dictionary<string, double> Make(double[] v)
    {
        Dictionary<string, double> d = new Dictionary<string, double>();
        for (int i = 0; i < Fields.Length; i++) d[Fields[i][0]] = v[i];
        return d;
    }
    // Atlas Power Limited FY2011 / FY2010, Rs '000 (borrowings exclude murabaha)
    public static Dictionary<string, double> SampleCur() { return Make(new double[] { 21389781, 16922170, 124341, 2970526, -5955, 11, 804450, 8198366, 7905026, 26808396, 6644649, 16300310, 164083, 7013598, 474000, 1515078 }); }
    public static Dictionary<string, double> SamplePri() { return Make(new double[] { 11212576, 9056064, 66189, 1506534, -41312, 0, 413830, 6839897, 6587887, 25755519, 5277871, 17368422, 215974, 5528786, 474000, -4576277 }); }
}

class MainForm : Form
{
    Dictionary<string, TextBox> cur = new Dictionary<string, TextBox>();
    Dictionary<string, TextBox> pri = new Dictionary<string, TextBox>();
    TextBox outBox;
    string store = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Path.Combine("FinLite", "inputs.txt"));

    public MainForm()
    {
        Text = "FinLite - financial statement analyser"; Size = new Size(1100, 720); Font = new Font("Segoe UI", 9.5f);
        outBox = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, Dock = DockStyle.Fill, Font = new Font("Consolas", 10f), BackColor = Color.White };
        Panel left = new Panel { Dock = DockStyle.Left, Width = 440 };
        Panel scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        Panel bar = new Panel { Dock = DockStyle.Bottom, Height = 48 };
        TableLayoutPanel t = new TableLayoutPanel { AutoSize = true, ColumnCount = 3, Dock = DockStyle.Top, Padding = new Padding(8) };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190)); t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110)); t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        t.Controls.Add(new Label { Text = "Item (one unit, e.g. Rs '000)", AutoSize = true });
        t.Controls.Add(new Label { Text = "Current", AutoSize = true }); t.Controls.Add(new Label { Text = "Prior", AutoSize = true });
        foreach (string[] f in Analyzer.Fields)
        {
            t.Controls.Add(new Label { Text = f[1], AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 3, 3) });
            TextBox a = new TextBox { Width = 100 }, b = new TextBox { Width = 100 };
            cur[f[0]] = a; pri[f[0]] = b; t.Controls.Add(a); t.Controls.Add(b);
        }
        scroll.Controls.Add(t);
        Button go = new Button { Text = "Analyse", Left = 8, Top = 8, Width = 100, Height = 30 };
        Button sm = new Button { Text = "Load sample", Left = 116, Top = 8, Width = 100, Height = 30 };
        Button cl = new Button { Text = "Clear", Left = 224, Top = 8, Width = 100, Height = 30 };
        Button sv = new Button { Text = "Save report", Left = 332, Top = 8, Width = 100, Height = 30 };
        go.Click += (s, e) => Run();
        sm.Click += (s, e) => { Fill(cur, Analyzer.SampleCur()); Fill(pri, Analyzer.SamplePri()); Run(); };
        cl.Click += (s, e) => { foreach (TextBox x in cur.Values) x.Text = ""; foreach (TextBox x in pri.Values) x.Text = ""; outBox.Text = ""; };
        sv.Click += (s, e) => { SaveFileDialog d = new SaveFileDialog { Filter = "Text|*.txt", FileName = "FinLite-report.txt" }; if (d.ShowDialog() == DialogResult.OK) File.WriteAllText(d.FileName, outBox.Text); };
        bar.Controls.AddRange(new Control[] { go, sm, cl, sv });
        left.Controls.Add(scroll); left.Controls.Add(bar);
        Controls.Add(outBox); Controls.Add(left);
        LoadInputs();
    }

    static void Fill(Dictionary<string, TextBox> m, Dictionary<string, double> v)
    {
        foreach (KeyValuePair<string, double> kv in v) m[kv.Key].Text = kv.Value.ToString("0", CultureInfo.InvariantCulture);
    }

    static Dictionary<string, double> Read(Dictionary<string, TextBox> m)
    {
        Dictionary<string, double> d = new Dictionary<string, double>();
        foreach (KeyValuePair<string, TextBox> kv in m)
        {
            double v;
            if (double.TryParse(kv.Value.Text.Replace(",", "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v)) d[kv.Key] = v;
        }
        return d;
    }

    void Run()
    {
        Dictionary<string, double> c = Read(cur), p = Read(pri);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(store));
            List<string> lines = new List<string>();
            foreach (KeyValuePair<string, TextBox> kv in cur) lines.Add("c_" + kv.Key + "=" + kv.Value.Text);
            foreach (KeyValuePair<string, TextBox> kv in pri) lines.Add("p_" + kv.Key + "=" + kv.Value.Text);
            File.WriteAllLines(store, lines.ToArray());
        }
        catch (Exception) { }
        outBox.Text = c.Count == 0 ? "Enter current-period figures first." : Analyzer.Report(c, p);
    }

    void LoadInputs()
    {
        try
        {
            if (!File.Exists(store)) return;
            foreach (string l in File.ReadAllLines(store))
            {
                int i = l.IndexOf('='); if (i < 3) continue;
                string k = l.Substring(2, i - 2), v = l.Substring(i + 1);
                if (l.StartsWith("c_") && cur.ContainsKey(k)) cur[k].Text = v;
                if (l.StartsWith("p_") && pri.ContainsKey(k)) pri[k].Text = v;
            }
        }
        catch (Exception) { }
    }

    [STAThread]
    static void Main() { Application.EnableVisualStyles(); Application.Run(new MainForm()); }
}
