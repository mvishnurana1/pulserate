using System.Net;
using PulseBoard.Monitoring.Infrastructure.Probing;

namespace PulseBoard.Monitoring.Tests.Probing;

public sealed class NetworkAddressPolicyTests
{
    [Theory]
    [InlineData("127.0.0.1")] // Loopback
    [InlineData("10.1.2.3")] // Private
    [InlineData("172.16.0.1")] // Private
    [InlineData("172.31.255.255")] // Private, top of range
    [InlineData("192.168.1.10")] // Private
    [InlineData("169.254.169.254")] // Cloud metadata endpoint
    [InlineData("100.64.0.1")] // Carrier-grade NAT
    [InlineData("0.0.0.0")]
    [InlineData("224.0.0.1")] // Multicast
    [InlineData("255.255.255.255")] // Broadcast
    [InlineData("::1")] // IPv6 loopback
    [InlineData("::")]
    [InlineData("fe80::1")] // IPv6 link-local
    [InlineData("fd12:3456::1")] // IPv6 unique local
    [InlineData("ff02::1")] // IPv6 multicast
    [InlineData("::ffff:127.0.0.1")] // IPv4-mapped loopback
    [InlineData("::ffff:10.0.0.1")] // IPv4-mapped private
    [InlineData("64:ff9b::a9fe:a9fe")] // NAT64 wrapping 169.254.169.254
    public void Blocks_private_and_reserved_addresses(string address)
    {
        Assert.False(NetworkAddressPolicy.IsPublic(IPAddress.Parse(address)));
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("172.32.0.1")] // Just outside 172.16.0.0/12
    [InlineData("192.169.0.1")] // Just outside 192.168.0.0/16
    [InlineData("2606:4700:4700::1111")]
    [InlineData("::ffff:8.8.8.8")]
    [InlineData("64:ff9b::808:808")] // NAT64 wrapping 8.8.8.8
    public void Allows_public_addresses(string address)
    {
        Assert.True(NetworkAddressPolicy.IsPublic(IPAddress.Parse(address)));
    }
}
