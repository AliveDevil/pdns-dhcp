using System.Text.Json.Serialization;

namespace pdns_dhcp.PowerDns;

public interface IMethod;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "method")]
[JsonDerivedType(typeof(InitializeMethod), "initialize")]
[JsonDerivedType(typeof(LookupMethod), "lookup")]
public class Method;

[JsonSerializable(typeof(Method)), JsonSourceGenerationOptions(
	GenerationMode = JsonSourceGenerationMode.Metadata,
	NumberHandling = JsonNumberHandling.AllowReadingFromString,
	UseStringEnumConverter = true
)]
internal partial class MethodContext : JsonSerializerContext;

public abstract class Method<TParam>(TParam parameters) : Method
	where TParam : MethodParameters
{
	public TParam Parameters => parameters;
}

public class InitializeMethod(InitializeMethodParameters parameters) : Method<InitializeMethodParameters>(parameters), IMethod;

public class LookupMethod(LookupMethodParameters parameters) : Method<LookupMethodParameters>(parameters), IMethod;
