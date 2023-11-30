using nietras.SeparatedValues;

namespace pdns_dhcp.Kea;

public class KeaDhcp6LeaseHandler : IKeaDhcpLeaseHandler
{
	public void Handle(in SepReader.Row row)
	{
		KeaDhcp6Lease lease = KeaDhcp6Lease.Parse(row);
	}
}
