using System.IO.Ports;

namespace V5B2_IAP_Updater_UI;

internal sealed class YModemSender
{
    private readonly SerialPort _port;
    private readonly Action<string> _log;
    private readonly Action<int> _progress;

    private const byte SOH = 0x01;
    private const byte STX = 0x02;
    private const byte EOT = 0x04;
    private const byte ACK = 0x06;
    private const byte NAK = 0x15;
    private const byte CAN = 0x18;
    private const byte CRC16 = (byte)'C';

    public YModemSender(SerialPort port, Action<string> logger, Action<int> progress)
    {
        _port = port;
        _log = logger;
        _progress = progress;
    }

    public void SendFile(string filePath, CancellationToken ct)
    {
        byte[] file = File.ReadAllBytes(filePath);
        string name = Path.GetFileName(filePath);

        _log("[YMODEM] waiting initial 'C'...");
        try { WaitByte(CRC16, 15000, ct); }
        catch (TimeoutException)
        {
            _log("[YMODEM] initial 'C' timeout; try sending block0 anyway");
        }

        bool block0Ok = false;
        Exception? block0Err = null;
        for (int i = 0; i < 3 && !block0Ok; i++)
        {
            try
            {
                _log($"[YMODEM] block0 tx try {i + 1}/3");
                SendBlock0(name, file.Length);
                ExpectAckThenC(ct);
                block0Ok = true;
            }
            catch (Exception ex)
            {
                block0Err = ex;
                _log($"[YMODEM] block0 handshake failed: {ex.Message}");
                Thread.Sleep(120);
            }
        }
        if (!block0Ok) throw new Exception("YMODEM block0 handshake failed", block0Err);

        int pktNo = 1;
        int offset = 0;
        int lastLogPct = -1;
        while (offset < file.Length)
        {
            ct.ThrowIfCancellationRequested();
            int remain = file.Length - offset;
            int size = remain >= 1024 ? 1024 : 128;
            byte[] chunk = new byte[size];
            Array.Copy(file, offset, chunk, 0, Math.Min(size, remain));
            if (remain < size)
                Array.Fill<byte>(chunk, 0x1A, remain, size - remain);

            bool acked = false;
            Exception? pktErr = null;
            for (int rtry = 0; rtry < 10 && !acked; rtry++)
            {
                try
                {
                    SendDataPacket(pktNo, chunk);
                    byte r = ReadByteWithTimeout(8000, ct);
                    if (r == ACK)
                    {
                        acked = true;
                        break;
                    }
                    if (r == NAK)
                    {
                        _log($"[YMODEM] pkt {pktNo} NAK retry {rtry + 1}/10");
                        continue;
                    }
                    pktErr = new Exception($"Data block {pktNo} unexpected resp: 0x{r:X2}");
                }
                catch (TimeoutException tex)
                {
                    pktErr = tex;
                    _log($"[YMODEM] pkt {pktNo} ACK timeout retry {rtry + 1}/10");
                }
            }
            if (!acked) throw new Exception($"Data block {pktNo} ACK failed after retries", pktErr);

            offset += size;
            pktNo = (pktNo + 1) & 0xFF;
            int pct = (int)((offset * 100L) / file.Length);
            _progress(pct);
            if (pct / 5 != lastLogPct / 5)
            {
                _log($"[YMODEM] progress {pct}%");
                lastLogPct = pct;
            }
        }

        _port.Write(new[] { EOT }, 0, 1);
        ExpectAckThenC(ct);

        SendFinalEmptyBlock0();
        byte last = ReadByteWithTimeout(6000, ct);
        if (last != ACK) throw new Exception($"Final ACK missing: 0x{last:X2}");

        _progress(100);
        _log("[YMODEM] done");
    }

    private void SendBlock0(string fileName, int fileSize)
    {
        byte[] data = new byte[128];
        var nameBytes = System.Text.Encoding.ASCII.GetBytes(fileName);
        var sizeBytes = System.Text.Encoding.ASCII.GetBytes(fileSize.ToString());
        int p = 0;
        Array.Copy(nameBytes, 0, data, p, Math.Min(nameBytes.Length, data.Length - 1));
        p += nameBytes.Length;
        if (p < data.Length) data[p++] = 0;
        Array.Copy(sizeBytes, 0, data, p, Math.Min(sizeBytes.Length, data.Length - p));
        SendPacket(SOH, 0x00, data);
    }

    private void SendFinalEmptyBlock0() => SendPacket(SOH, 0x00, new byte[128]);
    private void SendDataPacket(int pktNo, byte[] data) => SendPacket(data.Length == 1024 ? STX : SOH, (byte)pktNo, data);

    private void SendPacket(byte start, byte pktNo, byte[] data)
    {
        byte[] pkt = new byte[3 + data.Length + 2];
        pkt[0] = start;
        pkt[1] = pktNo;
        pkt[2] = (byte)~pktNo;
        Array.Copy(data, 0, pkt, 3, data.Length);
        ushort crc = Crc16(pkt, 3, data.Length);
        pkt[3 + data.Length] = (byte)((crc >> 8) & 0xFF);
        pkt[4 + data.Length] = (byte)(crc & 0xFF);
        _port.Write(pkt, 0, pkt.Length);
    }

    private void ExpectAckThenC(CancellationToken ct)
    {
        byte a = ReadByteWithTimeout(6000, ct);
        if (a != ACK) throw new Exception($"Expected ACK, got 0x{a:X2}");
        byte c = ReadByteWithTimeout(6000, ct);
        if (c != CRC16) throw new Exception($"Expected 'C', got 0x{c:X2}");
    }

    private void WaitByte(byte expected, int timeoutMs, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            ct.ThrowIfCancellationRequested();
            byte b;
            try { b = ReadByteWithTimeout(250, ct); }
            catch (TimeoutException) { continue; }
            if (b == expected) return;
            if (b == CAN) throw new Exception("YMODEM canceled by target");
        }
        throw new TimeoutException($"Timeout waiting byte 0x{expected:X2}");
    }

    private byte ReadByteWithTimeout(int timeoutMs, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        int original = _port.ReadTimeout;
        _port.ReadTimeout = timeoutMs;
        try
        {
            int v = _port.ReadByte();
            if (v < 0) throw new TimeoutException();
            return (byte)v;
        }
        finally
        {
            _port.ReadTimeout = original;
        }
    }

    private static ushort Crc16(byte[] buffer, int offset, int len)
    {
        ushort crc = 0;
        for (int i = 0; i < len; i++)
        {
            crc ^= (ushort)(buffer[offset + i] << 8);
            for (int b = 0; b < 8; b++)
                crc = (ushort)(((crc & 0x8000) != 0) ? ((crc << 1) ^ 0x1021) : (crc << 1));
        }
        return crc;
    }
}
