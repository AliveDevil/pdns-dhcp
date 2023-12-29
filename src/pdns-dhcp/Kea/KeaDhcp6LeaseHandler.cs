using nietras.SeparatedValues;

using pdns_dhcp.Dhcp;

namespace pdns_dhcp.Kea;

public class KeaDhcp6LeaseHandler : IKeaDhcpLeaseHandler
{
	public DhcpLeaseChange? Handle(in SepReader.Row row)
	{
		KeaDhcp6Lease lease = KeaDhcp6Lease.Parse(row);

		return default;
	}
}
