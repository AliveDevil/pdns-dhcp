using Microsoft.Extensions.Hosting;

using pdns_dhcp.Options;

namespace pdns_dhcp.Kea;

public abstract class KeaDhcpLeaseWatcher : BackgroundService
{
	private readonly FileSystemWatcher _fsw;

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

		_fsw = new(leaseDirectory, leaseFile.ToString());
		_fsw.Changed += OnLeaseChanged;
		_fsw.Created += OnLeaseChanged;
		_fsw.Deleted += OnLeaseChanged;
		_fsw.Renamed += OnLeaseRenamed;
		_fsw.Error += OnLeaseError;
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		return Task.CompletedTask;
	}

	private void OnLeaseChanged(object sender, FileSystemEventArgs e)
	{
		throw new NotImplementedException();
	}

	private void OnLeaseError(object sender, ErrorEventArgs e)
	{
		throw new NotImplementedException();
	}

	private void OnLeaseRenamed(object sender, RenamedEventArgs e)
	{
		throw new NotImplementedException();
	}
}
