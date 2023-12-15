using System.Net;
using System.Net.Sockets;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using pdns_dhcp.Connections;
using pdns_dhcp.Kea;
using pdns_dhcp.Options;
using pdns_dhcp.PowerDns;
using pdns_dhcp.Services;

using Stl.Interception;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DhcpOptions>(builder.Configuration.GetRequiredSection("Dhcp"));
builder.Services.Configure<PowerDnsOptions>(builder.Configuration.GetRequiredSection("PowerDns"));

builder.Services.AddHostedService<DhcpLeaseWatcher>();
builder.Services.AddHostedService<PowerDnsBackend>();

builder.Services.AddTypedFactory<IDhcpLeaseWatcherFactory>();

builder.Services.AddTransient<KeaDhcp4LeaseHandler>();
builder.Services.AddTransient<KeaDhcp6LeaseHandler>();

builder.WebHost.UseSockets(options =>
{
	options.CreateBoundListenSocket = endpoint =>
	{
		Socket socket;
		switch (endpoint)
		{
			case ProtocolSocketIPEndPoint socketIp:
				socket = new Socket(socketIp.AddressFamily, socketIp.SocketType, socketIp.ProtocolType);

				if (socketIp.Address.Equals(IPAddress.IPv6Any))
				{
					socket.DualMode = true;
				}

				break;

			default:
				return SocketTransportOptions.CreateDefaultBoundListenSocket(endpoint);
		}

		socket.Bind(endpoint);
		return socket;
	};
});

builder.WebHost.UseKestrelCore();
builder.WebHost.ConfigureKestrel((context, options) =>
{
	if (context.Configuration.GetRequiredSection("PowerDns:Listener").Get<PowerDnsListenerOptions>() is { } pdnsOptions)
	{
		var path = PathEx.ExpandPath(pdnsOptions.Socket);
		FileInfo file = new(path);
		file.Directory!.Create();
		file.Delete();
		options.ListenUnixSocket(path, options =>
		{
			options.UseConnectionHandler<PowerDnsHandler>();
		});
	}
});

var app = builder.Build();

app.Run();
