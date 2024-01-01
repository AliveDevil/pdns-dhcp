using System.Net;
using System.Net.NetworkInformation;

using nietras.SeparatedValues;

using Cell = System.ReadOnlySpan<char>;

namespace pdns_dhcp.Kea;

using Lease = KeaDhcp6Lease;

// ref: https://github.com/isc-projects/kea/blob/Kea-2.5.3/src/lib/dhcpsrv/csv_lease_file6.h
public record struct KeaDhcp6Lease(
	IPAddress Address,
	string DUId,
	TimeSpan ValidLifetime,
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
	public static Lease? Parse(in SepReader.Row row)
	{
		Lease result = new();
		for (int i = 0; i < row.ColCount; i++)
		{
			if (Parse(ref result, i, row[i].Span) == false)
			{
				return null;
			}
		}

		return result;
	}

	private static bool? Parse(ref Lease lease, int column, in Cell span)
	{
		return column switch
		{
			0 => ToAddress(ref lease, span),
			1 => ToDUId(ref lease, span),
			2 => ToValidLifetime(ref lease, span),
			3 => ToExpire(ref lease, span),
			4 => ToSubnetId(ref lease, span),
			5 => ToPrefLifetime(ref lease, span),
			6 => ToLeaseType(ref lease, span),
			7 => ToIAId(ref lease, span),
			8 => ToPrefixLen(ref lease, span),
			9 => ToFqdnFwd(ref lease, span),
			10 => ToFqdnRev(ref lease, span),
			11 => ToHostname(ref lease, span),
			12 when !span.IsWhiteSpace() => ToHWAddr(ref lease, span),
			13 => ToState(ref lease, span),
			14 when !span.IsWhiteSpace() => ToUserContext(ref lease, span),
			15 => ToHWType(ref lease, span),
			16 => ToHWAddrSource(ref lease, span),
			17 => ToPoolId(ref lease, span),

			_ => null
		};

		static bool ToAddress(ref Lease lease, in Cell span)
		{
			bool result = IPAddress.TryParse(span, out var address);
			lease.Address = address!;
			return result;
		}

		static bool ToDUId(ref Lease lease, in Cell span)
		{
			lease.DUId = span.ToString();
			return true;
		}

		static bool ToValidLifetime(ref Lease lease, in Cell span)
		{
			bool result = uint.TryParse(span, out var validLifetime);
			lease.ValidLifetime = TimeSpan.FromSeconds(validLifetime);
			return result;
		}

		static bool ToExpire(ref Lease lease, in Cell span)
		{
			bool result = ulong.TryParse(span, out var expire);
			lease.Expire = DateTimeOffset.FromUnixTimeSeconds(unchecked((long)expire));
			return result;
		}

		static bool ToSubnetId(ref Lease lease, in Cell span)
		{
			bool result = uint.TryParse(span, out var subnetId);
			lease.SubnetId = subnetId;
			return result;
		}

		static bool ToPrefLifetime(ref Lease lease, in Cell span)
		{
			bool result = uint.TryParse(span, out var prefLifetime);
			lease.PrefLifetime = prefLifetime;
			return result;
		}

		static bool ToLeaseType(ref Lease lease, in Cell span)
		{
			bool result = byte.TryParse(span, out var leaseType);
			lease.LeaseType = (LeaseType)leaseType;
			return result;
		}

		static bool ToIAId(ref Lease lease, in Cell span)
		{
			bool result = uint.TryParse(span, out var iaId);
			lease.IAId = iaId;
			return result;
		}

		static bool ToPrefixLen(ref Lease lease, in Cell span)
		{
			bool result = byte.TryParse(span, out var prefixLen);
			lease.PrefixLen = prefixLen;
			return result;
		}

		static bool ToFqdnFwd(ref Lease lease, in Cell span)
		{
			bool result = byte.TryParse(span, out var fqdnFwd);
			lease.FqdnFwd = fqdnFwd != 0;
			return result;
		}

		static bool ToFqdnRev(ref Lease lease, in Cell span)
		{
			bool result = byte.TryParse(span, out var fqdnRev);
			lease.FqdnRev = fqdnRev != 0;
			return result;
		}

		static bool ToHostname(ref Lease lease, in Cell span)
		{
			lease.Hostname = KeaDhcpLease.Unescape(span);
			return true;
		}

		static bool ToHWAddr(ref Lease lease, in Cell span)
		{
			bool result = PhysicalAddress.TryParse(span, out var hwAddr);
			lease.HWAddr = hwAddr!;
			return result;
		}

		static bool ToState(ref Lease lease, in Cell span)
		{
			bool result = uint.TryParse(span, out var state);
			lease.State = state;
			return result;
		}

		static bool ToUserContext(ref Lease lease, in Cell span)
		{
			lease.UserContext = KeaDhcpLease.Unescape(span);
			return true;
		}

		static bool ToHWType(ref Lease lease, in Cell span)
		{
			bool result = ushort.TryParse(span, out var hwType);
			lease.HWType = hwType;
			return result;
		}

		static bool ToHWAddrSource(ref Lease lease, in Cell span)
		{
			bool result = uint.TryParse(span, out var hwAddrSource);
			lease.HWAddrSource = hwAddrSource;
			return result;
		}

		static bool ToPoolId(ref Lease lease, in Cell span)
		{
			bool result = uint.TryParse(span, out var poolId);
			lease.PoolId = poolId;
			return result;
		}
	}
}
