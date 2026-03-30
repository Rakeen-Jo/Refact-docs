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
        int baud = args.Length >= 3 ? int.Parse(args[2]) : 921_600;

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

            const int maxAttempts = 3;
            Exception? last = null;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    Console.WriteLine($"[IAP] attempt {attempt}/{maxAttempts}");

                    for (int i = 0; i < 20; i++) { SendText(port, " "); Thread.Sleep(50); }

                    string first = WaitAnyContains(port, 9000, "Input Password", "Main Menu", "Waiting for the file");
                    if (first.Contains("Input Password", StringComparison.OrdinalIgnoreCase))
                    {
                        SendText(port, Password + "\r");
                        WaitContains(port, "Main Menu", 5000);
                        SendText(port, "1");
                        WaitContains(port, "Waiting for the file", 5000);
                    }
                    else if (first.Contains("Main Menu", StringComparison.OrdinalIgnoreCase))
                    {
                        SendText(port, "1");
                        WaitContains(port, "Waiting for the file", 5000);
                    }

                    var y = new YModemSender(port, Console.WriteLine);
                    y.SendFile(filePath);
                    WaitContains(port, "Programming Completed Successfully", 10000);
                    Console.WriteLine("[IAP] SUCCESS");
                    return 0;
                }
                catch (Exception ex)
                {
                    last = ex;
                    Console.WriteLine($"[IAP] attempt failed: {ex.Message}");
                    try { port.Write(new[] { (char)0x18, (char)0x18 }, 0, 2); } catch { }
                    Thread.Sleep(300);
                    try { port.DiscardInBuffer(); port.DiscardOutBuffer(); } catch { }
                }
            }

            throw new Exception("Update failed after retries", last);
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
        _ = WaitAnyContains(port, timeoutMs, token);
    }

    private static string WaitAnyContains(SerialPort port, int timeoutMs, params string[] tokens)
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

                if (sb.Length > 12000)
                    sb.Remove(0, sb.Length - 6000);

                string hay = sb.ToString();
                foreach (var token in tokens)
                    if (hay.Contains(token, StringComparison.OrdinalIgnoreCase))
                        return token;
            }
            catch (TimeoutException)
            {
                // keep polling
            }
        }

        throw new TimeoutException($"Timeout waiting token: {string.Join(" | ", tokens)}");
    }
}
