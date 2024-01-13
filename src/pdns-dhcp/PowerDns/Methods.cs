namespace pdns_dhcp.PowerDns;

public interface IMethod;

public record MethodBase(string Method);

public abstract record Method<TParam>(string Method, TParam Parameters) : MethodBase(Method)
	where TParam : MethodParameters;

public record InitializeMethod(InitializeParameters Parameters) : Method<InitializeParameters>("initialize", Parameters), IMethod;

public record LookupMethod(LookupParameters Parameters) : Method<LookupParameters>("lookup", Parameters), IMethod;
