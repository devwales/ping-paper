using System.IO;
using System.Text;

namespace Ping.Printing;

/// <summary>
/// Development / testing path: "prints" each receipt to a readable text file under
/// %LocalAppData%\Ping\receipts. Control bytes are stripped so files open cleanly
/// in Notepad. Lets you try Ping end-to-end with no printer attached.
/// </summary>
public class FileTransport : IPrintTransport
{
    private readonly string _folder;

    public FileTransport(string folder) => _folder = folder;

    public string Describe() => "receipt files (testing mode)";

    public Task SendAsync(byte[] payload, CancellationToken ct)
    {
        Directory.CreateDirectory(_folder);
        var name = $"receipt_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        File.WriteAllText(Path.Combine(_folder, name), ToReadable(payload));
        return Task.CompletedTask;
    }

    private static string ToReadable(byte[] payload)
    {
        // Walk the byte stream; keep printable ASCII and newlines, represent
        // ESC/POS control sequences as nothing (they only affect layout on paper).
        var sb = new StringBuilder(payload.Length);
        for (var i = 0; i < payload.Length; i++)
        {
            var b = payload[i];
            if (b == 0x0A) { sb.Append('\n'); continue; }
            if (b is 0x1B or 0x1D) { i += 2 < payload.Length ? 2 : 0; continue; } // skip 3-byte commands
            if (b >= 32 && b < 127) sb.Append((char)b);
        }
        return sb.ToString();
    }
}
