namespace pdns_dhcp.Kea;

// ref: https://github.com/isc-projects/kea/blob/Kea-2.5.3/src/lib/dhcpsrv/csv_lease_file6.h
public record struct KeaDhcp6Lease(
	string Address,
	string DUId,
	UInt32 ValidLifetime,
	UInt64 Expire,
	string SubnetId,
	UInt32 PrefLifetime,
	LeaseType LeaseType,
	UInt32 IAId,
	Byte PrefixLen,
	byte FqdnFwd,
	byte FqdnRev,
	string Hostname,
	string HWAddr,
	UInt32 State,
	string UserContext,
	UInt16? HWType,
	UInt32? HWAddrSource,
	UInt32 PoolId
);
