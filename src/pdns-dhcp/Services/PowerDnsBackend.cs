using System.Net.Sockets;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using pdns_dhcp.Options;
using pdns_dhcp.PowerDns;

namespace pdns_dhcp.Services;

public class PowerDnsBackend : IHostedService
{
	private readonly PowerDnsOptions _options;

	public PowerDnsBackend(IOptions<PowerDnsOptions> options, IPowerDnsFactory socketFactory)
	{
		_options = options.Value;
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
