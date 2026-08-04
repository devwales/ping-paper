using System.Drawing.Printing;

namespace Ping.Printing;

public record DetectedPrinter(string Name, int Score);

/// <summary>
/// Lists installed Windows printer queues and ranks the ones that look like
/// thermal receipt printers (CISSIYOG / POSSAF E, POS-58/80, Xprinter, Gprinter,
/// Epson TM, Star TSP, Generic/Text Only, ...). Ethernet printers are added in
/// Settings by address instead - they don't need a Windows queue.
/// </summary>
public static class PrinterDiscovery
{
    private static readonly string[] Keywords =
    {
        "cissiyog", "possaf", "pos-58", "pos-80", "pos58", "pos80", "pos ",
        "58mm", "80mm", "thermal", "receipt", "xp-", "xprinter", "gp-",
        "gprinter", "tm-t", "tm_", "epson tm", "tsp", "star micronics",
        "generic / text only", "generic text", "munbyn", "rongta", "hoin",
        "cashino", "zjiang", "netum", "vretti"
    };

    public static List<DetectedPrinter> Discover()
    {
        var found = new List<DetectedPrinter>();

        string[] names;
        try
        {
            // InstalledPrinters is lazy: faults surface during enumeration, not on access.
            // A stopped spooler service or a broken driver throws - never let that crash the app.
            names = PrinterSettings.InstalledPrinters.Cast<string>().ToArray();
        }
        catch (Exception)
        {
            return found;
        }

        foreach (var name in names)
        {
            var lower = name.ToLowerInvariant();
            var score = 0;
            foreach (var kw in Keywords)
            {
                if (lower.Contains(kw)) { score += kw.Length >= 4 ? 2 : 1; }
            }
            found.Add(new DetectedPrinter(name, score));
        }
        return found.OrderByDescending(p => p.Score).ThenBy(p => p.Name).ToList();
    }

    /// <summary>Best guess for an unattended first run, or null if nothing looks thermal.</summary>
    public static string? BestGuess()
        => Discover().Where(p => p.Score > 0).Select(p => p.Name).FirstOrDefault();
}
