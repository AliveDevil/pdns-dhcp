namespace pdns_dhcp.Options;

public class PowerDnsOptions(PowerDnsListenerOptions listener)
{
	public PowerDnsListenerOptions Listener { get; } = listener;
}

public class PowerDnsListenerOptions(string socket)
{
	public string Socket { get; } = socket;
}
