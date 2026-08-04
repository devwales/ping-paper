using Ping.Models;

namespace Ping.Printing;

/// <summary>
/// Builds the receipt layouts. Designed for 80mm paper; scales down cleanly to 58mm.
/// </summary>
public static class ReceiptComposer
{
    public static byte[] SingleTask(PingTask task, int paperWidthMm, bool missed = false)
    {
        var cols = EscPos.ColumnsFor(paperWidthMm);
        var p = new EscPos().Init().FontA();

        Header(p, cols);

        if (missed)
        {
            p.AlignCenter().Size(0, 0).TextLine("( missed earlier )").NewLine();
        }

        // The task itself, big and readable.
        p.AlignLeft().Size(1, 1).Bold(true);
        var bigCols = cols / 2;
        p.Wrapped(task.Text, bigCols);
        p.Size(0, 0).Bold(false).NewLine();

        p.AlignLeft()
            .TextLine($"Scheduled: {task.FriendlyDay} {task.FriendlyTime}")
            .NewLine()
            .Divider(cols)
            .AlignCenter()
            .TextLine("Done? Tick it off.")
            .Feed(3)
            .Cut();

        return p.Build();
    }

    public static byte[] Batch(IReadOnlyList<PingTask> tasks, string title, int paperWidthMm)
    {
        var cols = EscPos.ColumnsFor(paperWidthMm);
        var p = new EscPos().Init().FontA();

        Header(p, cols);

        p.AlignCenter().Bold(true).TextLine(title).Bold(false).NewLine();

        foreach (var task in tasks)
        {
            p.AlignLeft().TextLine($"{task.FriendlyTime}");
            p.Bold(true).Size(1, 0);
            p.Wrapped(task.Text, cols / 2);
            p.Size(0, 0).Bold(false).NewLine();
        }

        p.Divider(cols)
            .AlignCenter()
            .TextLine(tasks.Count == 1 ? "Done? Tick it off." : "Done? Tick them off.")
            .Feed(3)
            .Cut();

        return p.Build();
    }

    public static byte[] TodayPrintAll(IReadOnlyList<PingTask> tasks, int paperWidthMm)
        => Batch(tasks, "Today, on paper", paperWidthMm);

    public static byte[] MissedCatchUp(IReadOnlyList<PingTask> tasks, int paperWidthMm)
        => Batch(tasks, "While you were away", paperWidthMm);

    public static byte[] TestPrint(int paperWidthMm, string connectionDescription)
    {
        var cols = EscPos.ColumnsFor(paperWidthMm);
        var p = new EscPos().Init().FontA();

        Header(p, cols);

        p.AlignCenter()
            .Size(1, 1).Bold(true).TextLine("Test print").Size(0, 0).Bold(false)
            .NewLine()
            .TextLine("If you can read this,")
            .TextLine("your printer is ready.")
            .NewLine()
            .AlignLeft()
            .Wrapped($"Paper: {paperWidthMm}mm ({cols} columns)", cols)
            .Wrapped($"Via: {connectionDescription}", cols)
            .NewLine()
            .Divider(cols)
            .AlignCenter()
            .TextLine("Done? Tick it off.")
            .Feed(3)
            .Cut();

        return p.Build();
    }

    private static void Header(EscPos p, int cols)
    {
        var now = DateTime.Now;
        p.AlignCenter()
            .Size(1, 1).Bold(true).TextLine("Ping").Size(0, 0).Bold(false)
            .TextLine(now.ToString("ddd d MMM yyyy") + "  " + now.ToString("HH:mm"))
            .Divider(cols);
    }
}
