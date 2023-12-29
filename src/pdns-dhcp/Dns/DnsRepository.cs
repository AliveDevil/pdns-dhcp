using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

using DotNext.Threading;

using pdns_dhcp.Dhcp;

using Timeout = System.Threading.Timeout;

namespace pdns_dhcp.Dns;

public class DnsRepository
{
	private static ReadOnlySpan<int> Lifetimes => [600, 3600];

	private readonly ReaderWriterLockSlim _recordLock = new();
	private readonly List<DnsRecord> _records = [];

	public List<DnsRecord> Find(Predicate<DnsRecord> query)
	{
		bool enteredLock = false;
		try
		{
			enteredLock = _recordLock.TryEnterReadLock(Timeout.Infinite);
			return _records.FindAll(query);
		}
		finally
		{
			if (enteredLock)
			{
				_recordLock.ExitReadLock();
			}
		}
	}

	public Task<List<DnsRecord>> FindAsync(Predicate<DnsRecord> query, CancellationToken cancellationToken = default)
	{
		return Task.Factory.StartNew(state => Find((Predicate<DnsRecord>)state!), query,
			cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
	}

	public async ValueTask Record(DhcpLeaseChange leaseChange, CancellationToken cancellationToken = default)
	{
		// just lock that thing.
		using (await _recordLock.AcquireLockAsync(cancellationToken).ConfigureAwait(false))
		{
			RecordContinuation(leaseChange);
		}

		void RecordContinuation(DhcpLeaseChange leaseChange)
		{
			var search = Matches(leaseChange);
			bool lockEntered = false;

			try
			{
				lockEntered = _recordLock.TryEnterWriteLock(Timeout.Infinite);
				DnsRecordIdentifier identifier = leaseChange.Identifier switch
				{
					DhcpLeaseClientIdentifier clientId => new DnsRecordClientIdentifier(clientId.ClientId),
					DhcpLeaseHWAddrIdentifier hwAddr => new DnsRecordHWAddrIdentifier(hwAddr.HWAddr),
					_ => throw new ArgumentException(nameof(leaseChange.Identifier))
				};

				TimeSpan lifetime = leaseChange.Lifetime.TotalSeconds switch
				{
					<= 1800 => TimeSpan.FromSeconds(Lifetimes[0]),
					>= 10800 => TimeSpan.FromSeconds(Lifetimes[1]),
					{ } seconds => TimeSpan.FromSeconds(seconds / 3)
				};

				var record = new DnsRecord(leaseChange.Address, leaseChange.FQDN, identifier, lifetime);
				if (search.First is { } node)
				{
					search.RemoveFirst();
					_records[node.Value] = record;
				}
				else
				{
					_records.Add(record);
				}

				while (search.Last is { } replace)
				{
					search.RemoveLast();
					var last = _records.Count - 1;
					if (replace.Value < last)
					{
						_records[replace.Value] = _records[last];
					}

					_records.RemoveAt(last);
				}
			}
			finally
			{
				if (lockEntered)
				{
					_recordLock.ExitWriteLock();
				}
			}
		}

		LinkedList<int> Matches(DhcpLeaseChange query)
		{
			LinkedList<int> list = [];

			for (int i = 0; i < _records.Count; i++)
			{
				var record = _records[i];
				if (record.RecordType != query.LeaseType)
				{
					continue;
				}

				switch ((record.Identifier, query.Identifier))
				{
					case (DnsRecordClientIdentifier recordClientId, DhcpLeaseClientIdentifier queryClientId)
						when StringComparer.InvariantCultureIgnoreCase.Equals(recordClientId.ClientId, queryClientId.ClientId):

					case (DnsRecordHWAddrIdentifier recordHWAddr, DhcpLeaseHWAddrIdentifier queryHWAddr)
						when EqualityComparer<PhysicalAddress>.Default.Equals(recordHWAddr.HWAddr, queryHWAddr.HWAddr):

						list.AddLast(i);
						continue;
				}

				if (EqualityComparer<IPAddress>.Default.Equals(record.Address, query.Address))
				{
					list.AddLast(i);
				}
				// Opt-In to disallow duplicate FQDN?
				//else if (StringComparer.InvariantCultureIgnoreCase.Equals(record.FQDN, query.FQDN))
				//{
				//	list.AddLast(i);
				//}
			}

			return list;
		}
	}
}

// TODO Remove duplication
public record DnsRecordIdentifier;
public record DnsRecordClientIdentifier(string ClientId) : DnsRecordIdentifier;
public record DnsRecordHWAddrIdentifier(PhysicalAddress HWAddr) : DnsRecordIdentifier;
// /TODO

public record DnsRecord(IPAddress Address, string FQDN, DnsRecordIdentifier Identifier, TimeSpan Lifetime)
{
	public AddressFamily RecordType { get; } = Address.AddressFamily;
}
