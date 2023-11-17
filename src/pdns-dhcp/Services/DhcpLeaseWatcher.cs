using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using pdns_dhcp.Options;

namespace pdns_dhcp.Services;

public class DhcpLeaseWatcher : BackgroundService
{
	public DhcpLeaseWatcher(IOptions<DhcpOptions> options)
	{
		var dhcpOptions = options.Value;
		if (dhcpOptions.Kea is { } keaOptions)
		{
		}
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		throw new NotImplementedException();
	}
}
