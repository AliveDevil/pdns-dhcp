using Microsoft.AspNetCore.Connections;

namespace pdns_dhcp.PowerDns;

public class PowerDnsHandler : ConnectionHandler
{
	public override Task OnConnectedAsync(ConnectionContext connection)
	{
		return Task.CompletedTask;
	}
}
