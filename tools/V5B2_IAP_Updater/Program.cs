using System.IO.Ports;
using System.Text;

namespace V5B2_IAP_Updater;

internal static class Program
{
    private const string Password = "wonik1234";

    static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: V5B2_IAP_Updater <COMx> <firmware.bin> [baud=2000000]");
            return 1;
        }

        string portName = args[0];
        string filePath = args[1];
        int baud = args.Length >= 3 ? int.Parse(args[2]) : 2_000_000;

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"File not found: {filePath}");
            return 2;
        }

        using var port = new SerialPort(portName, baud, Parity.None, 8, StopBits.One)
        {
            ReadTimeout = 50,
            WriteTimeout = 1000,
            DtrEnable = false,
            RtsEnable = false,
            Encoding = Encoding.ASCII,
            NewLine = "\r\n"
        };

        try
        {
            port.Open();
            port.DiscardInBuffer();
            port.DiscardOutBuffer();

            Console.WriteLine($"[IAP] Opened {portName} @ {baud}");

            // 1) Break boot countdown with SPACE
            SendText(port, " ");

            // 2) Wait password prompt, send password
            WaitContains(port, "Input Password", 5000);
            SendText(port, Password + "\r");

            // 3) Wait menu and choose 1(download)
            WaitContains(port, "Main Menu", 5000);
            SendText(port, "1");

            // 4) Wait ymodem ready prompt / 'C'
            WaitContains(port, "Waiting for the file", 5000);

            var y = new YModemSender(port, Console.WriteLine);
            y.SendFile(filePath);

            // 5) Wait success text
            WaitContains(port, "Programming Completed Successfully", 8000);
            Console.WriteLine("[IAP] SUCCESS");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[IAP] FAILED: " + ex.Message);
            return 10;
        }
    }

    private static void SendText(SerialPort port, string text)
    {
        port.Write(text);
    }

    private static void WaitContains(SerialPort port, string token, int timeoutMs)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var sb = new StringBuilder();

        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                int b = port.ReadByte();
                if (b < 0) continue;
                char c = (char)b;
                sb.Append(c);

                if (sb.Length > 8000)
                    sb.Remove(0, sb.Length - 4000);

                if (sb.ToString().Contains(token, StringComparison.OrdinalIgnoreCase))
                    return;
            }
            catch (TimeoutException)
            {
                // keep polling
            }
        }

        throw new TimeoutException($"Timeout waiting token: {token}");
    }
}
