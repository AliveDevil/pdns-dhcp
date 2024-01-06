namespace pdns_dhcp.Options;

public class PowerDnsOptions
{
	public PowerDnsListenerOptions Listener { get; init; } = default!;

	public bool UniqueHostnames { get; init; } = true;
}

public record class PowerDnsListenerOptions(string Socket);
