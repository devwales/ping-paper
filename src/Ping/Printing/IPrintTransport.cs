namespace Ping.Printing;

/// <summary>
/// A way to get raw ESC/POS bytes to the printer.
/// Implementations throw InvalidOperationException with a calm, user-readable
/// message when the printer can't be reached.
/// </summary>
public interface IPrintTransport
{
    string Describe();
    Task SendAsync(byte[] payload, CancellationToken ct);
}
