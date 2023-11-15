using System.Net.Sockets;

using Microsoft.Extensions.Hosting;

namespace pdns_dhcp.PowerDns;

public class PowerDnsUnixSocket : BackgroundService
{
	private readonly IPowerDnsFactory _factory;
	private readonly Socket _socket;

	public PowerDnsUnixSocket(string socketPath, IPowerDnsFactory factory)
	{
		_factory = factory;
		_socket = new(SocketType.Stream, ProtocolType.Unspecified);
		_socket.Bind(new UnixDomainSocketEndPoint(socketPath));
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_socket.Listen();
		while (await _socket.AcceptAsync(stoppingToken) is { } client)
		{
			var instance = _factory.CreateClient(new NetworkStream(client, true));
		}
	}
}
