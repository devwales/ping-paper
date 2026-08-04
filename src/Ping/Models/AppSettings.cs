namespace Ping.Models;

public enum PrinterConnection
{
    Spooler,   // Windows printer driver (USB) - the usual path
    Network,   // Ethernet, raw TCP to port 9100
    File       // Write receipts to text files (for testing without a printer)
}

public class AppSettings
{
    // Bubble
    public double? BubbleLeft { get; set; }
    public double? BubbleTop { get; set; }
    public double BubbleSize { get; set; } = 56;
    public double BubbleOpacity { get; set; } = 0.95;
    public bool AlwaysOnTop { get; set; } = true;
    public bool ShowBubbleOnStart { get; set; } = true;

    // Working hours (time blocks offered in the Add Task flow)
    public TimeOnly WorkingStart { get; set; } = new(9, 0);
    public TimeOnly WorkingEnd { get; set; } = new(19, 0);
    public int TimeStepMinutes { get; set; } = 30;

    // Printer
    public PrinterConnection Connection { get; set; } = PrinterConnection.Spooler;
    public string PrinterName { get; set; } = "";   // Windows printer queue name
    public string NetworkHost { get; set; } = "";   // e.g. 192.168.1.50
    public int NetworkPort { get; set; } = 9100;
    public int PaperWidthMm { get; set; } = 80;     // 80 or 58

    // Behaviour
    public bool AutoStart { get; set; } = false;
    public bool CatchUpEnabled { get; set; } = true;   // print missed tasks on start / wake
}
