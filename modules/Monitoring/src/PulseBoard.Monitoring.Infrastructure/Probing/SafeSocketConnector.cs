using System.Net;
using System.Net.Sockets;

namespace PulseBoard.Monitoring.Infrastructure.Probing;

/// <summary>Thrown when every address a host resolves to is blocked by <see cref="NetworkAddressPolicy"/>.</summary>
public sealed class BlockedAddressException(string host)
    : Exception($"Blocked: {host} resolves to a private or reserved network address.");

/// <summary>
/// Resolves DNS and opens the TCP connection itself, so the address check and the connection use the same
/// IP. Checking the URL up front isn't enough: a hostname can resolve to a public address when validated
/// and a private one a moment later (DNS rebinding), and a redirect can point anywhere. This runs for every
/// connection, redirects included.
/// </summary>
internal static class SafeSocketConnector
{
    public static async ValueTask<Stream> ConnectAsync(
        SocketsHttpConnectionContext context,
        bool allowPrivateNetworks,
        CancellationToken cancellationToken)
    {
        var host = context.DnsEndPoint.Host;

        var addresses = IPAddress.TryParse(host, out var literal)
            ? [literal]
            : await Dns.GetHostAddressesAsync(host, cancellationToken);

        if (!allowPrivateNetworks)
        {
            addresses = addresses.Where(NetworkAddressPolicy.IsPublic).ToArray();
        }

        if (addresses.Length == 0)
        {
            throw new BlockedAddressException(host);
        }

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
