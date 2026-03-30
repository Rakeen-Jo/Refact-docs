using System.IO.Ports;
using System.Text;

namespace V5B2_IAP_Updater;

internal sealed class YModemSender
{
    private readonly SerialPort _port;
    private readonly Action<string> _log;

    private const byte SOH = 0x01;
    private const byte STX = 0x02;
    private const byte EOT = 0x04;
    private const byte ACK = 0x06;
    private const byte NAK = 0x15;
    private const byte CAN = 0x18;
    private const byte CRC16 = (byte)'C';

    public YModemSender(SerialPort port, Action<string> logger)
    {
        _port = port;
        _log = logger;
    }

    public void SendFile(string filePath)
    {
        byte[] file = File.ReadAllBytes(filePath);
        string name = Path.GetFileName(filePath);

        _log("[YMODEM] waiting 'C'...");
        WaitByte(CRC16, 5000);

        _log("[YMODEM] send block0");
        SendBlock0(name, file.Length);
        ExpectAckThenC();

        int pktNo = 1;
        int offset = 0;
        while (offset < file.Length)
        {
            int remain = file.Length - offset;
            int size = remain >= 1024 ? 1024 : 128;
            byte[] chunk = new byte[size];
            Array.Copy(file, offset, chunk, 0, Math.Min(size, remain));
            if (remain < size)
            {
                for (int i = remain; i < size; i++) chunk[i] = 0x1A;
            }

            SendDataPacket(pktNo, chunk);
            byte r = ReadByteWithTimeout(3000);
            if (r != ACK)
                throw new Exception($"Data block {pktNo} not ACK: 0x{r:X2}");

            offset += size;
            pktNo = (pktNo + 1) & 0xFF;
        }

        _log("[YMODEM] send EOT");
        _port.Write(new[] { EOT }, 0, 1);
        ExpectAckThenC();

        _log("[YMODEM] send final empty block0");
        SendFinalEmptyBlock0();
        byte last = ReadByteWithTimeout(3000);
        if (last != ACK) throw new Exception($"Final ACK missing: 0x{last:X2}");

        _log("[YMODEM] done");
    }

    private void SendBlock0(string fileName, int fileSize)
    {
        byte[] data = new byte[128];
        var nameBytes = Encoding.ASCII.GetBytes(fileName);
        var sizeBytes = Encoding.ASCII.GetBytes(fileSize.ToString());

        int p = 0;
        Array.Copy(nameBytes, 0, data, p, Math.Min(nameBytes.Length, data.Length - 1));
        p += nameBytes.Length;
        if (p < data.Length) data[p++] = 0;
        Array.Copy(sizeBytes, 0, data, p, Math.Min(sizeBytes.Length, data.Length - p));

        SendPacket(SOH, 0x00, data);
    }

    private void SendFinalEmptyBlock0()
    {
        byte[] data = new byte[128];
        SendPacket(SOH, 0x00, data);
    }

    private void SendDataPacket(int pktNo, byte[] data)
    {
        byte start = data.Length == 1024 ? STX : SOH;
        SendPacket(start, (byte)pktNo, data);
    }

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

    private void ExpectAckThenC()
    {
        byte a = ReadByteWithTimeout(3000);
        if (a != ACK) throw new Exception($"Expected ACK, got 0x{a:X2}");
        byte c = ReadByteWithTimeout(3000);
        if (c != CRC16) throw new Exception($"Expected 'C', got 0x{c:X2}");
    }

    private void WaitByte(byte expected, int timeoutMs)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            byte b;
            try { b = ReadByteWithTimeout(200); }
            catch (TimeoutException) { continue; }
            if (b == expected) return;
            if (b == CAN) throw new Exception("YMODEM canceled by target");
        }
        throw new TimeoutException($"Timeout waiting byte 0x{expected:X2}");
    }

    private byte ReadByteWithTimeout(int timeoutMs)
    {
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
            {
                if ((crc & 0x8000) != 0)
                    crc = (ushort)((crc << 1) ^ 0x1021);
                else
                    crc <<= 1;
            }
        }
        return crc;
    }
}
