using nietras.SeparatedValues;

using pdns_dhcp.Dhcp;

namespace pdns_dhcp.Kea;

public interface IKeaDhcpLeaseHandler
{
	DhcpLeaseChange? Handle(in SepReader.Row row);
}
