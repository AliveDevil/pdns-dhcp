using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace pdns_dhcp.PowerDns;

public record class Parameters;

[JsonDerivedType(typeof(InitializeParameters))]
[JsonDerivedType(typeof(LookupParameters))]
public record class MethodParameters : Parameters
{
	[JsonExtensionData]
	public Dictionary<string, JsonElement> AdditionalProperties { get; set; } = [];

	protected override bool PrintMembers(StringBuilder builder)
	{
		if (base.PrintMembers(builder))
		{
			builder.Append(", ");
		}

		builder.Append("AdditionalProperties = [");
		bool append = false;
		foreach (var kv in AdditionalProperties)
		{
			if (append)
			{
				builder.Append(", ");
			}

			append = true;
			builder.Append(kv.Key);
			builder.Append(" = ");
			builder.Append(kv.Value);
		}

		builder.Append(']');
		return true;
	}
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
