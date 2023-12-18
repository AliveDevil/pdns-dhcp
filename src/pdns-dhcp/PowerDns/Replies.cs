using System.Text.Json;
using System.Text.Json.Serialization;

namespace pdns_dhcp.PowerDns;

[JsonDerivedType(typeof(BoolReply))]
[JsonDerivedType(typeof(LookupReply))]
public abstract class Reply;

[JsonSerializable(typeof(Reply)), JsonSourceGenerationOptions(
	GenerationMode = JsonSourceGenerationMode.Serialization,
	PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower,
	WriteIndented = false
)]
internal partial class ReplyContext : JsonSerializerContext;

public abstract class Reply<T>(T result) : Reply
{
	public T Result => result;
}

public class BoolReply(bool result) : Reply<bool>(result)
{
	public static Reply False { get; } = new BoolReply(false);

	public static Reply True { get; } = new BoolReply(true);
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
