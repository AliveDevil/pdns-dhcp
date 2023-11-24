using System.Threading.Channels;

using Microsoft.Extensions.Hosting;

using pdns_dhcp.Options;

namespace pdns_dhcp.Kea;

public abstract class KeaDhcpLeaseWatcher : IHostedService
{
	private readonly FileSystemWatcher _fsw;
	private readonly string _leaseFile;
	private Channel<FileSystemEventArgs>? _eventChannel;
	private Task? _executeTask;
	private CancellationTokenSource? _stoppingCts;

	protected KeaDhcpServerOptions Options { get; }

	protected KeaDhcpLeaseWatcher(KeaDhcpServerOptions options)
	{
		Options = options;
		var leases = options.Leases.AsSpan();
		if (leases.IsWhiteSpace())
		{
			throw new ArgumentException($"{nameof(options.Leases)} must not be empty.", nameof(options));
		}

		var leaseFile = Path.GetFileName(leases);
		var leaseDirectory = Path.GetDirectoryName(leases).ToString();
		if (!Directory.Exists(leaseDirectory))
		{
			throw new ArgumentException($"{nameof(options.Leases)} must point to a file in an existing path.", nameof(options));
		}

		_leaseFile = leaseFile.ToString();
		_fsw = new(leaseDirectory, _leaseFile);
		_fsw.Changed += OnLeaseChanged;
		_fsw.Error += OnLeaseError;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		_stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		_executeTask = Reading(_stoppingCts.Token);
		return _executeTask is { IsCompleted: true } completed ? completed : Task.CompletedTask;
	}

	public async Task StopAsync(CancellationToken cancellationToken)
	{
		if (_executeTask is null)
		{
			return;
		}

		_fsw.EnableRaisingEvents = false;
		try
		{
			_stoppingCts!.Cancel();
		}
		finally
		{
			TaskCompletionSource taskCompletionSource = new();
			using (cancellationToken.Register(s => ((TaskCompletionSource)s!).SetCanceled(), taskCompletionSource))
			{
				await Task.WhenAny(_executeTask, taskCompletionSource.Task).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
	}

	private async Task Reading(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			using CancellationTokenSource loopCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
			_eventChannel = Channel.CreateUnbounded<FileSystemEventArgs>();
			_fsw.EnableRaisingEvents = true;
			ValueTask reader = default;
			try
			{
				await foreach (var @event in _eventChannel.Reader.ReadAllAsync(loopCts.Token))
				{
					switch (@event)
					{
						case { ChangeType: WatcherChangeTypes.Created }:
						case RenamedEventArgs { ChangeType: WatcherChangeTypes.Renamed } renamed when renamed.Name == _leaseFile:
							reader = FileReader(loopCts.Token);
							break;

						case { ChangeType: WatcherChangeTypes.Deleted }:
						case RenamedEventArgs { ChangeType: WatcherChangeTypes.Renamed } renamed when renamed.OldName == _leaseFile:
							loopCts.Cancel();
							break;

						case { ChangeType: WatcherChangeTypes.Changed }:
							break;
					}
				}
			}
			catch { }
			finally
			{
				_eventChannel.Writer.TryComplete();
				try
				{
					await reader.ConfigureAwait(continueOnCapturedContext: false);
				}
				catch { }
			}
		}

		ValueTask FileReader(CancellationToken stoppingToken)
		{
			return ValueTask.CompletedTask;
		}
	}

	private void OnLeaseChanged(object sender, FileSystemEventArgs e)
	{
		var writer = _eventChannel?.Writer;
		if (writer?.TryWrite(e) != false)
		{
			return;
		}

		var write = writer.WriteAsync(e);
		if (write.IsCompleted)
		{
			return;
		}

		write.GetAwaiter().GetResult();
	}

	private void OnLeaseError(object sender, ErrorEventArgs e)
	{
		_eventChannel?.Writer.Complete(e.GetException());
	}
}
