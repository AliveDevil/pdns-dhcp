using System.Text.Json;

using Stl.Async;

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
		Dispose();
	}

	public void Dispose()
	{
		using (_cts)
		using (_stream)
		{
			_cts.Cancel();
			_task.GetAwaiter().GetResult();
		}
		GC.SuppressFinalize(this);
	}

	public void Start(CancellationToken stoppingToken)
	{
		using var other = Interlocked.Exchange(ref _cts, CancellationTokenSource.CreateLinkedTokenSource(stoppingToken));
		_task = Run(_cts.Token);
		other.Cancel();
	}

	private async Task Run(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			await JsonSerializer.DeserializeAsync<Method>(_stream, cancellationToken: stoppingToken);
		}
	}
}
