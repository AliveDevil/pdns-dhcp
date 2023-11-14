namespace pdns_dhcp.Options;

public class PowerDnsOptions
{
	public PowerDnsListenerOptions Listener { get; set; }
}

public class PowerDnsListenerOptions(string socket)
{
	public string Socket { get; } = socket;
}
