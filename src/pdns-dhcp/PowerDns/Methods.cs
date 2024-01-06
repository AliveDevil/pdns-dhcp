using System.Text.Json.Serialization;

namespace pdns_dhcp.PowerDns;

public interface IMethod;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "method")]
[JsonDerivedType(typeof(InitializeMethod), "initialize")]
[JsonDerivedType(typeof(LookupMethod), "lookup")]
public class Method;

public abstract class Method<TParam>(TParam parameters) : Method
	where TParam : MethodParameters
{
	[JsonPropertyName("parameters")]
	public TParam Parameters => parameters;
}

public class InitializeMethod(InitializeParameters parameters) : Method<InitializeParameters>(parameters), IMethod;

public class LookupMethod(LookupParameters parameters) : Method<LookupParameters>(parameters), IMethod;
