using System.Net;
using System.Net.NetworkInformation;

using nietras.SeparatedValues;

namespace pdns_dhcp.Kea;

// ref: https://github.com/isc-projects/kea/blob/Kea-2.5.3/src/lib/dhcpsrv/csv_lease_file4.h
public record struct KeaDhcp4Lease(
	IPAddress Address,
	PhysicalAddress HWAddr,
	string? ClientId,
	uint ValidLifetime,
	DateTimeOffset Expire,
	uint SubnetId,
	bool FqdnFwd,
	bool FqdnRev,
	string Hostname,
	uint State,
	string? UserContext,
	uint PoolId)
{
	public static KeaDhcp4Lease Parse(in SepReader.Row row)
	{
		var address = IPAddress.Parse(row[0].Span);
		PhysicalAddress hwaddr = PhysicalAddress.None;
		if (row[1].Span is { IsEmpty: false } physical)
		{
			hwaddr = PhysicalAddress.Parse(physical);
		}
		string? clientId = row[2].ToString();
		uint validLifetime = uint.Parse(row[3].Span);
		DateTimeOffset expire = DateTimeOffset.FromUnixTimeSeconds(unchecked((long)ulong.Parse(row[4].Span)));
		uint subnetId = uint.Parse(row[5].Span);
		bool fqdnFwd = sbyte.Parse(row[6].Span) != 0;
		bool fqdnRev = sbyte.Parse(row[7].Span) != 0;
		string hostname = KeaDhcpLease.Unescape(row[8].Span);

		uint state = 0;
		if (row.ColCount > 9)
		{
			state = uint.Parse(row[9].Span);
		}

		string? userContext = default;
		if (row.ColCount > 10)
		{
			userContext = KeaDhcpLease.Unescape(row[10].Span);
		}

		uint poolId = 0;
		if (row.ColCount > 11)
		{
			poolId = uint.Parse(row[11].Span);
		}

		return new(
			address,
			hwaddr,
			clientId,
			validLifetime,
			expire,
			subnetId,
			fqdnFwd,
			fqdnRev,
			hostname,
			state,
			userContext,
			poolId);
	}
}
