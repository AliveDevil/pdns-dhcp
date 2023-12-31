using System.Collections.Immutable;

using Microsoft.Extensions.Hosting;

using pdns_dhcp.Options;

namespace pdns_dhcp.Kea;

public class KeaService : IHostedService
{
	private readonly ImmutableArray<IHostedService> _services;

	public KeaService(KeaDhcpOptions options, IKeaFactory factory)
	{
		var services = ImmutableArray.CreateBuilder<IHostedService>();
		
		if (options.Dhcp4 is { } dhcp4Options)
		{
			services.Add(factory.CreateWatcher(factory.CreateHandler4(), dhcp4Options));
		}

		if (options.Dhcp6 is { } dhcp6Options)
		{
			services.Add(factory.CreateWatcher(factory.CreateHandler6(), dhcp6Options));
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
		await Task.WhenAny(waitTask, taskCompletionSource.Task).ConfigureAwait(continueOnCapturedContext: false);
	}
}
