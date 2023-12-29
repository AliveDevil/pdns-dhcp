using System.Threading.Channels;

using Microsoft.Extensions.Hosting;

using pdns_dhcp.Dhcp;
using pdns_dhcp.Dns;

namespace pdns_dhcp.Services;

public class DhcpQueueWorker : BackgroundService
{
	private readonly ChannelReader<DhcpLeaseChange> _channelReader;
	private readonly DnsRepository _repository;

	public DhcpQueueWorker(DhcpLeaseQueue queue, DnsRepository repository)
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
				DnsRecordIdentifier identifier = lease.Identifier switch
				{
					DhcpLeaseClientIdentifier clientId => new DnsRecordClientIdentifier(clientId.ClientId),
					DhcpLeaseHWAddrIdentifier hwAddr => new DnsRecordHWAddrIdentifier(hwAddr.HWAddr),
					_ => throw new ArgumentException(nameof(lease.Identifier))
				};

				TimeSpan lifetime = lease.Lifetime.TotalSeconds switch
				{
					<= 1800 => TimeSpan.FromSeconds(600),
					>= 10800 => TimeSpan.FromSeconds(3600),
					{ } seconds => TimeSpan.FromSeconds(seconds / 3)
				};

				await _repository.Record(new DnsRecord(lease.Address, lease.FQDN, identifier, lifetime), stoppingToken).ConfigureAwait(false);
			}
		}
	}
}
