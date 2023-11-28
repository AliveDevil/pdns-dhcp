using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using pdns_dhcp.Options;
using pdns_dhcp.PowerDns;

namespace pdns_dhcp.Services;

public class PowerDnsBackend : BackgroundService
{
	private readonly PowerDnsOptions _options;

	public PowerDnsBackend(IOptions<PowerDnsOptions> options, IPowerDnsFactory socketFactory)
	{
		_options = options.Value;
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		return Task.CompletedTask;
	}
}
