using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

using Timeout = System.Threading.Timeout;

namespace pdns_dhcp.Dns;

public class DnsRepository
{
	private static ReadOnlySpan<int> Lifetimes => [600, 3600];

	private readonly ReaderWriterLockSlim _recordLock = new();
	private readonly List<DnsRecord> _records = [];
	private readonly SemaphoreSlim _syncLock = new(1, 1);

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

	public async ValueTask Record(DnsRecord record, CancellationToken cancellationToken = default)
	{
		bool entered = false;
		try
		{
			entered = await _syncLock.WaitAsync(Timeout.Infinite, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			RecordContinuation(record);
		}
		finally
		{
			if (entered)
			{
				_syncLock.Release();
			}
		}

		void RecordContinuation(DnsRecord record)
		{
			var search = Matches(record);
			bool entered = false;
			try
			{
				entered = _recordLock.TryEnterWriteLock(Timeout.Infinite);

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
				if (entered)
				{
					_recordLock.ExitWriteLock();
				}
			}
		}

		LinkedList<int> Matches(DnsRecord query)
		{
			LinkedList<int> list = [];

			for (int i = 0; i < _records.Count; i++)
			{
				var record = _records[i];
				if (record.RecordType != query.RecordType)
				{
					continue;
				}

				switch ((record.Identifier, query.Identifier))
				{
					case (
						DnsRecordClientIdentifier { ClientId: { } recordClientId },
						DnsRecordClientIdentifier { ClientId: { } queryClientId }
						) when StringComparer.InvariantCultureIgnoreCase.Equals(recordClientId, queryClientId):

					case (
						DnsRecordHWAddrIdentifier { HWAddr: { } recordHWAddr },
						DnsRecordHWAddrIdentifier { HWAddr: { } queryHWAddr }
						) when EqualityComparer<PhysicalAddress>.Default.Equals(recordHWAddr, queryHWAddr):

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
