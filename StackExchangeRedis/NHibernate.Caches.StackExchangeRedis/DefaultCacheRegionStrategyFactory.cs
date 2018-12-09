using System.Collections.Generic;
using NHibernate.Cache;
using StackExchange.Redis;

namespace NHibernate.Caches.StackExchangeRedis
{
	/// <inheritdoc />
	public class DefaultCacheRegionStrategyFactory : ICacheRegionStrategyFactory
	{
		/// <inheritdoc />
		public virtual AbstractRegionStrategy Create(IConnectionMultiplexer connectionMultiplexer,
			RedisCacheRegionConfiguration configuration, IDictionary<string, string> properties)
		{
			if (configuration.RegionStrategy == typeof(DefaultRegionStrategy))
			{
				return new DefaultRegionStrategy(connectionMultiplexer, configuration, properties);
			}
			if (configuration.RegionStrategy == typeof(FastRegionStrategy))
			{
				return new FastRegionStrategy(connectionMultiplexer, configuration, properties);
			}

			throw new CacheException(
				$"{configuration.RegionStrategy} is not supported by {GetType()}, register " +
				$"a custom {typeof(ICacheRegionStrategyFactory)} or use a supported one.");
		}
	}
}
