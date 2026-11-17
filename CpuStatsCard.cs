using System.Drawing;
using System.Windows.Forms;

namespace GpuMonitor;

/// <summary>A styled "card" for CPU statistics, matching the GpuCard style.</summary>
internal sealed class CpuStatsCard : Panel
{
    private readonly Label _loadLabel;
    private readonly Label _powerLabel;

    public CpuStatsCard()
    {
        BackColor = Color.FromArgb(34, 37, 44);
        ForeColor = Color.FromArgb(226, 228, 233);
        Font = new Font("Segoe UI", 9F);
        BorderStyle = BorderStyle.FixedSingle;
        Margin = new Padding(10, 8, 10, 8);
        AutoSize = true;

        var table = new TableLayoutPanel
        {
            AutoSize = true,
            Width = 340,
            ColumnCount = 2,
            Padding = new Padding(12, 8, 12, 12),
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70f));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));

        for (int i = 0; i < 4; i++)
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var loadTitle = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Text = "CPU Load",
            ForeColor = Color.FromArgb(120, 190, 255),
        };

        _loadLabel = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
        };

        var powerTitle = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Text = "CPU Power",
            ForeColor = Color.FromArgb(120, 190, 255),
        };

        _powerLabel = new Label
        {
            AutoSize = true,
            ForeColor = Color.FromArgb(170, 175, 185),
        };

        table.Controls.Add(loadTitle, 0, 0);
        table.Controls.Add(_loadLabel, 1, 0);

        table.Controls.Add(powerTitle, 0, 1);
        table.Controls.Add(_powerLabel, 1, 1);

        Controls.Add(table);
    }

    public void Update(float load, float power)
    {
        _loadLabel.Text = $"{load:F1}%";
        _powerLabel.Text = $"{power:F1} W";
    }
}
