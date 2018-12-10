using NHibernate.Cfg;

namespace NHibernate.Caches.StackExchangeRedis
{
	/// <summary>
	/// An extension of NHibernate <see cref="Environment"/> that provides configuration for the Redis cache.
	/// </summary>
	public static class RedisEnvironment
	{
		/// <summary>
		/// The StackExchange.Redis configuration string.
		/// </summary>
		public const string Configuration = "cache.configuration";

		/// <summary>
		/// The Redis database index.
		/// </summary>
		public const string Database = "cache.database";

		/// <summary>
		/// The name of the environment that will be prepended before each cache key in order to allow having
		/// multiple environments on the same Redis database.
		/// </summary>
		public const string EnvironmentName = "cache.environment_name";

		/// <summary>
		/// The assembly qualified name of the serializer.
		/// </summary>
		public const string Serializer = "cache.serializer";

		/// <summary>
		/// The assembly qualified name of the  region strategy.
		/// </summary>
		public const string RegionStrategy = "cache.region_strategy";

		/// <summary>
		/// The assembly qualified name of the region strategy factory.
		/// </summary>
		public const string RegionStrategyFactory = "cache.region_strategy_factory";

		/// <summary>
		/// The assembly qualified name of the connection multiplexer provider.
		/// </summary>
		public const string ConnectionMultiplexerProvider = "cache.connection_multiplexer_provider";

		/// <summary>
		/// The assembly qualified name of the database provider.
		/// </summary>
		public const string DatabaseProvider = "cache.database_provider";

		/// <summary>
		/// Whether the expiration delay should be sliding.
		/// </summary>
		public const string UseSlidingExpiration = "cache.use_sliding_expiration";

		/// <summary>
		/// Whether the hash code of the key should be added to the cache key.
		/// </summary>
		public const string AppendHashcode = "cache.append_hashcode";

		/// <summary>
		/// The prefix that will be prepended before each cache key in order to avoid having collisions when multiple clients
		/// uses the same Redis database.
		/// </summary>
		public const string KeyPrefix = "cache.key_prefix";

		/// <summary>
		/// The timeout for a lock key to expire in seconds.
		/// </summary>
		public const string LockKeyTimeout = "cache.lock.key_timeout";

		/// <summary>
		/// The time limit to acquire the lock in seconds.
		/// </summary>
		public const string LockAcquireTimeout = "cache.lock.acquire_timeout";

		/// <summary>
		/// The number of retries for acquiring the lock.
		/// </summary>
		public const string LockRetryTimes = "cache.lock.retry_times";

		/// <summary>
		/// The maximum delay before retrying to acquire the lock in milliseconds.
		/// </summary>
		public const string LockMaxRetryDelay = "cache.lock.max_retry_delay";

		/// <summary>
		/// The minimum delay before retrying to acquire the lock in milliseconds.
		/// </summary>
		public const string LockMinRetryDelay = "cache.lock.min_retry_delay";

		/// <summary>
		/// The assembly qualified name of the lock value provider.
		/// </summary>
		public const string LockValueProvider = "cache.lock.value_provider";

		/// <summary>
		/// The assembly qualified name of the lock retry delay provider.
		/// </summary>
		public const string LockRetryDelayProvider = "cache.lock.retry_delay_provider";

		/// <summary>
		/// The suffix for the lock key.
		/// </summary>
		public const string LockKeySuffix = "cache.lock.key_suffix";
	}
}
