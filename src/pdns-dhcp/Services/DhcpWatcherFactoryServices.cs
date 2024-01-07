using Microsoft.Extensions.DependencyInjection;

using pdns_dhcp.Kea;
using pdns_dhcp.Options;

namespace pdns_dhcp.Services;

public static class DhcpWatcherFactoryServices
{
	public static IServiceCollection AddDhcpWatcherFactory(this IServiceCollection services)
	{
		services.AddTransient<IDhcpWatcherFactory, DhcpWatcherFactory>();
		return services;
	}

	private class DhcpWatcherFactory(IServiceProvider services) : IDhcpWatcherFactory
	{
		private ObjectFactory<KeaService>? _cachedKeaService;

		KeaService IDhcpWatcherFactory.KeaService(KeaDhcpOptions options)
		{
			_cachedKeaService ??= ActivatorUtilities.CreateFactory<KeaService>([typeof(KeaDhcpOptions)]);
			return _cachedKeaService(services, [options]);
		}
	}
}
