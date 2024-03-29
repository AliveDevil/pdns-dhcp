﻿using System.Net;
using System.Net.Sockets;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using pdns_dhcp.Connections;
using pdns_dhcp.Dhcp;
using pdns_dhcp.Dns;
using pdns_dhcp.Kea;
using pdns_dhcp.Options;
using pdns_dhcp.PowerDns;
using pdns_dhcp.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSystemd();

builder.Services.Configure<DhcpOptions>(builder.Configuration.GetRequiredSection("Dhcp"));
builder.Services.Configure<PowerDnsOptions>(builder.Configuration.GetRequiredSection("PowerDns"));

builder.Services.AddHostedService<DhcpWatcher>();
builder.Services.AddHostedService<DhcpQueueWorker>();

builder.Services.AddSingleton<DhcpLeaseQueue>();
builder.Services.AddSingleton<DnsRepository>();

builder.Services.AddDhcpWatcherFactory();
builder.Services.AddKeaFactory();

builder.Services.Configure<SocketTransportOptions>(options =>
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

builder.WebHost.ConfigureKestrel((context, options) =>
{
	bool isSystemd = false;
	options.UseSystemd(options =>
	{
		isSystemd = true;
		options.UseConnectionHandler<PowerDnsHandler>();
	});
	
	if (!isSystemd && context.Configuration.GetRequiredSection("PowerDns:Listener").Get<PowerDnsListenerOptions>() is { } pdnsOptions)
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
