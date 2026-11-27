using System.Net;
using System.Net.Sockets;

namespace PulseBoard.Monitoring.Infrastructure.Probing;

/// <summary>
/// Decides which IP addresses the prober may connect to. Users choose the URLs PulseBoard calls, so without
/// this anyone could point a check at internal services (server-side request forgery), such as a cloud
/// metadata endpoint, a database admin page or anything else on the private network.
/// </summary>
public static class NetworkAddressPolicy
{
    // IPv4 ranges that are never reachable on the public internet (RFC 6890 and friends).
    private static readonly (uint Network, int PrefixLength)[] BlockedIPv4Ranges =
    [
        (Parse("0.0.0.0"), 8), // "This network"
        (Parse("10.0.0.0"), 8), // Private
        (Parse("100.64.0.0"), 10), // Carrier-grade NAT
        (Parse("127.0.0.0"), 8), // Loopback
        (Parse("169.254.0.0"), 16), // Link-local, including cloud metadata (169.254.169.254)
        (Parse("172.16.0.0"), 12), // Private
        (Parse("192.0.0.0"), 24), // IETF protocol assignments
        (Parse("192.0.2.0"), 24), // Documentation
        (Parse("192.168.0.0"), 16), // Private
        (Parse("198.18.0.0"), 15), // Benchmarking
        (Parse("198.51.100.0"), 24), // Documentation
        (Parse("203.0.113.0"), 24), // Documentation
        (Parse("224.0.0.0"), 4), // Multicast
        (Parse("240.0.0.0"), 4), // Reserved, including broadcast
    ];

    public static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        return address.AddressFamily switch
        {
            AddressFamily.InterNetwork => IsPublicIPv4(address),
            AddressFamily.InterNetworkV6 => IsPublicIPv6(address),
            _ => false,
        };
    }

    private static bool IsPublicIPv4(IPAddress address)
    {
        var value = ToUInt32(address.GetAddressBytes());
        return !BlockedIPv4Ranges.Any(range => InRange(value, range.Network, range.PrefixLength));
    }

    private static bool IsPublicIPv6(IPAddress address)
    {
        if (IPAddress.IPv6Any.Equals(address) || IPAddress.IPv6Loopback.Equals(address)
            || address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
        {
            return false;
        }

        var bytes = address.GetAddressBytes();

        // fc00::/7 unique local addresses: the IPv6 equivalent of 10.0.0.0/8.
        if ((bytes[0] & 0xFE) == 0xFC)
        {
            return false;
        }

        // 64:ff9b::/96 NAT64 embeds an IPv4 address in the last four bytes: judge that address instead.
        if (bytes[0] == 0x00 && bytes[1] == 0x64 && bytes[2] == 0xFF && bytes[3] == 0x9B
            && bytes.AsSpan(4, 8).IndexOfAnyExcept((byte)0) < 0)
        {
            return IsPublicIPv4(new IPAddress(bytes.AsSpan(12, 4)));
        }

        return true;
    }

    private static bool InRange(uint address, uint network, int prefixLength)
    {
        var mask = prefixLength == 0 ? 0u : uint.MaxValue << (32 - prefixLength);
        return (address & mask) == (network & mask);
    }

    private static uint Parse(string address) => ToUInt32(IPAddress.Parse(address).GetAddressBytes());

    private static uint ToUInt32(byte[] bytes) =>
        ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
}
