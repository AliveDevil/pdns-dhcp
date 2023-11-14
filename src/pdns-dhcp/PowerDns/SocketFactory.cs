using Stl.Interception;

namespace pdns_dhcp.PowerDns;

public interface SocketFactory : IRequiresFullProxy
{
	PowerDnsSocket Create();
}
