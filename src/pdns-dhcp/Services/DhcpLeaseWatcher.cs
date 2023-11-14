using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using pdns_dhcp.Options;
using pdns_dhcp.PowerDns;

namespace pdns_dhcp.Services;

public class DhcpLeaseWatcher : IHostedService
{
	private readonly PowerDnsOptions _options;
	private readonly PowerDnsSocket _socket;

	public DhcpLeaseWatcher(IOptions<PowerDnsOptions> options, SocketFactory factory)
	{
		_options = options.Value;
		_socket = factory.Create();
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}
}
