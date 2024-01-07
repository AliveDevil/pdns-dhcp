using Microsoft.Extensions.DependencyInjection;

using pdns_dhcp.Options;

namespace pdns_dhcp.Kea;

public static class KeaFactoryServices
{
	public static IServiceCollection AddKeaFactory(this IServiceCollection services)
	{
		services.AddTransient<IKeaFactory, KeaFactory>();
		return services;
	}

	private class KeaFactory(IServiceProvider services) : IKeaFactory
	{
		private ObjectFactory<KeaDhcp4LeaseHandler>? _cachedCreateHandler4;
		private ObjectFactory<KeaDhcp6LeaseHandler>? _cachedCreateHandler6;
		private ObjectFactory<KeaDhcpLeaseWatcher>? _cachedCreateWatcher;

		KeaDhcp4LeaseHandler IKeaFactory.CreateHandler4()
		{
			_cachedCreateHandler4 ??= ActivatorUtilities.CreateFactory<KeaDhcp4LeaseHandler>([]);
			return _cachedCreateHandler4(services, null);
		}

		KeaDhcp6LeaseHandler IKeaFactory.CreateHandler6()
		{
			_cachedCreateHandler6 ??= ActivatorUtilities.CreateFactory<KeaDhcp6LeaseHandler>([]);
			return _cachedCreateHandler6(services, null);
		}

		KeaDhcpLeaseWatcher IKeaFactory.CreateWatcher(IKeaDhcpLeaseHandler handler, KeaDhcpServerOptions options)
		{
			_cachedCreateWatcher ??= ActivatorUtilities.CreateFactory<KeaDhcpLeaseWatcher>([typeof(IKeaDhcpLeaseHandler), typeof(KeaDhcpServerOptions)]);
			return _cachedCreateWatcher(services, [handler, options]);
		}
	}
}
