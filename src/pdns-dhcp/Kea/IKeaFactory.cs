using pdns_dhcp.Options;

namespace pdns_dhcp.Kea;

public interface IKeaFactory
{
	KeaDhcp4LeaseHandler CreateHandler4();

	KeaDhcp6LeaseHandler CreateHandler6();

	KeaDhcpLeaseWatcher CreateWatcher(IKeaDhcpLeaseHandler handler, KeaDhcpServerOptions options);
}
