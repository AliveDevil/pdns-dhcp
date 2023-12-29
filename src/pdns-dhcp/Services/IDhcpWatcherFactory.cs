using pdns_dhcp.Kea;
using pdns_dhcp.Options;

using Stl.Interception;

namespace pdns_dhcp.Services;

public interface IDhcpWatcherFactory : IRequiresFullProxy
{
	KeaService KeaService(KeaDhcpOptions options);
}
