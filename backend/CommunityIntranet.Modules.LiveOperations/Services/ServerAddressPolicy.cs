using System.Net;
using System.Net.Sockets;

namespace CommunityIntranet.Modules.LiveOperations.Services;

public static class ServerAddressPolicy
{
    public static bool IsValidHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host)
            || host.Length > 253
            || host.IndexOfAny(['/', '\\', '@', '?', '#']) >= 0)
        {
            return false;
        }

        return Uri.CheckHostName(host) is UriHostNameType.Dns
            or UriHostNameType.IPv4
            or UriHostNameType.IPv6;
    }

    public static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return !IsInIpv4Range(bytes, 0, 8)
                && !IsInIpv4Range(bytes, 10, 8)
                && !IsInIpv4Range(bytes, 100, 10, 64)
                && !IsInIpv4Range(bytes, 127, 8)
                && !IsInIpv4Range(bytes, 169, 16, 254)
                && !IsInIpv4Range(bytes, 172, 12, 16)
                && !IsInIpv4Range(bytes, 192, 24, 0, 0)
                && !IsInIpv4Range(bytes, 192, 24, 0, 2)
                && !IsInIpv4Range(bytes, 192, 24, 88, 99)
                && !IsInIpv4Range(bytes, 192, 16, 168)
                && !IsInIpv4Range(bytes, 198, 15, 18)
                && !IsInIpv4Range(bytes, 198, 24, 51, 100)
                && !IsInIpv4Range(bytes, 203, 24, 0, 113)
                && bytes[0] < 224;
        }

        if (address.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return false;
        }

        return !IPAddress.IsLoopback(address)
            && !address.IsIPv6LinkLocal
            && !address.IsIPv6Multicast
            && (bytes[0] & 0xfe) != 0xfc
            && !IsInIpv6Range(bytes, [0x20, 0x01, 0x0d, 0xb8], 32)
            && !address.Equals(IPAddress.IPv6Any)
            && !address.Equals(IPAddress.IPv6None);
    }

    private static bool IsInIpv4Range(
        byte[] address,
        byte first,
        int prefixLength,
        byte second = 0,
        byte third = 0)
    {
        var network = new[] { first, second, third, (byte)0 };
        return PrefixMatches(address, network, prefixLength);
    }

    private static bool IsInIpv6Range(
        byte[] address,
        byte[] prefix,
        int prefixLength)
    {
        var network = new byte[16];
        prefix.CopyTo(network, 0);
        return PrefixMatches(address, network, prefixLength);
    }

    private static bool PrefixMatches(
        IReadOnlyList<byte> address,
        IReadOnlyList<byte> network,
        int prefixLength)
    {
        var wholeBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;
        for (var index = 0; index < wholeBytes; index++)
        {
            if (address[index] != network[index])
            {
                return false;
            }
        }

        if (remainingBits == 0)
        {
            return true;
        }

        var mask = (byte)(0xff << (8 - remainingBits));
        return (address[wholeBytes] & mask) == (network[wholeBytes] & mask);
    }
}
