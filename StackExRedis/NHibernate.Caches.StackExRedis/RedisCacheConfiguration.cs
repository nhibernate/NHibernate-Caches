using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHibernate.Caches.StackExRedis
{
	/// <summary>
	/// Global cache configuration.
	/// </summary>
	public class RedisCacheConfiguration
	{
		/// <summary>
		/// The <see cref="IRedisSerializer"/> instance.
		/// </summary>
		public IRedisSerializer Serializer { get; set; } = new BinaryRedisSerializer();

		/// <summary>
		/// The prefix that will be prepended before each cache key in order to avoid having collisions when multiple clients
		/// uses the same Redis database.
		/// </summary>
		public string CacheKeyPrefix { get; set; } = "NHibernate-Cache:";

		/// <summary>
		/// The name of the environment that will be prepended before each cache key in order to allow having
		/// multiple environments on the same Redis database.
		/// </summary>
		public string EnvironmentName { get; set; }

		/// <summary>
		/// The prefix that will be prepended before the region name when building a cache key.
		/// </summary>
		public string RegionPrefix { get; set; }

		/// <summary>
		/// Should the expiration delay be sliding?
		/// </summary>
		/// <value><see langword="true" /> for resetting a cached item expiration each time it is accessed.</value>
		public bool DefaultUseSlidingExpiration { get; set; }

		/// <summary>
		/// Whether the hash code of the key should be added to the cache key.
		/// </summary>
		public bool DefaultAppendHashcode { get; set; }

		/// <summary>
		/// The default expiration time for the keys to expire.
		/// </summary>
		public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromSeconds(300);

		/// <summary>
		/// The default Redis database index.
		/// </summary>
		public int DefaultDatabase { get; set; } = -1;

		/// <summary>
		/// The <see cref="ICacheRegionStrategyFactory"/> instance.
		/// </summary>
		public ICacheRegionStrategyFactory RegionStrategyFactory { get; set; } = new DefaultCacheRegionStrategyFactory();

		/// <summary>
		/// The <see cref="IConnectionMultiplexerProvider"/> instance.
		/// </summary>
		public IConnectionMultiplexerProvider ConnectionMultiplexerProvider { get; set; } = new DefaultConnectionMultiplexerProvider();

		/// <summary>
		/// The <see cref="IDatabaseProvider"/> instance.
		/// </summary>
		public IDatabaseProvider DatabaseProvider { get; set; } = new DefaultDatabaseProvider();

		/// <summary>
		/// The default <see cref="AbstractRegionStrategy"/> type.
		/// </summary>
		public System.Type DefaultRegionStrategy { get; set; } = typeof(DefaultRegionStrategy);

		/// <summary>
		/// The configuration for locking keys.
		/// </summary>
		public RedisCacheLockConfiguration LockConfiguration { get; } = new RedisCacheLockConfiguration();
	}
}
