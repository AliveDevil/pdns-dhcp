using System.Threading.Channels;

using Microsoft.Extensions.Hosting;

using nietras.SeparatedValues;

using pdns_dhcp.Options;

using Stl.Async;

namespace pdns_dhcp.Kea;

public abstract class KeaDhcpLeaseWatcher : IHostedService
{
	private static readonly FileStreamOptions _leaseFileStreamOptions = new()
	{
		Access = FileAccess.Read,
		Mode = FileMode.Open,
		Options = FileOptions.SequentialScan,
		Share = (FileShare)7,
	};
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
		SemaphoreSlim readerLock = new(1, 1);
		while (!stoppingToken.IsCancellationRequested)
		{
			using CancellationTokenSource loopCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
			var loopToken = loopCts.Token;
			_eventChannel = Channel.CreateUnbounded<FileSystemEventArgs>();
			_fsw.EnableRaisingEvents = true;
			Task reader = FileReader(loopToken);
			try
			{
				await foreach (var @event in _eventChannel.Reader.ReadAllAsync(loopToken))
				{
					// Guard for Deleted-events and moved-away events,
					// both have to stop this reader immediately.
					// Just wait for the file being created/moved to _leaseFile.
					// Described in [The LFC Process](https://kea.readthedocs.io/en/latest/arm/lfc.html#kea-lfc)
					switch (@event)
					{
						case { ChangeType: WatcherChangeTypes.Deleted }:
						case RenamedEventArgs renamed when renamed.OldName == _leaseFile:
							loopCts.Cancel();
							continue;
					}

					if (reader is not { IsCompleted: false })
					{
						reader = FileReader(loopToken);
					}

					if (@event.ChangeType == WatcherChangeTypes.Changed)
					{

					}
				}
			}
			catch { }
			finally
			{
				_eventChannel.Writer.TryComplete();
				if (reader is { IsCompleted: false })
				{
					try
					{
						await reader.ConfigureAwait(continueOnCapturedContext: false);
					}
					catch { }
				}
			}
		}
	}

	private async Task FileReader(CancellationToken stoppingToken)
	{
		try
		{
			using var fileStream = new FileStream(Options.Leases, _leaseFileStreamOptions);
		}
		catch { }
	}

	private void OnLeaseChanged(object sender, FileSystemEventArgs e)
	{
		var writer = _eventChannel?.Writer;
		if (writer?.TryWrite(e) != false)
		{
			return;
		}

#pragma warning disable CA2012 // Task is awaited immediately.
		if (writer.WriteAsync(e) is { IsCompleted: false } task)
		{
			try
			{
				task.GetAwaiter().GetResult();
			}
			catch { }
		}
#pragma warning restore
	}

	private void OnLeaseError(object sender, ErrorEventArgs e)
	{
		_eventChannel?.Writer.Complete(e.GetException());
	}
}
