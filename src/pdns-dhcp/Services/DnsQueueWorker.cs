using System.Threading.Channels;

using Microsoft.Extensions.Hosting;

using pdns_dhcp.Dhcp;
using pdns_dhcp.Dns;

namespace pdns_dhcp.Services;

public class DnsQueueWorker : BackgroundService
{
	private readonly ChannelReader<DhcpLeaseChange> _channelReader;
	private readonly DnsRepository _repository;

	public DnsQueueWorker(DhcpLeaseQueue queue, DnsRepository repository)
	{
		_channelReader = queue.Reader;
		_repository = repository;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (await _channelReader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
		{
			while (_channelReader.TryRead(out var lease))
			{
				await _repository.Record(default, stoppingToken).ConfigureAwait(false);
			}
		}
	}
}
