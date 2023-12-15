using System.Net;
using System.Net.Sockets;

namespace pdns_dhcp.Connections;

public class ProtocolSocketIPEndPoint(IPAddress address, int port, ProtocolType protocolType, SocketType socketType) : IPEndPoint(address, port)
{
	public ProtocolType ProtocolType => protocolType;

	public SocketType SocketType => socketType;
}
