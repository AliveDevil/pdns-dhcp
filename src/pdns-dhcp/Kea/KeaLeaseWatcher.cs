using Microsoft.Extensions.Hosting;

using pdns_dhcp.Options;

using Stl.IO;

namespace pdns_dhcp.Kea;

public abstract class KeaDhcpLeaseWatcher : BackgroundService
{
	private readonly FileSystemWatcher fsw;

	protected KeaDhcpServerOptions Options { get; }

	protected KeaDhcpLeaseWatcher(KeaDhcpServerOptions options)
	{
		Options = options;
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		throw new NotImplementedException();
	}
}
