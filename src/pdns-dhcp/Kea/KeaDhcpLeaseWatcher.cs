using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Channels;

using Microsoft.Extensions.Hosting;

using nietras.SeparatedValues;

using pdns_dhcp.Options;

namespace pdns_dhcp.Kea;

public sealed class KeaDhcpLeaseWatcher<T> : IHostedService
	where T : IKeaDhcpLeaseHandler
{
	private static readonly FileStreamOptions LeaseFileStreamOptions = new()
	{
		Access = FileAccess.Read,
		Mode = FileMode.Open,
		Options = FileOptions.SequentialScan,
		Share = (FileShare)7,
	};

	private readonly Decoder _decoder;
	private readonly FileSystemWatcher _fsw;
	private readonly T _handler;
	private readonly string _leaseFile;
	private readonly Pipe _pipe;
	private Channel<FileSystemEventArgs>? _eventChannel;
	private Task? _executeTask;
	private CancellationTokenSource? _stoppingCts;

	private KeaDhcpServerOptions Options { get; }

	public KeaDhcpLeaseWatcher(KeaDhcpServerOptions options, T handler)
	{
		Options = options;
		_handler = handler;

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

		_decoder = Encoding.UTF8.GetDecoder();
		_leaseFile = leaseFile.ToString();
		_fsw = new(leaseDirectory, _leaseFile);
		_fsw.Changed += OnLeaseChanged;
		_fsw.Error += OnLeaseError;
		_pipe = new();
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
			var loopToken = loopCts.Token;
			_eventChannel = Channel.CreateUnbounded<FileSystemEventArgs>();
			_fsw.EnableRaisingEvents = true;
			using AutoResetEvent resetEvent = new(false);
			Task reader = Task.CompletedTask;
			try
			{
				for (
					EventArgs readArgs = EventArgs.Empty;
					!loopToken.IsCancellationRequested;
					readArgs = await _eventChannel.Reader.ReadAsync(loopToken))
				{
					// Guard for Deleted and renamed away events,
					// both have to stop this reader immediately.
					// Just wait for the file being created/renamed to _leaseFile.
					// Described in [The LFC Process](https://kea.readthedocs.io/en/latest/arm/lfc.html#kea-lfc)
					switch (readArgs)
					{
						case FileSystemEventArgs { ChangeType: WatcherChangeTypes.Deleted }:
						case RenamedEventArgs renamed when renamed.OldName == _leaseFile:
							loopCts.Cancel();
							continue;
					}

					if (reader is not { IsCompleted: false })
					{
						// In any case that the reader failed (for whatever reason)
						// restart now.
						// Incoming event could be Changed/Created/Renamed
						// This doesn't care, as we already lost the file handle.
						reader = FileReader(resetEvent, stoppingToken);
					}
					else
					{
						resetEvent.Set();
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

	private async Task FileReader(AutoResetEvent waitHandle, CancellationToken stoppingToken)
	{
		PipeWriter writer = _pipe.Writer;
		SepReader? reader = null;
		try
		{
			using var file = new FileStream(Options.Leases, LeaseFileStreamOptions);

			bool awaitLineFeed = false;
			int newLinesEncountered = 0;
			while (!stoppingToken.IsCancellationRequested)
			{
				for (; newLinesEncountered > 0; newLinesEncountered--)
				{
					if (reader is null)
					{
						reader = Sep.Reader().From(_pipe.Reader.AsStream());
						continue;
					}

					if (!reader.MoveNext())
					{
						// TODO Error state.
						return;
					}
				}

				var memory = writer.GetMemory();
				int read = await file.ReadAsync(memory, stoppingToken);
				if (read > 0)
				{
					CountNewLines(_decoder, memory[..read], ref newLinesEncountered, ref awaitLineFeed);
					writer.Advance(read);
					await writer.FlushAsync(stoppingToken);
				}
				else
				{
					await waitHandle.WaitOneAsync(stoppingToken).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
		}
		finally
		{
			reader?.Dispose();
			writer.Complete();
			_pipe.Reset();
		}

		static void CountNewLines(Decoder decoder, in Memory<byte> memory, ref int newLinesEncountered, ref bool awaitLineFeed)
		{
			Span<char> buffer = stackalloc char[128];
			bool completed = false;
			ReadOnlySequence<byte> sequence = new(memory);
			var reader = new SequenceReader<byte>(sequence);
			while (!reader.End)
			{
				decoder.Convert(reader.UnreadSpan, buffer, false, out var bytesUsed, out var charsUsed, out completed);
				reader.Advance(bytesUsed);
				foreach (ref readonly char c in buffer[..charsUsed])
				{
					if (awaitLineFeed || c == '\n')
					{
						newLinesEncountered++;
						awaitLineFeed = false;
					}
					else if (c == '\r')
					{
						awaitLineFeed = true;
					}
				}
			}
		}
	}

	private void OnLeaseChanged(object sender, FileSystemEventArgs e)
	{
		if (_eventChannel?.Writer is not { } writer)
		{
			return;
		}

#pragma warning disable CA2012
		var task = writer.WriteAsync(e, CancellationToken.None);
#pragma warning restore
		if (task.IsCompleted)
		{
			return;
		}

		task.GetAwaiter().GetResult();
	}


	private void OnLeaseError(object sender, ErrorEventArgs e)
	{
		_eventChannel?.Writer.Complete(e.GetException());
	}
}
