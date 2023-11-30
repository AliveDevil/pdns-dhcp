using System.Net;
using System.Net.NetworkInformation;

using nietras.SeparatedValues;

namespace pdns_dhcp.Kea;

// ref: https://github.com/isc-projects/kea/blob/Kea-2.5.3/src/lib/dhcpsrv/csv_lease_file6.h
public record struct KeaDhcp6Lease(
	IPAddress Address,
	string DUId,
	uint ValidLifetime,
	DateTimeOffset Expire,
	uint SubnetId,
	uint PrefLifetime,
	LeaseType LeaseType,
	uint IAId,
	byte PrefixLen,
	bool FqdnFwd,
	bool FqdnRev,
	string Hostname,
	PhysicalAddress HWAddr,
	uint State,
	string? UserContext,
	ushort? HWType,
	uint? HWAddrSource,
	uint PoolId)
{
	public static KeaDhcp6Lease Parse(in SepReader.Row row)
	{
		KeaDhcp6Lease result = new();
		for (int i = 0; i < row.ColCount; i++)
		{
			var span = row[i].Span;
			switch (i)
			{
				case 0:
					result.Address = IPAddress.Parse(span);
					break;

				case 1:
					result.DUId = span.ToString();
					break;

				case 2:
					result.ValidLifetime = uint.Parse(span);
					break;

				case 3:
					result.Expire = DateTimeOffset.FromUnixTimeSeconds(unchecked((long)ulong.Parse(span)));
					break;

				case 4:
					result.SubnetId = uint.Parse(span);
					break;

				case 5:
					result.PrefLifetime = uint.Parse(span);
					break;

				case 6:
					result.LeaseType = (LeaseType)byte.Parse(span);
					break;

				case 7:
					result.IAId = uint.Parse(span);
					break;

				case 8:
					result.PrefixLen = byte.Parse(span);
					break;

				case 9:
					result.FqdnFwd = byte.Parse(span) != 0;
					break;

				case 10:
					result.FqdnRev = byte.Parse(span) != 0;
					break;

				case 11:
					result.Hostname = KeaDhcpLease.Unescape(span);
					break;

				case 12 when !span.IsWhiteSpace():
					result.HWAddr = PhysicalAddress.Parse(span);
					break;

				case 13:
					result.State = uint.Parse(span);
					break;

				case 14 when !span.IsWhiteSpace():
					result.UserContext = KeaDhcpLease.Unescape(span);
					break;

				case 15:
					result.HWType = ushort.Parse(span);
					break;

				case 16:
					result.HWAddrSource = uint.Parse(span);
					break;

				case 17:
					result.PoolId = uint.Parse(span);
					break;
			}
		}

		return result;
	}
}
