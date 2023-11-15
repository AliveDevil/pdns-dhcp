using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using pdns_dhcp.Options;
using pdns_dhcp.PowerDns;
using pdns_dhcp.Services;

using Stl.Interception;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<DhcpOptions>(builder.Configuration.GetRequiredSection("Dhcp"));
builder.Services.Configure<PowerDnsOptions>(builder.Configuration.GetRequiredSection("PowerDns"));

builder.Services.AddHostedService<DhcpLeaseWatcher>();
builder.Services.AddHostedService<PowerDnsBackend>();

builder.Services.AddTypedFactory<IPowerDnsFactory>();

builder.Build().Run();
