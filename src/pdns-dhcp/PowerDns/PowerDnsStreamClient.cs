using System.Text.Json;

namespace pdns_dhcp.PowerDns;

public class PowerDnsStreamClient : IDisposable
{
	private readonly Stream _stream;
	private CancellationTokenSource _cts = new();
	private Task _task;

	public PowerDnsStreamClient(Stream stream)
	{
		_stream = stream;
	}

	~PowerDnsStreamClient()
	{
		DisposeCore();
	}

	public void Dispose()
	{
		_cts.Cancel();
		if (Interlocked.Exchange(ref _task, null!) is { } task)
		{
			task
				.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing)
				.GetAwaiter().GetResult();
		}

		DisposeCore();

		GC.SuppressFinalize(this);
	}

	public void Start(CancellationToken stoppingToken)
	{
		var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
		using var old = Interlocked.Exchange(ref _cts, cts);
		old.Cancel();
		_task = Run(cts.Token);
	}

	private void DisposeCore()
	{
		_cts.Dispose();
		_stream.Dispose();
	}

	private async Task Run(CancellationToken stoppingToken)
	{
		try
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				switch (await JsonSerializer.DeserializeAsync<Method>(_stream, cancellationToken: stoppingToken).ConfigureAwait(false))
				{
					case InitializeMethod init:
						break;
				
					case LookupMethod lookup:
						break;
				
					default:
						break;
				}
			}
		}
		finally
		{
			if (Interlocked.Exchange(ref _task, null!) is not null)
			{
				Dispose();
			}
		}
	}
}
