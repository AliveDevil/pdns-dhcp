namespace pdns_dhcp.Kea;

public enum LeaseType : byte
{
	NonTempraryIPv6 = 0,
	TemporaryIPv6 = 1,
	IPv6Prefix = 2,
	IPv4 = 3
}
