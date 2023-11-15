namespace pdns_dhcp.PowerDns;

public class PowerDnsStreamClient : IDisposable
{
	private readonly Stream _stream;

	public PowerDnsStreamClient(Stream stream)
	{
		_stream = stream;
	}

	~PowerDnsStreamClient()
	{
		Dispose(false);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		_stream.Dispose();
	}
}
