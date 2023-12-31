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

		return new(lease.Address, lease.Hostname, null, default);
	}
}
