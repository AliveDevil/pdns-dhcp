using pdns_dhcp.Kea;
using pdns_dhcp.Options;

using Stl.Interception;

namespace pdns_dhcp.Services;

public interface IDhcpLeaseWatcherFactory : IRequiresFullProxy
{
	KeaDhcp4LeaseWatcher KeaDhcp4Watcher(KeaDhcpServerOptions options);

	KeaDhcp6LeaseWatcher KeaDhcp6Watcher(KeaDhcpServerOptions options);
}
