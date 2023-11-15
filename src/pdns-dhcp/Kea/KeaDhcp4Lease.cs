namespace pdns_dhcp.Kea;

// ref: https://github.com/isc-projects/kea/blob/Kea-2.5.3/src/lib/dhcpsrv/csv_lease_file4.h
public record struct KeaDhcp4Lease(
	string Address,
	string HWAddr,
	string? ClientId,
	UInt32 ValidLifetime,
	UInt64 Expire,
	string SubnetId,
	byte FqdnFwd,
	byte FqdnRev,
	string Hostname,
	UInt32 State,
	string UserContext,
	UInt32 PoolId
);
