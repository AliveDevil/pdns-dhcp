using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
builder.Services.AddTypedFactory<IPowerDnsFactory>();

builder.Services.AddTransient<KeaDhcp4LeaseHandler>();
builder.Services.AddTransient<KeaDhcp6LeaseHandler>();

builder.Build().Run();
