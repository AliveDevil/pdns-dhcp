using nietras.SeparatedValues;

using pdns_dhcp.Dhcp;

namespace pdns_dhcp.Kea;

public class KeaDhcp6LeaseHandler : IKeaDhcpLeaseHandler
{
	public DhcpLeaseChange? Handle(in SepReader.Row row)
	{
		if (KeaDhcp6Lease.Parse(row) is not { } lease)
		{
			return null;
		}

		DhcpLeaseIdentifier identifier = lease.DUId switch
		{
			string clientId when !string.IsNullOrWhiteSpace(clientId) => new DhcpLeaseClientIdentifier(clientId),
			_ => new DhcpLeaseHWAddrIdentifier(lease.HWAddr)
		};

		return new(lease.Address, lease.Hostname, identifier, lease.ValidLifetime);
	}
}
