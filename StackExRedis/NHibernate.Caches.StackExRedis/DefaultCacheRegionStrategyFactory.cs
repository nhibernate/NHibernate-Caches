using System;
using System.Collections.Generic;
using StackExchange.Redis;

namespace NHibernate.Caches.StackExRedis
{
	/// <inheritdoc />
	public class DefaultCacheRegionStrategyFactory : ICacheRegionStrategyFactory
	{
		/// <inheritdoc />
		public AbstractRegionStrategy Create(ConnectionMultiplexer connectionMultiplexer,
			RedisCacheRegionConfiguration configuration, IDictionary<string, string> properties)
		{
			return (AbstractRegionStrategy) Activator.CreateInstance(configuration.RegionStrategy, connectionMultiplexer,
				configuration, properties);
		}
	}
}
