namespace pdns_dhcp.Kea;

// ref: https://github.com/isc-projects/kea/blob/Kea-2.5.3/src/lib/dhcpsrv/csv_lease_file6.h
public record struct KeaDhcp6Lease(
	string Address,
	string DUId,
	uint ValidLifetime,
	ulong Expire,
	string SubnetId,
	uint PrefLifetime,
	LeaseType LeaseType,
	uint IAId,
	byte PrefixLen,
	byte FqdnFwd,
	byte FqdnRev,
	string Hostname,
	string HWAddr,
	uint State,
	string UserContext,
	ushort? HWType,
	uint? HWAddrSource,
	uint PoolId
);
