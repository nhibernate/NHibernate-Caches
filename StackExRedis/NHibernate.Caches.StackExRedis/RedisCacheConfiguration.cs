using System;
using NHibernate.Caches.Common;

namespace NHibernate.Caches.StackExRedis
{
	/// <summary>
	/// Global cache configuration.
	/// </summary>
	public class RedisCacheConfiguration
	{
		private static readonly CacheSerializerBase DefaultSerializer = new BinaryCacheSerializer();
		private static readonly ICacheRegionStrategyFactory DefaultRegionStrategyFactory = new DefaultCacheRegionStrategyFactory();
		private static readonly IConnectionMultiplexerProvider DefaultConnectionMultiplexerProvider = new DefaultConnectionMultiplexerProvider();
		private static readonly IDatabaseProvider DefaultDatabaseProvider = new DefaultDatabaseProvider();
		private static readonly System.Type DefaultRegionStrategyType = typeof(DefaultRegionStrategy);

		private CacheSerializerBase _serializer;
		private ICacheRegionStrategyFactory _regionStrategyFactory;
		private IConnectionMultiplexerProvider _connectionMultiplexerProvider;
		private IDatabaseProvider _databaseProvider;
		private System.Type _defaultRegionStrategy;

		/// <summary>
		/// The <see cref="CacheSerializerBase"/> instance.
		/// </summary>
		public CacheSerializerBase Serializer
		{
			get => _serializer ?? DefaultSerializer;
			set => _serializer = value;
		}

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
		public ICacheRegionStrategyFactory RegionStrategyFactory
		{
			get => _regionStrategyFactory ?? DefaultRegionStrategyFactory;
			set => _regionStrategyFactory = value;
		}

		/// <summary>
		/// The <see cref="IConnectionMultiplexerProvider"/> instance.
		/// </summary>
		public IConnectionMultiplexerProvider ConnectionMultiplexerProvider
		{
			get => _connectionMultiplexerProvider ?? DefaultConnectionMultiplexerProvider;
			set => _connectionMultiplexerProvider = value;
		}

		/// <summary>
		/// The <see cref="IDatabaseProvider"/> instance.
		/// </summary>
		public IDatabaseProvider DatabaseProvider
		{
			get => _databaseProvider ?? DefaultDatabaseProvider;
			set => _databaseProvider = value;
		}

		/// <summary>
		/// The default <see cref="AbstractRegionStrategy"/> type.
		/// </summary>
		public System.Type DefaultRegionStrategy
		{
			get => _defaultRegionStrategy ?? DefaultRegionStrategyType;
			set => _defaultRegionStrategy = value;
		}

		/// <summary>
		/// The configuration for locking keys.
		/// </summary>
		public RedisCacheLockConfiguration LockConfiguration { get; } = new RedisCacheLockConfiguration();
	}
}
