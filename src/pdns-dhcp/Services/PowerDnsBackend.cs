using Microsoft.Extensions.Hosting;

namespace pdns_dhcp.Services;

public class PowerDnsBackend : IHostedService
{
	public Task StartAsync(CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}
}
