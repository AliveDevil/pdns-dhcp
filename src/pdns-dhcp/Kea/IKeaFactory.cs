using pdns_dhcp.Options;

using Stl.Interception;

namespace pdns_dhcp.Kea;

public interface IKeaFactory : IRequiresFullProxy
{
	KeaDhcp4LeaseHandler CreateHandler4();

	KeaDhcp6LeaseHandler CreateHandler6();

	KeaDhcpLeaseWatcher CreateWatcher(IKeaDhcpLeaseHandler handler, KeaDhcpServerOptions options);
}
