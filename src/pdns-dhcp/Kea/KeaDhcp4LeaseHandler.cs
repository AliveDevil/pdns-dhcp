using nietras.SeparatedValues;

namespace pdns_dhcp.Kea;

public class KeaDhcp4LeaseHandler : IKeaDhcpLeaseHandler
{
	public void Handle(in SepReader.Row row)
	{
		KeaDhcp4Lease lease = KeaDhcp4Lease.Parse(row);
	}
}
