using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Channels;

namespace pdns_dhcp.Dhcp;

public class DhcpLeaseQueue
{
	private readonly Channel<DhcpLeaseChange> _pipe = Channel.CreateUnbounded<DhcpLeaseChange>();
}

public readonly record struct DhcpLeaseChange(IPAddress Address, string FQDN, DhcpLeaseIdentifier Identifier, TimeSpan Lifetime)
{
	public AddressFamily LeaseType { get; } = Address.AddressFamily;
}

public record DhcpLeaseIdentifier;
public record DhcpLeaseClientIdentifier(string ClientId) : DhcpLeaseIdentifier;
public record DhcpLeaseHWAddrIdentifier(PhysicalAddress HWAddr) : DhcpLeaseIdentifier;
