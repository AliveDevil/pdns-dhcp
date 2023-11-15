using System.Net.Sockets;

using Stl.Interception;

namespace pdns_dhcp.PowerDns;

public interface IPowerDnsFactory : IRequiresFullProxy
{
	PowerDnsStreamClient CreateClient(Stream stream);

	PowerDnsUnixSocket CreateUnixSocket(string socketPath);
}
