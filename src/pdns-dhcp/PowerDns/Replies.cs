namespace pdns_dhcp.PowerDns;

public abstract class Reply;

public abstract class Reply<T>(T result) : Reply
{
	public T Result => result;
}

public class BoolReply(bool result) : Reply<bool>(result);
