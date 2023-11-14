namespace pdns_dhcp.Options;

public class KeaDhcpOptions
{
	public KeaDhcpServerOptions? Dhcp4 { get; set; }

	public KeaDhcpServerOptions? Dhcp6 { get; set; }
}

public record class KeaDhcpServerOptions(FileInfo Leases);
