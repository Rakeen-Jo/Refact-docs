using System.IO.Ports;

namespace V5B2_IAP_Updater_UI;

internal sealed class Iap2Sender
{
    private readonly SerialPort _port;
    private readonly Action<string> _log;
    private readonly Action<int> _progress;

    private const uint MAGIC = 0x32504149; // IAP2

    private const byte HELLO = 0x01;
    private const byte META = 0x02;
    private const byte ERASE = 0x03;
    private const byte DATA = 0x04;
    private const byte COMMIT = 0x05;
    private const byte REBOOT = 0x06;

    private const byte ACK = 0x90;
    private const byte NACK = 0x91;
    private const byte DONE = 0x92;

    private ushort _seq;

    public Iap2Sender(SerialPort port, Action<string> log, Action<int> progress)
    {
        _port = port;
        _log = log;
        _progress = progress;
    }

    public void SendFile(string filePath, CancellationToken ct)
    {
        byte[] file = File.ReadAllBytes(filePath);
        uint crc = Crc32(file);

        _log("[IAP2] HELLO");
        Exchange(HELLO, Array.Empty<byte>(), ct);

        _log("[IAP2] META");
        byte[] meta = new byte[8];
        BitConverter.GetBytes((uint)file.Length).CopyTo(meta, 0);
        BitConverter.GetBytes(crc).CopyTo(meta, 4);
        Exchange(META, meta, ct);

        _log("[IAP2] ERASE");
        Exchange(ERASE, Array.Empty<byte>(), ct);

        int off = 0;
        while (off < file.Length)
        {
            ct.ThrowIfCancellationRequested();
            int n = Math.Min(256, file.Length - off);
            byte[] pl = new byte[6 + n];
            BitConverter.GetBytes((uint)off).CopyTo(pl, 0);
            BitConverter.GetBytes((ushort)n).CopyTo(pl, 4);
            Array.Copy(file, off, pl, 6, n);

            Exchange(DATA, pl, ct, retries: 5);
            off += n;
            _progress((int)(off * 100L / file.Length));
        }

        _log("[IAP2] COMMIT");
        var t = Exchange(COMMIT, Array.Empty<byte>(), ct);
        if (t != DONE && t != ACK) throw new Exception($"IAP2 commit failed type=0x{t:X2}");

        _log("[IAP2] REBOOT");
        Exchange(REBOOT, Array.Empty<byte>(), ct);
        _progress(100);
    }

    private byte Exchange(byte type, byte[] payload, CancellationToken ct, int retries = 3)
    {
        Exception? last = null;
        for (int i = 0; i < retries; i++)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                WriteFrame(type, payload);
                var (rt, _, _) = ReadFrame(ct);
                if (rt == ACK || rt == DONE) return rt;
                if (rt == NACK) throw new Exception("NACK");
                throw new Exception($"Unexpected response type 0x{rt:X2}");
            }
            catch (Exception ex)
            {
                last = ex;
                Thread.Sleep(30);
            }
        }
        throw new Exception($"IAP2 exchange failed type=0x{type:X2}", last);
    }

    private void WriteFrame(byte type, byte[] payload)
    {
        ushort len = (ushort)payload.Length;
        byte[] buf = new byte[9 + len + 2];
        BitConverter.GetBytes(MAGIC).CopyTo(buf, 0);
        buf[4] = type;
        BitConverter.GetBytes(_seq).CopyTo(buf, 5);
        BitConverter.GetBytes(len).CopyTo(buf, 7);
        if (len > 0) Array.Copy(payload, 0, buf, 9, len);

        ushort crc = Crc16(buf, 4, 5 + len);
        buf[9 + len] = (byte)(crc >> 8);
        buf[10 + len] = (byte)(crc & 0xFF);

        _port.Write(buf, 0, buf.Length);
        _seq++;
    }

    private (byte type, ushort seq, byte[] payload) ReadFrame(CancellationToken ct)
    {
        byte[] h = ReadExact(9, 3000, ct);
        uint magic = BitConverter.ToUInt32(h, 0);
        if (magic != MAGIC) throw new Exception("IAP2 bad magic");
        byte type = h[4];
        ushort seq = BitConverter.ToUInt16(h, 5);
        ushort len = BitConverter.ToUInt16(h, 7);
        byte[] tail = ReadExact(len + 2, 3000, ct);
        byte[] whole = new byte[9 + len];
        Array.Copy(h, 0, whole, 0, 9);
        if (len > 0) Array.Copy(tail, 0, whole, 9, len);

        ushort recv = (ushort)((tail[len] << 8) | tail[len + 1]);
        ushort calc = Crc16(whole, 4, 5 + len);
        if (recv != calc) throw new Exception("IAP2 bad crc");

        byte[] payload = new byte[len];
        if (len > 0) Array.Copy(tail, 0, payload, 0, len);
        return (type, seq, payload);
    }

    private byte[] ReadExact(int n, int timeoutMs, CancellationToken ct)
    {
        byte[] b = new byte[n];
        int got = 0;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (got < n)
        {
            ct.ThrowIfCancellationRequested();
            if (sw.ElapsedMilliseconds > timeoutMs) throw new TimeoutException();
            int can = _port.BytesToRead;
            if (can <= 0) { Thread.Sleep(2); continue; }
            int r = _port.Read(b, got, Math.Min(can, n - got));
            got += r;
        }
        return b;
    }

    private static ushort Crc16(byte[] data, int off, int len)
    {
        ushort crc = 0;
        for (int i = 0; i < len; i++)
        {
            crc ^= (ushort)(data[off + i] << 8);
            for (int b = 0; b < 8; b++)
                crc = (ushort)(((crc & 0x8000) != 0) ? ((crc << 1) ^ 0x1021) : (crc << 1));
        }
        return crc;
    }

    private static uint Crc32(byte[] data)
    {
        uint crc = 0xFFFFFFFFu;
        foreach (var x in data)
        {
            crc ^= x;
            for (int i = 0; i < 8; i++)
                crc = (crc >> 1) ^ (0xEDB88320u & (uint)-(int)(crc & 1));
        }
        return ~crc;
    }
}
