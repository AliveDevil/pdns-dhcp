using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using pdns_dhcp.Options;
using pdns_dhcp.PowerDns;
using pdns_dhcp.Services;

using Stl.Interception;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<KeaDhcpOptions>(builder.Configuration.GetSection("KeaDhcp"));
builder.Services.Configure<PowerDnsOptions>(builder.Configuration.GetSection("PowerDns"));

builder.Services.AddHostedService<DhcpLeaseWatcher>();
builder.Services.AddHostedService<PowerDnsBackend>();

builder.Services.AddTypedFactory<SocketFactory>();

builder.Build().Run();
