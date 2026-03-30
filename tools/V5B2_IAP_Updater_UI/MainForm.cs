using System.IO.Ports;
using System.Text;

namespace V5B2_IAP_Updater_UI;

public class MainForm : Form
{
    private readonly ComboBox _cbPort = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _cbBaud = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _tbFile = new() { ReadOnly = true };
    private readonly Button _btnRefresh = new() { Text = "Refresh" };
    private readonly Button _btnBrowse = new() { Text = "Browse BIN" };
    private readonly Button _btnStart = new() { Text = "Start Download" };
    private readonly Button _btnCancel = new() { Text = "Cancel", Enabled = false };
    private readonly ProgressBar _progress = new() { Minimum = 0, Maximum = 100 };
    private readonly TextBox _tbLog = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };

    private CancellationTokenSource? _cts;
    private const string Password = "wonik1234";

    public MainForm()
    {
        Text = "V5B2 IAP Updater";
        Width = 980;
        Height = 640;
        MinimumSize = new Size(900, 560);
        StartPosition = FormStartPosition.CenterScreen;

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
        _btnBrowse.Dock = DockStyle.Fill;
        _btnStart.Dock = DockStyle.Fill;
        _btnCancel.Dock = DockStyle.Fill;
        _progress.Dock = DockStyle.Fill;
        _tbLog.Dock = DockStyle.Fill;
        _tbLog.Font = new Font("Consolas", 9f);

        layout.Controls.Add(lblPort, 0, 0);
        layout.Controls.Add(_cbPort, 1, 0);
        layout.Controls.Add(_btnRefresh, 2, 0);

        layout.Controls.Add(lblBaud, 0, 1);
        layout.Controls.Add(_cbBaud, 1, 1);

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

        Controls.Add(layout);

        _cbBaud.Items.AddRange(new object[] { "2000000", "115200", "921600" });
        _cbBaud.SelectedIndex = 0;

        _btnRefresh.Click += (_, _) => RefreshPorts();
        _btnBrowse.Click += (_, _) => BrowseBin();
        _btnStart.Click += async (_, _) => await StartAsync();
        _btnCancel.Click += (_, _) => _cts?.Cancel();

        RefreshPorts();
    }

    private void RefreshPorts()
    {
        _cbPort.Items.Clear();
        foreach (var p in SerialPort.GetPortNames().OrderBy(x => x)) _cbPort.Items.Add(p);
        if (_cbPort.Items.Count > 0) _cbPort.SelectedIndex = 0;
    }

    private void BrowseBin()
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Binary (*.bin)|*.bin|All files (*.*)|*.*",
            Title = "Select firmware binary"
        };
        if (ofd.ShowDialog(this) == DialogResult.OK)
            _tbFile.Text = ofd.FileName;
    }

    private async Task StartAsync()
    {
        if (_cbPort.SelectedItem is null) { Log("[ERR] Select COM port."); return; }
        if (!int.TryParse(_cbBaud.Text, out int baud)) { Log("[ERR] Invalid baud."); return; }
        if (string.IsNullOrWhiteSpace(_tbFile.Text) || !File.Exists(_tbFile.Text)) { Log("[ERR] Select BIN file."); return; }

        ToggleUi(false);
        _progress.Value = 0;
        _cts = new CancellationTokenSource();

        try
        {
            await Task.Run(() => RunUpdate((string)_cbPort.SelectedItem!, baud, _tbFile.Text, _cts.Token));
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

    private void RunUpdate(string portName, int baud, string filePath, CancellationToken ct)
    {
        using var port = new SerialPort(portName, baud, Parity.None, 8, StopBits.One)
        {
            ReadTimeout = 80,
            WriteTimeout = 1000,
            Encoding = Encoding.ASCII,
            NewLine = "\r\n"
        };

        port.Open();
        port.DiscardInBuffer();
        port.DiscardOutBuffer();
        Log($"[IAP] Open {portName} @ {baud}");

        // state machine
        Send(port, " ");
        WaitContains(port, "Input Password", 6000, ct);
        Send(port, Password + "\r");
        WaitContains(port, "Main Menu", 6000, ct);
        Send(port, "1");
        WaitContains(port, "Waiting for the file", 6000, ct);

        var y = new YModemSender(port, Log, SetProgress);
        y.SendFile(filePath, ct);

        WaitContains(port, "Programming Completed Successfully", 12000, ct);
    }

    private static void Send(SerialPort port, string s) => port.Write(s);

    private void WaitContains(SerialPort port, string token, int timeoutMs, CancellationToken ct)
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
                if (sb.ToString().Contains(token, StringComparison.OrdinalIgnoreCase))
                {
                    Log($"[IAP] token ok: {token}");
                    return;
                }
            }
            catch (TimeoutException) { }
        }

        throw new TimeoutException($"Timeout waiting token: {token}");
    }

    private void ToggleUi(bool enabled)
    {
        if (InvokeRequired) { BeginInvoke(() => ToggleUi(enabled)); return; }
        _btnStart.Enabled = enabled;
        _btnRefresh.Enabled = enabled;
        _btnBrowse.Enabled = enabled;
        _btnCancel.Enabled = !enabled;
        _cbPort.Enabled = enabled;
        _cbBaud.Enabled = enabled;
    }

    private void SetProgress(int value)
    {
        if (InvokeRequired) { BeginInvoke(() => SetProgress(value)); return; }
        _progress.Value = Math.Clamp(value, 0, 100);
    }

    private void Log(string msg)
    {
        if (InvokeRequired) { BeginInvoke(() => Log(msg)); return; }
        _tbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
    }
}
