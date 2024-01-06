using System.Text.Json;
using System.Text.Json.Serialization;

namespace pdns_dhcp.PowerDns;

[JsonDerivedType(typeof(InitializeParameters))]
[JsonDerivedType(typeof(LookupParameters))]
public record class Parameters;

public record class MethodParameters : Parameters
{
	[JsonExtensionData]
	public Dictionary<string, JsonElement> AdditionalProperties { get; set; } = [];
}

public record class InitializeParameters(
	[property: JsonPropertyName("command")] string Command,
	[property: JsonPropertyName("timeout")] int Timeout
) : MethodParameters;

public record class LookupParameters(
	[property: JsonPropertyName("qtype")] string Qtype,
	[property: JsonPropertyName("qname")] string Qname,
	[property: JsonPropertyName("remote")] string Remote,
	[property: JsonPropertyName("local")] string Local,
	[property: JsonPropertyName("real-remote")] string RealRemote,
	[property: JsonPropertyName("zone-id")] int ZoneId
) : MethodParameters;
