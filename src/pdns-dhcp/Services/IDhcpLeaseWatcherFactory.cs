using pdns_dhcp.Kea;
using pdns_dhcp.Options;

using Stl.Interception;

namespace pdns_dhcp.Services;

public interface IDhcpLeaseWatcherFactory : IRequiresFullProxy
{
	KeaDhcpLeaseWatcher<KeaDhcp4LeaseHandler> KeaDhcp4Watcher(KeaDhcpServerOptions options);

	KeaDhcpLeaseWatcher<KeaDhcp6LeaseHandler> KeaDhcp6Watcher(KeaDhcpServerOptions options);
}
