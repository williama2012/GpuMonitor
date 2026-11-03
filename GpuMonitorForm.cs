using System.Drawing;
using System.Windows.Forms;

namespace GpuMonitor;

/// <summary>Main window: enumerates every video card and refreshes live NVML data on a timer.</summary>
public sealed class GpuMonitorForm : Form
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly FlowLayoutPanel _host;
    private readonly Label _header;
    private readonly GpuReader _reader = new();
    private readonly List<GpuCard> _cards = new();

    public GpuMonitorForm()
    {
        Text = "NVIDIA GPU Monitor";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(600, 600);
        BackColor = Color.FromArgb(24, 26, 32);
        ForeColor = Color.FromArgb(226, 228, 233);
        Font = new Font("Segoe UI", 9F);
        _header = new Label
        {
            Dock = DockStyle.Top,
            Height = 34,
            Padding = new Padding(12, 8, 12, 4),
            Margin = new Padding(0,0,0,8),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(150, 170, 255),
            Text = "Reading GPU…",
        };

        _host = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Padding = new Padding(12),
        };

        // Fill control must be added LAST so docked siblings reserve their space first.
        Controls.Add(_header);
        Controls.Add(_host);

        _timer = new System.Windows.Forms.Timer { Interval = 700 };
        _timer.Tick += OnTick;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _timer.Start();
        OnTick(this, EventArgs.Empty); // paint first, don't wait 700ms
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var (ok, gpus, err) = _reader.GetGpus();

        if (!ok)
        {
            _timer.Stop();
            _header.Text = err ?? "Error";
            _header.ForeColor = Color.FromArgb(235, 120, 120);
            _host.Controls.Clear();
            var msg = new Label
            {
                AutoSize = true,
                Padding = new Padding(8),
                Text = (err ?? "Unknown error.") + "\n\nThis app reads NVIDIA GPUs via the NVML driver API.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(230, 130, 130),
            };
            _host.Controls.Add(msg);
            return;
        }

        if (gpus.Count == 0)
        {
            _timer.Stop();
            _header.Text = "No NVIDIA GPU detected.";
            _header.ForeColor = Color.FromArgb(235, 120, 120);
            _host.Controls.Clear();
            var msg = new Label
            {
                AutoSize = true,
                Padding = new Padding(8),
                Text = "NVML is initialized but reports zero devices.",
                ForeColor = Color.FromArgb(230, 130, 130),
            };
            _host.Controls.Add(msg);
            return;
        }

        // Build cards once; the device count doesn't change at runtime.
        if (_cards.Count == 0)
        {
            for (int i = 0; i < gpus.Count; i++)
            {
                var card = new GpuCard();
                _cards.Add(card);
                _host.Controls.Add(card);
            }
        }

        for (int i = 0; i < _cards.Count && i < gpus.Count; i++)
            _cards[i].Update(gpus[i]);

        _header.Text = $"NVIDIA   ·   {gpus.Count} GPU(s)   ·   NVML   ·   updated {DateTime.Now:HH:mm:ss}";
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Stop();
        _reader.Dispose();
        base.OnFormClosed(e);
    }
}
