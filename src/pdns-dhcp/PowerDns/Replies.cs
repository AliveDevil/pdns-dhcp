using System.Text.Json.Serialization;

namespace pdns_dhcp.PowerDns;

[JsonDerivedType(typeof(BoolReply))]
[JsonDerivedType(typeof(LookupReply))]
public abstract class Reply;

public abstract class Reply<T>(T result) : Reply
{
	[JsonPropertyName("result")]
	public T Result => result;
}

public class BoolReply(bool result) : Reply<bool>(result)
{
	public static BoolReply False { get; } = new BoolReply(false);

	public static BoolReply True { get; } = new BoolReply(true);
}

public class LookupReply(QueryResult[] result) : Reply<QueryResult[]>(result);

public record QueryResult(
	[property: JsonPropertyName("qtype")] string QType,
	[property: JsonPropertyName("qname")] string QName,
	[property: JsonPropertyName("content")] string Content,
	[property: JsonPropertyName("ttl")] int TTL
)
{
	[JsonPropertyName("auth")]
	public bool? Auth { get; init; } = default;

	[JsonPropertyName("domain_id")]
	public int? DomainId { get; init; } = default;

	[JsonPropertyName("scopeMask")]
	public int? ScopeMask { get; init; } = default;
}
