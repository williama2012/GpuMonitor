using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace GpuMonitor;

public sealed class ConfigForm : Form
{
    private readonly TextBox _uriEntry;
    private readonly TextBox _dbNameEntry;
    private readonly TextBox _collectionEntry;

    private const string RegistryPath = @"Software\GpuMonitor";

    public ConfigForm()
    {
        Text = "MongoDB Configuration";
        Size = new System.Drawing.Size(400, 300);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            ColumnCount = 2,
            RowCount = 4,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        _uriEntry = new TextBox { Dock = DockStyle.Fill };
        _dbNameEntry = new TextBox { Dock = DockStyle.Fill };
        _collectionEntry = new TextBox { Dock = DockStyle.Fill };

        layout.Controls.Add(new Label { Text = "URI:", Dock = DockStyle.Fill }, 0, 0);
        layout.Controls.Add(_uriEntry, 1, 0);
        layout.Controls.Add(new Label { Text = "Database Name:", Dock = DockStyle.Fill }, 0, 1);
        layout.Controls.Add(_dbNameEntry, 1, 1);
        layout.Controls.Add(new Label { Text = "Collection Name:", Dock = DockStyle.Fill }, 0, 2);
        layout.Controls.Add(_collectionEntry, 1, 2);

        var saveBtn = new Button { Text = "Save", Dock = DockStyle.Fill };
        saveBtn.Click += (s, e) => SaveConfig();
        layout.Controls.Add(saveBtn, 0, 3);
        layout.Controls.Add(new Label { Text = "", Dock = DockStyle.Fill }, 1, 3);

        Controls.Add(layout);

        LoadConfig();
    }

    private void LoadConfig()
    {
        var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
        if (key != null)
        {
            _uriEntry.Text = key.GetValue("MONGODB_URI")?.ToString() ?? "";
            _dbNameEntry.Text = key.GetValue("DB_Name")?.ToString() ?? "";
            _collectionEntry.Text = key.GetValue("COLLECTION_NAME")?.ToString() ?? "";
            key.Close();
        }
    }

    private void SaveConfig()
    {
        var key = Registry.CurrentUser.CreateSubKey(RegistryPath);
        key.SetValue("MONGODB_URI", _uriEntry.Text);
        key.SetValue("DB_Name", _dbNameEntry.Text);
        key.SetValue("COLLECTION_NAME", _collectionEntry.Text);
        key.Close();
        MessageBox.Show("Configuration saved!");
        this.Close();
    }
}
