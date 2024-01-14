using System.Collections.Immutable;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using pdns_dhcp.Options;

namespace pdns_dhcp.Services;

public class DhcpWatcher : IHostedService
{
	private readonly ImmutableArray<IHostedService> _services;

	public DhcpWatcher(IOptions<DhcpOptions> options, IDhcpWatcherFactory factory)
	{
		var dhcpOptions = options.Value;
		var services = ImmutableArray.CreateBuilder<IHostedService>();
		if (dhcpOptions.Kea is { } keaOptions)
		{
			services.Add(factory.KeaService(keaOptions));
		}

		_services = services.DrainToImmutable();
	}

	public Task StartAsync(CancellationToken cancellationToken = default)
	{
		Task[] tasks = new Task[_services.Length];
		for (int i = 0; i < tasks.Length; i++)
		{
			tasks[i] = _services[i].StartAsync(cancellationToken);
		}

		return Task.WhenAll(tasks);
	}

	public async Task StopAsync(CancellationToken cancellationToken = default)
	{
		Task[] tasks = new Task[_services.Length];
		for (int i = 0; i < tasks.Length; i++)
		{
			tasks[i] = _services[i].StopAsync(cancellationToken);
		}

		var waitTask = Task.WhenAll(tasks);
		TaskCompletionSource taskCompletionSource = new();
		using var registration = cancellationToken.Register(s => ((TaskCompletionSource)s!).SetCanceled(), taskCompletionSource);
		await Task.WhenAny(waitTask, taskCompletionSource.Task).ConfigureAwait(false);
	}
}
