using System.IO.Ports;
using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices;

namespace V5B2_IAP_Updater_UI;

public class MainForm : Form
{
    private readonly ComboBox _cbPort = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _cbBaud = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _tbFile = new() { ReadOnly = true };
    private readonly Button _btnRefresh = new() { Text = "Refresh" };
    private readonly Button _btnConnect = new() { Text = "Open Port" };
    private readonly Button _btnMonitor = new() { Text = "Debug Monitor" };
    private readonly Button _btnBrowse = new() { Text = "Browse BIN" };
    private readonly Button _btnStart = new() { Text = "Start Download" };
    private readonly Button _btnCancel = new() { Text = "Cancel", Enabled = false };
    private readonly ProgressBar _progress = new() { Minimum = 0, Maximum = 100 };
    private readonly TextBox _tbLog = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };

    private CancellationTokenSource? _cts;
    private SerialPort? _openedPort;

    private const string Password = "wonik1234";
    private const string ResetCmd = "RESET";

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "V5B2_IAP_Updater_UI",
        "settings.json");

    private sealed class AppSettings
    {
        public string? LastBinPath { get; set; }
    }

    public MainForm()
    {
        Text = "V5B2 IAP Updater";
        Width = 980;
        Height = 640;
        MinimumSize = new Size(900, 560);
        StartPosition = FormStartPosition.CenterScreen;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(0),
            AutoSize = false
        };

        var header = new Panel { Dock = DockStyle.Fill, Height = 72, BackColor = Color.White };
        var logoPath = Path.Combine(AppContext.BaseDirectory, "assets", "wonik_logo.png");
        if (File.Exists(logoPath))
        {
            var pic = new PictureBox
            {
                Image = Image.FromFile(logoPath),
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(12, 8),
                Size = new Size(220, 56)
            };
            header.Controls.Add(pic);
        }
        var title = new Label
        {
            Text = "V5B2 IAP Updater",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 56, 140),
            AutoSize = true,
            Location = new Point(250, 22)
        };
        header.Controls.Add(title);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 5,
            Padding = new Padding(12),
            AutoSize = false
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var lblPort = new Label { Text = "Serial Port", AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft };
        var lblBaud = new Label { Text = "Baud", AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft };
        var lblBin = new Label { Text = "Firmware BIN", AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft };

        _cbPort.Dock = DockStyle.Fill;
        _cbBaud.Dock = DockStyle.Fill;
        _tbFile.Dock = DockStyle.Fill;
        _btnRefresh.Dock = DockStyle.Fill;
        _btnConnect.Dock = DockStyle.Fill;
        _btnMonitor.Dock = DockStyle.Fill;
        _btnBrowse.Dock = DockStyle.Fill;
        _btnStart.Dock = DockStyle.Fill;
        _btnCancel.Dock = DockStyle.Fill;
        _progress.Dock = DockStyle.Fill;
        _tbLog.Dock = DockStyle.Fill;
        _tbLog.Font = new Font("Consolas", 9f);

        layout.Controls.Add(lblPort, 0, 0);
        layout.Controls.Add(_cbPort, 1, 0);
        layout.Controls.Add(_btnRefresh, 2, 0);
        layout.Controls.Add(_btnConnect, 3, 0);

        layout.Controls.Add(lblBaud, 0, 1);
        layout.Controls.Add(_cbBaud, 1, 1);
        layout.Controls.Add(_btnMonitor, 2, 1);

        layout.Controls.Add(lblBin, 0, 2);
        layout.Controls.Add(_tbFile, 1, 2);
        layout.SetColumnSpan(_tbFile, 2);
        layout.Controls.Add(_btnBrowse, 3, 2);

        layout.Controls.Add(_btnStart, 1, 3);
        layout.Controls.Add(_btnCancel, 2, 3);
        layout.Controls.Add(_progress, 3, 3);

        layout.Controls.Add(_tbLog, 0, 4);
        layout.SetColumnSpan(_tbLog, 4);
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(header, 0, 0);
        root.Controls.Add(layout, 0, 1);

        Controls.Add(root);

        _cbBaud.Items.AddRange(new object[] { "921600", "460800", "2000000", "115200" });
        _cbBaud.SelectedIndex = 0;

        _btnRefresh.Click += (_, _) => RefreshPorts();
        _btnConnect.Click += (_, _) => TogglePortOpenClose();
        _btnMonitor.Click += (_, _) => OpenMonitor();
        _btnBrowse.Click += (_, _) => BrowseBin();
        _btnStart.Click += async (_, _) => await StartAsync();
        _btnCancel.Click += (_, _) => _cts?.Cancel();
        FormClosing += (_, _) => CloseOpenedPort();

        RefreshPorts();
        LoadSettings();
        ApplyBrandIcon();
    }

    private void Ui(Action action)
    {
        if (IsDisposed) return;
        if (InvokeRequired) BeginInvoke(action);
        else action();
    }

    private void RefreshPorts()
    {
        _cbPort.Items.Clear();
        foreach (var p in SerialPort.GetPortNames().OrderBy(x => x)) _cbPort.Items.Add(p);
        if (_cbPort.Items.Count > 0) _cbPort.SelectedIndex = 0;
    }

    private void OpenMonitor()
    {
        var mon = new SerialMonitorForm(() => _openedPort);
        mon.Show(this);
    }

    private void TogglePortOpenClose()
    {
        if (_openedPort is { IsOpen: true })
        {
            CloseOpenedPort();
            Log("[PORT] closed.");
            return;
        }

        if (_cbPort.SelectedItem is null) { Log("[ERR] Select COM port."); return; }
        if (!int.TryParse(_cbBaud.Text, out int baud)) { Log("[ERR] Invalid baud."); return; }

        try
        {
            var p = new SerialPort((string)_cbPort.SelectedItem, baud, Parity.None, 8, StopBits.One)
            {
                ReadTimeout = 80,
                WriteTimeout = 1000,
                Encoding = Encoding.ASCII,
                NewLine = "\r\n"
            };
            p.Open();
            p.DiscardInBuffer();
            p.DiscardOutBuffer();
            _openedPort = p;
            _btnConnect.Text = "Close Port";
            Log($"[PORT] opened {_openedPort.PortName} @ {baud}");
        }
        catch (Exception ex)
        {
            Log("[ERR] Open failed (already in use?): " + ex.Message);
        }
    }

    private void CloseOpenedPort()
    {
        try
        {
            if (_openedPort is not null)
            {
                if (_openedPort.IsOpen) _openedPort.Close();
                _openedPort.Dispose();
                _openedPort = null;
            }
        }
        catch { }
        Ui(() => _btnConnect.Text = "Open Port");
    }

    private void AutoEnterIap(SerialPort port, CancellationToken ct)
    {
        // Pre-clean line noise / partial tokens before sending real commands.
        try { port.DiscardInBuffer(); } catch { }
        Send(port, "\r\n");
        Thread.Sleep(20);
        Send(port, "\r\n");
        Thread.Sleep(20);

        string cmd = ResetCmd;
        Send(port, cmd + "\r\n");
        Thread.Sleep(350);
        Log($"[IAP] reset cmd sent: {cmd}");

        // Watch boot text and inject SPACE on "Booting" prompt.
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var sb = new StringBuilder();
        long nextSpaceMs = 0;

        while (sw.ElapsedMilliseconds < 7000)
        {
            ct.ThrowIfCancellationRequested();

            if (sw.ElapsedMilliseconds >= nextSpaceMs)
            {
                Send(port, " ");
                nextSpaceMs = sw.ElapsedMilliseconds + 150;
            }

            try
            {
                int b = port.ReadByte();
                if (b < 0) continue;
                sb.Append((char)b);
                if (sb.Length > 4000) sb.Remove(0, sb.Length - 2000);

                string s = sb.ToString();
                if (s.Contains("Booting", StringComparison.OrdinalIgnoreCase) ||
                    s.Contains("Serial KEY [space] pressed", StringComparison.OrdinalIgnoreCase) ||
                    s.Contains("Input Password", StringComparison.OrdinalIgnoreCase) ||
                    s.Contains("Main Menu", StringComparison.OrdinalIgnoreCase) ||
                    s.Contains("Waiting for the file", StringComparison.OrdinalIgnoreCase))
                {
                    Log("[IAP] boot/menu text detected.");
                    return;
                }
            }
            catch (TimeoutException) { }
        }

        Log("[IAP] auto-enter window elapsed (continuing with token wait)...");
    }

    private void BrowseBin()
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Binary (*.bin)|*.bin|All files (*.*)|*.*",
            Title = "Select firmware binary"
        };
        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            _tbFile.Text = ofd.FileName;
            SaveSettings();
        }
    }

    private async Task StartAsync()
    {
        if (_openedPort is null || !_openedPort.IsOpen)
        {
            Log("[ERR] Open COM port first.");
            return;
        }
        if (string.IsNullOrWhiteSpace(_tbFile.Text) || !File.Exists(_tbFile.Text))
        {
            Log("[ERR] Select BIN file.");
            return;
        }

        ToggleUi(false);
        _progress.Value = 0;
        _cts = new CancellationTokenSource();

        try
        {
            string file = _tbFile.Text;
            SaveSettings();
            await Task.Run(() => RunUpdateWithRetry(_openedPort!, file, _cts.Token));
            Log("[OK] Update completed.");
            _progress.Value = 100;
        }
        catch (OperationCanceledException)
        {
            Log("[WARN] Canceled.");
        }
        catch (Exception ex)
        {
            Log("[ERR] " + ex.Message);
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            ToggleUi(true);
        }
    }

    private void RunUpdateWithRetry(SerialPort port, string filePath, CancellationToken ct)
    {
        const int maxAttempts = 3;
        Exception? last = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                Log($"[IAP] attempt {attempt}/{maxAttempts}");
                RunUpdate(port, filePath, ct);
                return;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                last = ex;
                Log($"[IAP] attempt {attempt} failed: {ex.Message}");

                // Try to reset YMODEM state on target side (CAN x2)
                try { port.Write(new[] { (char)0x18, (char)0x18 }, 0, 2); } catch { }
                Thread.Sleep(300);
                try { port.DiscardInBuffer(); port.DiscardOutBuffer(); } catch { }

                if (attempt < maxAttempts)
                    Log("[IAP] retrying...");
            }
        }

        throw new Exception($"Update failed after {maxAttempts} attempts", last);
    }

    private void RunUpdate(SerialPort port, string filePath, CancellationToken ct)
    {
        lock (port)
        {
            port.DiscardInBuffer();
            port.DiscardOutBuffer();
            Log($"[IAP] using {port.PortName} @ {port.BaudRate}");

            // Auto-enter IAP: reset command + boot text watch + space injection
            AutoEnterIap(port, ct);

            Log("[IAP] waiting for password/menu/file token...");
            string first = WaitAnyContains(port, 12000, ct, "Input Password", "Main Menu", "Waiting for the file");

            if (first.Contains("Input Password", StringComparison.OrdinalIgnoreCase))
            {
                Send(port, Password + "\r");
                WaitContains(port, "Main Menu", 6000, ct);
                Send(port, "1");
                WaitContains(port, "Waiting for the file", 6000, ct);
            }
            else if (first.Contains("Main Menu", StringComparison.OrdinalIgnoreCase))
            {
                Send(port, "1");
                WaitContains(port, "Waiting for the file", 6000, ct);
            }
            // else: already waiting for file

            var y = new YModemSender(port, Log, SetProgress);
            y.SendFile(filePath, ct);

            WaitContains(port, "Programming Completed Successfully", 12000, ct);
        }
    }

    private static void Send(SerialPort port, string s) => port.Write(s);

    private void WaitContains(SerialPort port, string token, int timeoutMs, CancellationToken ct)
    {
        _ = WaitAnyContains(port, timeoutMs, ct, token);
    }

    private string WaitAnyContains(SerialPort port, int timeoutMs, CancellationToken ct, params string[] tokens)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var sb = new StringBuilder();

        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                int b = port.ReadByte();
                if (b < 0) continue;
                sb.Append((char)b);
                if (sb.Length > 16000) sb.Remove(0, sb.Length - 8000);
                string hay = sb.ToString();
                foreach (var token in tokens)
                {
                    if (hay.Contains(token, StringComparison.OrdinalIgnoreCase))
                    {
                        Log($"[IAP] token ok: {token}");
                        return token;
                    }
                }
            }
            catch (TimeoutException) { }
        }

        string tail = sb.ToString();
        if (tail.Length > 300) tail = tail[^300..];
        throw new TimeoutException($"Timeout waiting token: {string.Join(" | ", tokens)} | tail='{tail.Replace("\r", "\\r").Replace("\n", "\\n")}'");
    }

    private void ToggleUi(bool enabled)
    {
        Ui(() =>
        {
            _btnStart.Enabled = enabled;
            _btnRefresh.Enabled = enabled;
            _btnBrowse.Enabled = enabled;
            _btnConnect.Enabled = enabled;
            _btnCancel.Enabled = !enabled;
            _cbPort.Enabled = enabled;
            _cbBaud.Enabled = enabled;
        });
    }

    private void SetProgress(int value) => Ui(() => _progress.Value = Math.Clamp(value, 0, 100));

    private void Log(string msg)
    {
        Ui(() => _tbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}"));
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private void ApplyBrandIcon()
    {
        try
        {
            var logoPath = Path.Combine(AppContext.BaseDirectory, "assets", "wonik_logo.png");
            if (!File.Exists(logoPath)) return;

            using var bmp = new Bitmap(logoPath);
            IntPtr hIcon = bmp.GetHicon();
            try
            {
                using var tmp = Icon.FromHandle(hIcon);
                Icon = (Icon)tmp.Clone();
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }
        catch { }
    }

    private void SaveSettings()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(dir);
            var s = new AppSettings { LastBinPath = _tbFile.Text };
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(s));
        }
        catch { }
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath));
            if (!string.IsNullOrWhiteSpace(s?.LastBinPath))
                _tbFile.Text = s.LastBinPath;
        }
        catch { }
    }
}
