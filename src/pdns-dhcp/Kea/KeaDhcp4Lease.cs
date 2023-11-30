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
		KeaDhcp4Lease result = new();
		for (int i = 0; i < row.ColCount; i++)
		{
			var span = row[i].Span;
			switch (i)
			{
				case 0:
					result.Address = IPAddress.Parse(span);
					break;

				case 1 when !span.IsWhiteSpace():
					result.HWAddr = PhysicalAddress.Parse(span);
					break;

				case 2:
					result.ClientId = span.ToString();
					break;

				case 3:
					result.ValidLifetime = uint.Parse(span);
					break;

				case 4:
					result.Expire = DateTimeOffset.FromUnixTimeSeconds(unchecked((long)ulong.Parse(span)));
					break;

				case 5:
					result.SubnetId = uint.Parse(span);
					break;

				case 6:
					result.FqdnFwd = byte.Parse(span) != 0;
					break;

				case 7:
					result.FqdnRev = byte.Parse(span) != 0;
					break;

				case 8:
					result.Hostname = KeaDhcpLease.Unescape(span);
					break;

				case 9:
					result.State = uint.Parse(span);
					break;

				case 10:
					result.UserContext = KeaDhcpLease.Unescape(span);
					break;

				case 11:
					result.PoolId = uint.Parse(span);
					break;
			}
		}

		return result;
	}
}
