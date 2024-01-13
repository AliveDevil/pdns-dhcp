# pdns-dhcp

Enabling PowerDNS to query popular supported Dhcp-servers.

This project was born out of the necessity for my home-lab network to be able to resolve both IPv4 and IPv6 addresses from one Dhcp-service.

Theoretically Kea can update DNS servers using RFC2136 nsupdate-mechanisms using kea-ddns, but this interoperation can cause issues in networks with devices sharing a hostname (i.e. DHCID records), missing update requests due to service restarts or temporary connectivity issues.

## Scope

At the moment there is no need to implement more than is minimally required to get Dhcp4 and Dhcp6 leases queryable by PowerDNS using the memfile "database" of Kea using the remote backend with unix domain sockets.

Following parts may be implemented later as I see fit:
- Different PowerDNS remote backends
  - mainly HTTP REST
- Support different Kea lease databases
  - MySQL
  - PostgreSQL

## Building

Requires .NET 8 SDK  
Create binary using
> dotnet publish -c Release -p:PublishTrimmed=true -p:PublishSingleFile=true --self-contained

## Usage

Install, and configure Kea (optionally with Stork) Dhcp4, Dhcp6 or both.  
Make sure to enable the memfile lease store.

Install and configure PowerDNS, including the [remote backend](https://doc.powerdns.com/authoritative/backends/remote.html).  
A sample configuration file is provided.

Deploy pdns-dhcp to /opt/pdns-dhcp  
Setup systemd using the provided socket and service units, configure as necessary.

Start Kea, pdns-dhcp and PowerDNS.

**To be done**: Packaging for common Linux distributions.  
Deb-packages (Debian)  
RPM-packages (EL)

### Configuration

pdns-dhcp can be configured using environment variables or the appsettings.json file - [Configuration#Binding hierarchies](https://learn.microsoft.com/dotnet/core/extensions/configuration#binding-hierarchies) describes the naming scheme in this section.

Default configuration:
```
Dhcp:Kea:Dhcp4:Leases=/var/lib/kea/kea-leases4.csv
Dhcp:Kea:Dhcp6:Leases=/var/lib/kea/kea-leases6.csv
PowerDns:UniqueHostnames=true
PowerDns:Listener:Socket=/run/pdns-dhcp/pdns.sock
```

`Dhcp:Kea` allows configuring `Dhcp4` and `Dhcp6` lease file watchers, respective for each of both services.

In `PowerDns:Listener:Socket` you can optionally configure the unix domain socket to be used in case Systemd isn't providing them (e.g. when starting the service manually).

pdns-dhcp continuously monitors the Dhcp service leases and upon seeing a new lease all previous records that match in hostname and lease type (IPv4, IPv6) are replaced. If you want to change this behavior you can opt-out of this behavior by setting `PowerDns:UniqueHostnames=false`.

See [Logging in C#](https://learn.microsoft.com/dotnet/core/extensions/logging?tabs=command-line#configure-logging-without-code) for options related to logging.

## Acknowledgments

Incorporates following libraries directly:

**.NET Foundation and Contributors**
- [CommunityToolkit.HighPerformance](https://github.com/CommunityToolkit/dotnet) - MIT
- [dotNext.Threading](https://github.com/dotnet/dotNext) - MIT
- Several runtime libraries, as part of [.NET](https://github.com/dotnet/runtime)
  - Microsoft.AspNetCore.App
  - Microsoft.Extensions.Configuration.Binder
  - Microsoft.Extensions.Hosting.Systemd
  - System.IO.Pipelines

**[Nietras](https://github.com/nietras)**
- [Sep](https://github.com/nietras/Sep) - MIT

Incorporates data structures and protocol implementations as required for interop scenarios:

- [kea](https://gitlab.isc.org/isc-projects/kea) by [ISC](https://isc.org/) - MPL 2.0
- [PowerDNS](https://github.com/PowerDNS/pdns) by [PowerDNS.COM BV](https://www.powerdns.com/) and contributors - GPL 2.0
