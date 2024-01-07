using pdns_dhcp.Kea;
using pdns_dhcp.Options;

namespace pdns_dhcp.Services;

public interface IDhcpWatcherFactory
{
	KeaService KeaService(KeaDhcpOptions options);
}
