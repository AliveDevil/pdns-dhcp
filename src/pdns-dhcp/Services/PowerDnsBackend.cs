using System.Net.Sockets;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using pdns_dhcp.Options;
using pdns_dhcp.PowerDns;

namespace pdns_dhcp.Services;

public class PowerDnsBackend : BackgroundService
{
	private readonly IPowerDnsFactory _factory;
	private readonly Socket _socket;

	public PowerDnsBackend(IOptions<PowerDnsOptions> options, IPowerDnsFactory socketFactory)
	{
		_factory = socketFactory;
		_socket = new(SocketType.Stream, ProtocolType.Unknown);
		_socket.Bind(new UnixDomainSocketEndPoint(options.Value.Listener.Socket));
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_socket.Listen();
		while (await _socket.AcceptAsync(stoppingToken) is { } client)
		{
			_factory.CreateClient(new NetworkStream(client, true))
				.Start(stoppingToken);
		}
	}
}
