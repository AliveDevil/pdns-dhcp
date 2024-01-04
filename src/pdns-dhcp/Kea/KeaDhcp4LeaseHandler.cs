using nietras.SeparatedValues;

using pdns_dhcp.Dhcp;

namespace pdns_dhcp.Kea;

public class KeaDhcp4LeaseHandler : IKeaDhcpLeaseHandler
{
	public DhcpLeaseChange? Handle(in SepReader.Row row)
	{
		if (KeaDhcp4Lease.Parse(row) is not { } lease)
		{
			goto exitNull;
		}

		if (lease.State != 0)
		{
			goto exitNull;
		}

		DhcpLeaseIdentifier identifier = lease.ClientId switch
		{
			string clientId when !string.IsNullOrWhiteSpace(clientId) => new DhcpLeaseClientIdentifier(clientId),
			_ => new DhcpLeaseHWAddrIdentifier(lease.HWAddr)
		};

		return new(lease.Address, lease.Hostname, identifier, lease.ValidLifetime);

	exitNull:
		return null;
	}
}
