using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using pdns_dhcp.Options;

namespace pdns_dhcp.Services;

public class DhcpLeaseWatcher : IHostedService
{
	public DhcpLeaseWatcher(IOptions<DhcpOptions> options)
	{
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
