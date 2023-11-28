namespace pdns_dhcp.Options;

public class PowerDnsOptions
{
	public PowerDnsListenerOptions Listener { get; set; } = default!;
}

public record class PowerDnsListenerOptions(string Socket);
