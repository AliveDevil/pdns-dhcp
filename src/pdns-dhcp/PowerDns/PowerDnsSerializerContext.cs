using System.Text.Json.Serialization;

namespace pdns_dhcp.PowerDns;

[JsonSerializable(typeof(Reply))]
[JsonSerializable(typeof(MethodParameters))]
[JsonSourceGenerationOptions(
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	NumberHandling = JsonNumberHandling.AllowReadingFromString,
	UseStringEnumConverter = true,
	WriteIndented = false
)]
internal partial class PowerDnsSerializerContext : JsonSerializerContext;
