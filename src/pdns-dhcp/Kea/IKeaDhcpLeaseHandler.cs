using nietras.SeparatedValues;

namespace pdns_dhcp.Kea;

public interface IKeaDhcpLeaseHandler
{
	void Handle(in SepReader.Row row);
}
