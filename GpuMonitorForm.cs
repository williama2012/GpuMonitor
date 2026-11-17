using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.Win32;

namespace GpuMonitor;

/// <summary>Main window: enumerates every video card and refreshes live NVML data on a timer.</summary>
public sealed class GpuMonitorForm : Form
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly FlowLayoutPanel _host;
    private readonly Label _header;
    private readonly GpuReader _reader = new();
    private readonly CpuMonitor _cpuMonitor = new();
    private readonly List<GpuCard> _cards = new();
    private readonly Button _configBtn;

    private readonly CpuStatsCard _cpuStatsCard;

    private static string? _mongoUri;
    private static string? _dbName;
    private static string? _collectionName;

    public GpuMonitorForm()
    {
        Text = "NVIDIA GPU Monitor";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new System.Drawing.Size(600, 900);
        BackColor = Color.FromArgb(24, 26, 32);
        ForeColor = Color.FromArgb(226, 228, 233);
        Font = new Font("Segoe UI", 9F);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        SizeGripStyle = SizeGripStyle.Hide;
        MinimizeBox = false;
        MaximizeBox = false;
        
        var topBar = new Panel
        {
            Height = 70,
            Width = this.Width,
            BackColor = Color.FromArgb(24, 26, 32),
        };

        _header = new Label
        {
            Dock = DockStyle.Top,
            Height = 34,
            Width = this.Width,
            Padding = new Padding(12, 8, 12, 4),
            Margin = new Padding(0,0,0,8),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(150, 170, 255),
            Text = "Reading GPU…"
        };

        _configBtn = new Button
        {
            Text = "⚙",
            Dock = DockStyle.Top,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(150, 170, 255),
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(10),
            Font = new Font("Segoe UI", 12F)
        };
        _configBtn.Click += (s, e) => new ConfigForm().ShowDialog();

        topBar.Controls.Add(_header);
        topBar.Controls.Add(_configBtn);

        _cpuStatsCard = new CpuStatsCard();

        _host = new FlowLayoutPanel
        {
            AutoScroll = true,
            BackColor = Color.Transparent,
            Padding = new Padding(12),
            Height = this.Height,
            Width = this.Width
        };

        _host.Controls.Add(_cpuStatsCard);

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            ColumnStyles = { new ColumnStyle(SizeType.Percent, 100f) },
            RowStyles = { new RowStyle(SizeType.Absolute, 70f), new RowStyle(SizeType.Percent, 100f) }
        };

        mainLayout.Controls.Add(topBar, 0, 0);
        mainLayout.Controls.Add(_host, 0, 1);

        this.Controls.Add(mainLayout);

        _timer = new System.Windows.Forms.Timer { Interval = 1000 };
        _timer.Tick += OnTick;

        LoadConfig();
    }

    private static void LoadConfig()
    {
        var key = Registry.CurrentUser.OpenSubKey(@"Software\GpuMonitor");
        if (key != null)
        {
            _mongoUri = key.GetValue("MONGODB_URI")?.ToString();
            _dbName = key.GetValue("DB_Name")?.ToString();
            _collectionName = key.GetValue("COLLECTION_NAME")?.ToString();
            key.Close();
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _timer.Start();
        OnTick(this, EventArgs.Empty); // paint first, don't wait 700ms
    }

    private async void OnTick(object? sender, EventArgs e)
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
        {
            _cards[i].Update(gpus[i]);
        }

        _header.Text = $"NVIDIA   ·   {gpus.Count} GPU(s)   ·   NVML   ·   updated {DateTime.Now:hh:mm:ss tt}";

        var (cpuLoad, cpuPower) = _cpuMonitor.GetStats();
        _cpuStatsCard.Update(cpuLoad, cpuPower);

        // Log to MongoDB
        if (!string.IsNullOrEmpty(_mongoUri) && !string.IsNullOrEmpty(_dbName) && !string.IsNullOrEmpty(_collectionName))
        {
            try
            {
                var client = new MongoClient(_mongoUri);
                var database = client.GetDatabase(_dbName);
                var collection = database.GetCollection<BsonDocument>(_collectionName);

                var entry = new BsonDocument
                {
                    { "timestamp", DateTime.UtcNow },
                    { "cpu_load", cpuLoad },
                    { "cpu_power", cpuPower },
                    { "gpus", new BsonArray { } }
                };

                foreach (var gpu in gpus)
                {
                    ((BsonArray)entry["gpus"]).Append(new BsonDocument
                    {
                        { "core_load", gpu.CoreLoad },
                        { "core_power", gpu.Power },
                        { "vram_used", (long)gpu.VramUsed },
                        { "vram_load", gpu.VramLoad }
                    });
                }

                await collection.InsertOneAsync(entry);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MongoDB logging failed: {ex.Message}");
            }
        }
    }


    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Stop();
        _reader.Dispose();
        _cpuMonitor.Dispose();
        base.OnFormClosed(e);
    }
}
