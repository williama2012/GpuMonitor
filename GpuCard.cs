using System.Drawing;
using System.Windows.Forms;

namespace GpuMonitor;

/// <summary>One styled "card" that displays a single video card's live stats.</summary>
internal sealed class GpuCard : Panel
{
    private readonly Label _name;
    private readonly ProgressBar _vramBar;
    private readonly Label _vramText;
    private readonly Label _corePct;
    private readonly ProgressBar _coreBar;
    private readonly Label _vramLoadPct;
    private readonly ProgressBar _vramLoadBar;

    public GpuCard()
    {
        BackColor = Color.FromArgb(34, 37, 44);
        ForeColor = Color.FromArgb(226, 228, 233);
        Font = new Font("Segoe UI", 9F);
        BorderStyle = BorderStyle.FixedSingle;
        Margin = new Padding(10, 8, 10, 8);
        AutoSize = true;

        // Auto-sizing table: every row fits its content, so text can never be
        // clipped at the bottom edge regardless of DPI or font substitution.
        var table = new TableLayoutPanel
        {
            AutoSize = true,
            Width = 340,
            ColumnCount = 2,
            Padding = new Padding(12, 8, 12, 12),
        };
        // Column 0 = 70% (labels), Column 1 = 30% (right-aligned percent value).
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70f));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));

        // Rows: 0 name · 1 vramTitle · 2 vramBar · 3 vramText · 4 coreTitle · 5 coreBar · 6 vramLoadTitle · 7 vramLoadBar
        for (int i = 0; i < 8; i++)
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _name = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Text = "GPU …",
        };

        var vramTitle = SectionTitle("Video memory (VRAM)");
        _vramBar = Bar();
        _vramText = new Label
        {
            AutoSize = true,
            ForeColor = Color.FromArgb(170, 175, 185),
        };
        var coreTitle = SectionTitle("Core load");
        _corePct = PctLabel();
        _coreBar = Bar();
        var vramLoadTitle = SectionTitle("VRAM load");
        _vramLoadPct = PctLabel();
        _vramLoadBar = Bar();

        table.Controls.Add(_name, 0, 0);
        table.SetColumnSpan(_name, 2);

        table.Controls.Add(vramTitle, 0, 1);
        table.Controls.Add(_vramBar, 0, 2);
        table.SetColumnSpan(_vramBar, 2);

        table.Controls.Add(_vramText, 0, 3);
        table.SetColumnSpan(_vramText, 2);

        table.Controls.Add(coreTitle, 0, 4);
        table.Controls.Add(_corePct, 1, 4);

        table.Controls.Add(_coreBar, 0, 5);
        table.SetColumnSpan(_coreBar, 2);

        table.Controls.Add(vramLoadTitle, 0, 6);
        table.Controls.Add(_vramLoadPct, 1, 6);

        table.Controls.Add(_vramLoadBar, 0, 7);
        table.SetColumnSpan(_vramLoadBar, 2);

        Controls.Add(table);
    }

    private static Label SectionTitle(string text) => new Label
    {
        AutoSize = true,
        Text = text,
        ForeColor = Color.FromArgb(120, 190, 255),
    };

    private static Label PctLabel() => new Label
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight,
    };

    private static ProgressBar Bar() => new ProgressBar
    {
        Dock = DockStyle.Fill,
        Maximum = 100,
    };

    public void Update(GpuData g)
    {
        _name.Text = string.IsNullOrEmpty(g.Name) ? $"GPU {g.Index}" : g.Name;

        double total = GpuData.BytesToGiB(g.VramTotal);
        double used = GpuData.BytesToGiB(g.VramUsed);
        double free = GpuData.BytesToGiB(g.VramFree);
        int vramPct = total > 0 ? (int)Math.Round(100d * used / total) : 0;
        _vramBar.Value = ClampPct(vramPct);
        _vramText.Text = $"Used {used:0.0} GB   ·   Free {free:0.0} GB   (of {total:0.0} GB)";

        _corePct.Text = $"{g.CoreLoad}%";
        _coreBar.Value = ClampPct((int)g.CoreLoad);

        _vramLoadPct.Text = $"{g.VramLoad}%";
        _vramLoadBar.Value = ClampPct((int)g.VramLoad);
    }

    private static int ClampPct(int v) => Math.Clamp(v, 0, 100);
}
