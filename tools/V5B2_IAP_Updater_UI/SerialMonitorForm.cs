using System.IO.Ports;
using System.Text;

namespace V5B2_IAP_Updater_UI;

public sealed class SerialMonitorForm : Form
{
    private readonly Func<SerialPort?> _portGetter;
    private readonly RichTextBox _rxBox = new() { Dock = DockStyle.Fill, ReadOnly = true, Font = new Font("Consolas", 9f) };
    private readonly TextBox _txBox = new() { Dock = DockStyle.Fill };
    private readonly Button _sendBtn = new() { Text = "Send", Dock = DockStyle.Fill };
    private readonly CheckBox _crlf = new() { Text = "CRLF", Checked = true, AutoSize = true };
    private readonly System.Windows.Forms.Timer _pollTimer = new() { Interval = 50 };
    private DateTime _lastTxAt = DateTime.MinValue;
    private string _lastTx = string.Empty;

    public SerialMonitorForm(Func<SerialPort?> portGetter)
    {
        _portGetter = portGetter;

        Text = "Serial Debug Monitor";
        Width = 820;
        Height = 520;
        StartPosition = FormStartPosition.CenterParent;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(8) };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        var txRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
        txRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        txRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        txRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        txRow.Controls.Add(_txBox, 0, 0);
        txRow.Controls.Add(_sendBtn, 1, 0);
        txRow.Controls.Add(_crlf, 2, 0);

        root.Controls.Add(_rxBox, 0, 0);
        root.Controls.Add(txRow, 0, 1);
        Controls.Add(root);

        _sendBtn.Click += (_, _) => SendNow();
        _txBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SendNow(); } };

        _pollTimer.Tick += (_, _) => PollSerial();
        _pollTimer.Start();
        FormClosing += (_, _) => _pollTimer.Stop();
    }

    private void PollSerial()
    {
        var p = _portGetter();
        if (p is null || !p.IsOpen) return;

        try
        {
            string incoming = string.Empty;
            lock (p)
            {
                int n = p.BytesToRead;
                if (n > 0) incoming = p.ReadExisting();
            }

            if (!string.IsNullOrEmpty(incoming))
            {
                _rxBox.AppendText(incoming);
                _rxBox.SelectionStart = _rxBox.TextLength;
                _rxBox.ScrollToCaret();
            }
        }
        catch { }
    }

    private void SendNow()
    {
        var p = _portGetter();
        if (p is null || !p.IsOpen) return;

        var text = _txBox.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        // Debounce accidental duplicated Enter/Click within short window.
        var now = DateTime.UtcNow;
        if (text == _lastTx && (now - _lastTxAt).TotalMilliseconds < 180)
            return;

        try
        {
            lock (p)
            {
                // Command-boundary preamble helps parser resync when previous line ended oddly.
                p.Write("\r");
                Thread.Sleep(4);
                p.Write(_crlf.Checked ? text + "\r\n" : text);
            }

            _lastTx = text;
            _lastTxAt = now;

            _rxBox.AppendText($"\r\n>> {text}\r\n");
            _txBox.Clear();
        }
        catch { }
    }
}
