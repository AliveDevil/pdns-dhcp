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
	public static KeaDhcp4Lease? Parse(in SepReader.Row row)
	{
		KeaDhcp4Lease result = new();
		for (int i = 0; i < row.ColCount; i++)
		{
			if (Parse(ref result, i, row[i].Span) == false)
			{
				return null;
			}
		}

		return result;
	}

	private static bool? Parse(ref KeaDhcp4Lease lease, int column, in ReadOnlySpan<char> span)
	{
		return column switch
		{
			0 => ToIPAddress(ref lease, span),
			1 when !span.IsWhiteSpace() => ToHWAddr(ref lease, span),
			2 => ToClientId(ref lease, span),
			3 => ToValidLifetime(ref lease, span),
			4 => ToExpire(ref lease, span),
			5 => ToSubnetId(ref lease, span),
			6 => ToFqdnFwd(ref lease, span),
			7 => ToFqdnRev(ref lease, span),
			8 => ToHostname(ref lease, span),
			9 => ToState(ref lease, span),
			10 => ToUserContext(ref lease, span),
			11 => ToPoolId(ref lease, span),
			_ => null
		};

		static bool? ToIPAddress(ref KeaDhcp4Lease lease, in ReadOnlySpan<char> span)
		{
			bool result = IPAddress.TryParse(span, out var address);
			lease.Address = address!;
			return result;
		}

		static bool? ToHWAddr(ref KeaDhcp4Lease lease, in ReadOnlySpan<char> span)
		{
			bool result = PhysicalAddress.TryParse(span, out var hwaddr);
			lease.HWAddr = hwaddr!;
			return result;
		}

		static bool? ToClientId(ref KeaDhcp4Lease lease, in ReadOnlySpan<char> span)
		{
			lease.ClientId = span.ToString();
			return true;
		}

		static bool? ToValidLifetime(ref KeaDhcp4Lease lease, in ReadOnlySpan<char> span)
		{
			bool result = uint.TryParse(span, out var validLifetime);
			lease.ValidLifetime = validLifetime;
			return result;
		}

		static bool? ToExpire(ref KeaDhcp4Lease lease, in ReadOnlySpan<char> span)
		{
			bool result = ulong.TryParse(span, out var expire);
			lease.Expire = DateTimeOffset.FromUnixTimeSeconds(unchecked((long)expire));
			return result;
		}

		static bool? ToSubnetId(ref KeaDhcp4Lease lease, in ReadOnlySpan<char> span)
		{
			bool result = uint.TryParse(span, out var subnetId);
			lease.SubnetId = subnetId;
			return result;
		}

		static bool? ToFqdnFwd(ref KeaDhcp4Lease lease, in ReadOnlySpan<char> span)
		{
			bool result = byte.TryParse(span, out var fqdnFwd);
			lease.FqdnFwd = fqdnFwd != 0;
			return result;
		}

		static bool? ToFqdnRev(ref KeaDhcp4Lease lease, in ReadOnlySpan<char> span)
		{
			bool result = byte.TryParse(span, out var fqdnRev);
			lease.FqdnRev = fqdnRev != 0;
			return result;
		}

		static bool? ToHostname(ref KeaDhcp4Lease lease, in ReadOnlySpan<char> span)
		{
			lease.Hostname = KeaDhcpLease.Unescape(span);
			return true;
		}

		static bool? ToState(ref KeaDhcp4Lease lease, in ReadOnlySpan<char> span)
		{
			bool result = uint.TryParse(span, out var state);
			lease.State = state;
			return result;
		}

		static bool? ToUserContext(ref KeaDhcp4Lease lease, in ReadOnlySpan<char> span)
		{
			lease.UserContext = KeaDhcpLease.Unescape(span);
			return true;
		}

		static bool? ToPoolId(ref KeaDhcp4Lease lease, in ReadOnlySpan<char> span)
		{
			bool result = uint.TryParse(span, out var poolId);
			lease.PoolId = poolId;
			return result;
		}
	}
}
