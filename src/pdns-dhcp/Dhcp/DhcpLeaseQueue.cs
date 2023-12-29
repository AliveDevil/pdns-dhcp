using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Channels;

namespace pdns_dhcp.Dhcp;

public class DhcpLeaseQueue
{
	private readonly Channel<DhcpLeaseChange> _pipe;
	private readonly ChannelReader<DhcpLeaseChange> _reader;
	private readonly ChannelWriter<DhcpLeaseChange> _writer;

	public ref readonly ChannelReader<DhcpLeaseChange> Reader => ref _reader;

	public DhcpLeaseQueue()
	{
		_pipe = Channel.CreateUnbounded<DhcpLeaseChange>();
		_reader = _pipe.Reader;
		_writer = _pipe.Writer;
	}

	public ValueTask Write(DhcpLeaseChange change, CancellationToken cancellationToken = default)
	{
		return _writer.WriteAsync(change, cancellationToken);
	}
}

public readonly record struct DhcpLeaseChange(IPAddress Address, string FQDN, DhcpLeaseIdentifier Identifier, TimeSpan Lifetime)
{
	public AddressFamily LeaseType { get; } = Address.AddressFamily;
}

public record DhcpLeaseIdentifier;
public record DhcpLeaseClientIdentifier(string ClientId) : DhcpLeaseIdentifier;
public record DhcpLeaseHWAddrIdentifier(PhysicalAddress HWAddr) : DhcpLeaseIdentifier;
