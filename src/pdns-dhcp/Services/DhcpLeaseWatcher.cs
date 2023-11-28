using System.Collections.Immutable;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using pdns_dhcp.Kea;
using pdns_dhcp.Options;

namespace pdns_dhcp.Services;

public class DhcpLeaseWatcher : IHostedService
{
	private readonly ImmutableArray<IHostedService> _services;

	public DhcpLeaseWatcher(IOptions<DhcpOptions> options, IDhcpLeaseWatcherFactory factory)
	{
		var dhcpOptions = options.Value;
		var services = ImmutableArray.CreateBuilder<IHostedService>();
		if (dhcpOptions.Kea is { } keaOptions)
		{
			if (keaOptions.Dhcp4 is { } dhcp4Options)
			{
				services.Add(factory.KeaDhcp4Watcher(dhcp4Options));
			}

			if (keaOptions.Dhcp6 is { } dhcp6Options)
			{
				services.Add(factory.KeaDhcp6Watcher(dhcp6Options));
			}
		}

		_services = services.DrainToImmutable();
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		Task[] tasks = new Task[_services.Length];
		for (int i = 0; i < tasks.Length; i++)
		{
			tasks[i] = _services[i].StartAsync(cancellationToken);
		}

		return Task.WhenAll(tasks);
	}

	public async Task StopAsync(CancellationToken cancellationToken)
	{
		Task[] tasks = new Task[_services.Length];
		for (int i = 0; i < tasks.Length; i++)
		{
			tasks[i] = _services[i].StopAsync(cancellationToken);
		}

		var waitTask = Task.WhenAll(tasks);
		TaskCompletionSource taskCompletionSource = new();
		using var registration = cancellationToken.Register(s => (s as TaskCompletionSource)!.SetCanceled(), taskCompletionSource);
		await Task.WhenAny(waitTask, taskCompletionSource.Task).ConfigureAwait(continueOnCapturedContext: false);
	}
}
