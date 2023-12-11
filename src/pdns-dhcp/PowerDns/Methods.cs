using System.Text.Json;
using System.Text.Json.Serialization;

namespace pdns_dhcp.PowerDns;

public interface IMethod
{
	public abstract static string Method { get; }
}

public abstract class Method<TSelf> where TSelf : Method<TSelf>, IMethod
{
	[JsonExtensionData]
	public Dictionary<string, JsonElement> ExtensionData { get; } = [];
}

public abstract class Method<TSelf, TParam>(TParam parameters) : Method<TSelf> where TSelf : Method<TSelf, TParam>, IMethod
{
	public TParam Parameters => parameters;
}

public class InitializeMethod : Method<InitializeMethod>, IMethod
{
	public static string Method => "Initialize";
}

public class LookupMethod : Method<LookupMethod>, IMethod
{
	public static string Method => "Lookup";
}
