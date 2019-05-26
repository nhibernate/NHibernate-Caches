using System;
using System.Collections.Generic;
using NHibernate.Caches.StackExchangeRedis.Tests.MemoryCaches;
using StackExchange.Redis;

namespace NHibernate.Caches.StackExchangeRedis.Tests
{
	public class CacheRegionStrategyFactory : DefaultCacheRegionStrategyFactory
	{
		public override AbstractRegionStrategy Create(IConnectionMultiplexer connectionMultiplexer, RedisCacheRegionConfiguration configuration,
			IDictionary<string, string> properties)
		{
			var memoryCache = new RuntimeRegionMemoryCache(configuration, Guid.NewGuid().ToString());
			if (configuration.RegionStrategy == typeof(FastTwoLayerCacheRegionStrategy))
			{
				return new FastTwoLayerCacheRegionStrategy(connectionMultiplexer, configuration, memoryCache, properties);
			}

			if (configuration.RegionStrategy == typeof(TwoLayerCacheRegionStrategy))
			{
				return new TwoLayerCacheRegionStrategy(connectionMultiplexer, configuration, memoryCache, properties);
			}

			if (configuration.RegionStrategy == typeof(DistributedLocalCacheRegionStrategy))
			{
				return new DistributedLocalCacheRegionStrategy(connectionMultiplexer, configuration, memoryCache, properties);
			}

			return base.Create(connectionMultiplexer, configuration, properties);
		}
	}
}
