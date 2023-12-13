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
		_socket = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
		var path = PathEx.ExpandPath(options.Value.Listener.Socket);
		FileInfo file = new(path);
		file.Directory!.Create();
		file.Delete();
		_socket.Bind(new UnixDomainSocketEndPoint(path));
	}

	~PowerDnsBackend()
	{
		DisposeCore();
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

	public override void Dispose()
	{
		base.Dispose();
		DisposeCore();
		GC.SuppressFinalize(this);
	}

	private void DisposeCore()
	{
		_socket.Dispose();
	}
}
