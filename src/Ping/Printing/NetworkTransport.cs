using System.Net.Sockets;

namespace Ping.Printing;

/// <summary>
/// Ethernet path: raw ESC/POS over TCP, port 9100 (the de-facto standard for
/// network receipt printers, including the CISSIYOG / POSSAF E Ethernet port).
/// </summary>
public class NetworkTransport : IPrintTransport
{
    private readonly string _host;
    private readonly int _port;

    public NetworkTransport(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public string Describe() => $"network printer at {_host}:{_port}";

    public async Task SendAsync(byte[] payload, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_host))
            throw new InvalidOperationException("No printer address set - add it in Settings.");

        using var client = new TcpClient();
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(8));
            await client.ConnectAsync(_host, _port, timeoutCts.Token);
            await client.GetStream().WriteAsync(payload, ct);
            await client.GetStream().FlushAsync(ct);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new InvalidOperationException("The printer didn't answer - check it's on and the address is right.");
        }
        catch (SocketException)
        {
            throw new InvalidOperationException("Couldn't reach the printer - check the network connection.");
        }
    }
}
