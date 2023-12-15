using Microsoft.Extensions.Hosting;

namespace pdns_dhcp.Services;

public class PowerDnsBackend : BackgroundService
{
	public PowerDnsBackend()
	{
	}

	~PowerDnsBackend()
	{
		DisposeCore();
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		return Task.CompletedTask;		
	}

	public override void Dispose()
	{
		base.Dispose();
		DisposeCore();
		GC.SuppressFinalize(this);
	}

	private void DisposeCore()
	{
	}
}
