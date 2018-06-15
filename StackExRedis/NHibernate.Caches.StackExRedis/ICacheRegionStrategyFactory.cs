using System.Collections.Generic;
using StackExchange.Redis;

namespace NHibernate.Caches.StackExRedis
{
	/// <summary>
	/// Defines a factory to create concrete <see cref="AbstractRegionStrategy"/> instances.
	/// </summary>
	public interface ICacheRegionStrategyFactory
	{
		/// <summary>
		/// Creates a concrete <see cref="AbstractRegionStrategy"/> instance.
		/// </summary>
		/// <param name="connectionMultiplexer">The connection to be used.</param>
		/// <param name="configuration">The region configuration.</param>
		/// <param name="properties">The properties from NHibernate configuration.</param>
		/// <returns></returns>
		AbstractRegionStrategy Create(ConnectionMultiplexer connectionMultiplexer,
			RedisCacheRegionConfiguration configuration, IDictionary<string, string> properties);
	}
}
