using System;
using System.Collections.Generic;
using System.Configuration;
using NHibernate.Cache;
using StackExchange.Redis;
using static NHibernate.Caches.StackExchangeRedis.ConfigurationHelper;

namespace NHibernate.Caches.StackExchangeRedis
{
	/// <summary>
	/// Cache provider using the <see cref="RedisCache"/> classes.
	/// </summary>
	public class RedisCacheProvider : ICacheProvider
	{
		private static readonly INHibernateLogger Log;
		private static readonly Dictionary<string, RegionConfig> ConfiguredCacheRegions;
		private static readonly CacheConfig ConfiguredCache;
		private static RedisCacheConfiguration _defaultCacheConfiguration = new RedisCacheConfiguration();

		/// <summary>
		/// The default configuration that will be used for creating the <see cref="CacheConfiguration"/>.
		/// </summary>
		public static RedisCacheConfiguration DefaultCacheConfiguration
		{
			get => _defaultCacheConfiguration;
			set => _defaultCacheConfiguration = value ?? new RedisCacheConfiguration();
		}

		/// <summary>
		/// Set the region configuration.
		/// </summary>
		/// <param name="regionName">The region name.</param>
		/// <param name="configuration">The region configuration.</param>
		public static void SetRegionConfiguration(string regionName, RegionConfig configuration)
		{
			ConfiguredCacheRegions[regionName] = configuration;
		}

		static RedisCacheProvider()
		{
			Log = NHibernateLogger.For(typeof(RedisCacheProvider));
			ConfiguredCacheRegions = new Dictionary<string, RegionConfig>();

			if (!(ConfigurationManager.GetSection("redis") is CacheConfig config))
				return;

			ConfiguredCache = config;
			foreach (var cache in config.Regions)
			{
				ConfiguredCacheRegions.Add(cache.Region, cache);
			}
		}

		private static RedisCacheConfiguration CreateCacheConfiguration()
		{
			var defaultConfiguration = DefaultCacheConfiguration;
			var defaultLockConfiguration = defaultConfiguration.LockConfiguration;
			return new RedisCacheConfiguration
			{
				DefaultRegionStrategy = defaultConfiguration.DefaultRegionStrategy,
				DatabaseProvider = defaultConfiguration.DatabaseProvider,
				ConnectionMultiplexerProvider = defaultConfiguration.ConnectionMultiplexerProvider,
				RegionPrefix = defaultConfiguration.RegionPrefix,
				LockConfiguration =
				{
					RetryTimes = defaultLockConfiguration.RetryTimes,
					RetryDelayProvider = defaultLockConfiguration.RetryDelayProvider,
					MaxRetryDelay = defaultLockConfiguration.MaxRetryDelay,
					ValueProvider = defaultLockConfiguration.ValueProvider,
					KeyTimeout = defaultLockConfiguration.KeyTimeout,
					AcquireTimeout = defaultLockConfiguration.AcquireTimeout,
					KeySuffix = defaultLockConfiguration.KeySuffix,
					MinRetryDelay = defaultLockConfiguration.MinRetryDelay
				},
				Serializer = defaultConfiguration.Serializer,
				RegionStrategyFactory = defaultConfiguration.RegionStrategyFactory,
				CacheKeyPrefix = defaultConfiguration.CacheKeyPrefix,
				DefaultUseSlidingExpiration = defaultConfiguration.DefaultUseSlidingExpiration,
				DefaultExpiration = defaultConfiguration.DefaultExpiration,
				DefaultDatabase = defaultConfiguration.DefaultDatabase,
				DefaultAppendHashcode = defaultConfiguration.DefaultAppendHashcode,
				EnvironmentName = defaultConfiguration.EnvironmentName
			};
		}

		/// <summary>
		/// The Redis connection that is shared across caches.
		/// </summary>
		protected virtual IConnectionMultiplexer ConnectionMultiplexer { get; set; }

		/// <summary>
		/// The Redis cache configuration that is populated by the NHibernate configuration.
		/// </summary>
		public RedisCacheConfiguration CacheConfiguration { get; } = CreateCacheConfiguration();

		/// <inheritdoc />
#pragma warning disable 618
		public ICache BuildCache(string regionName, IDictionary<string, string> properties)
#pragma warning restore 618
		{
			if (regionName == null)
			{
				regionName = string.Empty;
			}

			var regionConfiguration = ConfiguredCacheRegions.TryGetValue(regionName, out var regionConfig)
				? BuildRegionConfiguration(regionConfig, properties)
				: BuildRegionConfiguration(regionName, properties);
			Log.Debug("Building cache: {0}", regionConfiguration.ToString());
			return BuildCache(regionConfiguration, properties);
		}

		/// <inheritdoc />
		public long NextTimestamp()
		{
			return Timestamper.Next();
		}

		/// <inheritdoc />
		public void Start(IDictionary<string, string> properties)
		{
			var configurationString = GetString(RedisEnvironment.Configuration, properties, ConfiguredCache?.Configuration);
			if (string.IsNullOrEmpty(configurationString))
			{
				throw new CacheException("The StackExchange.Redis configuration string was not provided.");
			}

			Log.Debug("Starting with configuration string: {0}", configurationString);
			BuildDefaultConfiguration(properties);
			Start(configurationString, properties);
		}

		/// <inheritdoc />
		public virtual void Stop()
		{
			try
			{
				if (Log.IsDebugEnabled())
				{
					Log.Debug("Releasing connection.");
				}

				ConnectionMultiplexer.Dispose();
				ConnectionMultiplexer = null;
			}
			catch (Exception e)
			{
				Log.Error(e, "An error occurred while releasing the connection.");
			}
		}

		/// <summary>
		/// Callback to perform any necessary initialization of the underlying cache implementation
		/// during ISessionFactory construction.
		/// </summary>
		/// <param name="configurationString">The StackExchange.Redis configuration string.</param>
		/// <param name="properties">NHibernate configuration settings.</param>
		protected virtual void Start(string configurationString, IDictionary<string, string> properties)
		{
			ConnectionMultiplexer = CacheConfiguration.ConnectionMultiplexerProvider.Get(configurationString);
		}

		/// <summary>
		/// Builds the cache.
		/// </summary>
		/// <param name="regionConfiguration">The region cache configuration.</param>
		/// <param name="properties">NHibernate configuration settings.</param>
		/// <returns>The built cache.</returns>
		protected virtual CacheBase BuildCache(RedisCacheRegionConfiguration regionConfiguration, IDictionary<string, string> properties)
		{
			var regionStrategy =
				CacheConfiguration.RegionStrategyFactory.Create(ConnectionMultiplexer, regionConfiguration, properties);

			regionStrategy.Validate();

			return new RedisCache(regionConfiguration.RegionName, regionStrategy);
		}
		private RedisCacheRegionConfiguration BuildRegionConfiguration(string regionName, IDictionary<string, string> properties)
		{
			return BuildRegionConfiguration(new RegionConfig(regionName), properties);
		}

		private RedisCacheRegionConfiguration BuildRegionConfiguration(RegionConfig regionConfig, IDictionary<string, string> properties)
		{
			var config = new RedisCacheRegionConfiguration(regionConfig.Region)
			{
				LockConfiguration = CacheConfiguration.LockConfiguration,
				RegionPrefix = CacheConfiguration.RegionPrefix,
				Serializer = CacheConfiguration.Serializer,
				EnvironmentName = CacheConfiguration.EnvironmentName,
				CacheKeyPrefix = CacheConfiguration.CacheKeyPrefix,
				DatabaseProvider = CacheConfiguration.DatabaseProvider
			};

			config.Database = GetInteger("database", properties,
				regionConfig.Database ?? GetInteger(RedisEnvironment.Database, properties,
					CacheConfiguration.DefaultDatabase));
			Log.Debug("Database for region {0}: {1}", regionConfig.Region, config.Database);

			config.Expiration = GetTimeSpanFromSeconds("expiration", properties,
				regionConfig.Expiration ?? GetTimeSpanFromSeconds(Cfg.Environment.CacheDefaultExpiration, properties,
					CacheConfiguration.DefaultExpiration));
			Log.Debug("Expiration for region {0}: {1} seconds", regionConfig.Region, config.Expiration.TotalSeconds);

			config.RegionStrategy = GetSystemType("strategy", properties,
				regionConfig.RegionStrategy ?? GetSystemType(RedisEnvironment.RegionStrategy, properties,
					CacheConfiguration.DefaultRegionStrategy));
			Log.Debug("Region strategy for region {0}: {1}", regionConfig.Region, config.RegionStrategy);

			config.UseSlidingExpiration = GetBoolean("sliding", properties,
				regionConfig.UseSlidingExpiration ?? GetBoolean(RedisEnvironment.UseSlidingExpiration, properties,
					CacheConfiguration.DefaultUseSlidingExpiration));

			config.AppendHashcode = GetBoolean("append-hashcode", properties,
				regionConfig.AppendHashcode ?? GetBoolean(RedisEnvironment.AppendHashcode, properties,
					CacheConfiguration.DefaultAppendHashcode));

			Log.Debug("Use sliding expiration for region {0}: {1}", regionConfig.Region, config.UseSlidingExpiration);

			return config;
		}

		private void BuildDefaultConfiguration(IDictionary<string, string> properties)
		{
			var config = CacheConfiguration;

			config.CacheKeyPrefix = GetString(RedisEnvironment.KeyPrefix, properties, config.CacheKeyPrefix);
			Log.Debug("Cache key prefix: {0}", config.CacheKeyPrefix);

			config.EnvironmentName = GetString(RedisEnvironment.EnvironmentName, properties, config.EnvironmentName);
			Log.Debug("Cache environment name: {0}", config.EnvironmentName);

			Log.Debug("Region prefix: {0}", config.RegionPrefix);

			config.Serializer = GetInstance(RedisEnvironment.Serializer, properties, config.Serializer, Log);
			Log.Debug("Serializer: {0}", config.Serializer);

			config.RegionStrategyFactory = GetInstance(RedisEnvironment.RegionStrategyFactory, properties, config.RegionStrategyFactory, Log);
			Log.Debug("Region strategy factory: {0}", config.RegionStrategyFactory);

			config.ConnectionMultiplexerProvider = GetInstance(RedisEnvironment.ConnectionMultiplexerProvider, properties, config.ConnectionMultiplexerProvider, Log);
			Log.Debug("Connection multiplexer provider: {0}", config.ConnectionMultiplexerProvider);

			config.DatabaseProvider = GetInstance(RedisEnvironment.DatabaseProvider, properties, config.DatabaseProvider, Log);
			Log.Debug("Database provider: {0}", config.DatabaseProvider);

			config.DefaultExpiration = GetTimeSpanFromSeconds(Cfg.Environment.CacheDefaultExpiration, properties, config.DefaultExpiration);
			Log.Debug("Default expiration: {0} seconds", config.DefaultExpiration.TotalSeconds);

			config.DefaultDatabase = GetInteger(RedisEnvironment.Database, properties, config.DefaultDatabase);
			Log.Debug("Default database: {0}", config.DefaultDatabase);

			config.DefaultRegionStrategy = GetSystemType(RedisEnvironment.RegionStrategy, properties, config.DefaultRegionStrategy);
			Log.Debug("Default region strategy: {0}", config.DefaultRegionStrategy);

			config.DefaultUseSlidingExpiration = GetBoolean(RedisEnvironment.UseSlidingExpiration, properties,
				config.DefaultUseSlidingExpiration);
			Log.Debug("Default use sliding expiration: {0}", config.DefaultUseSlidingExpiration);

			config.DefaultAppendHashcode = GetBoolean(RedisEnvironment.AppendHashcode, properties,
				config.DefaultAppendHashcode);
			Log.Debug("Default append hash code: {0}", config.DefaultAppendHashcode);

			var lockConfig = config.LockConfiguration;
			lockConfig.KeyTimeout = GetTimeSpanFromSeconds(RedisEnvironment.LockKeyTimeout, properties, lockConfig.KeyTimeout);
			Log.Debug("Lock key timeout: {0} seconds", lockConfig.KeyTimeout.TotalSeconds);

			lockConfig.AcquireTimeout = GetTimeSpanFromSeconds(RedisEnvironment.LockAcquireTimeout, properties, lockConfig.AcquireTimeout);
			Log.Debug("Lock acquire timeout: {0} seconds", lockConfig.AcquireTimeout.TotalSeconds);

			lockConfig.RetryTimes = GetInteger(RedisEnvironment.LockRetryTimes, properties, lockConfig.RetryTimes);
			Log.Debug("Lock retry times: {0}", lockConfig.RetryTimes);

			lockConfig.MaxRetryDelay = GetTimeSpanFromMilliseconds(RedisEnvironment.LockMaxRetryDelay, properties, lockConfig.MaxRetryDelay);
			Log.Debug("Lock max retry delay: {0} milliseconds", lockConfig.MaxRetryDelay.TotalMilliseconds);

			lockConfig.MinRetryDelay = GetTimeSpanFromMilliseconds(RedisEnvironment.LockMinRetryDelay, properties, lockConfig.MinRetryDelay);
			Log.Debug("Lock min retry delay: {0} milliseconds", lockConfig.MinRetryDelay.TotalMilliseconds);

			lockConfig.ValueProvider = GetInstance(RedisEnvironment.LockValueProvider, properties, lockConfig.ValueProvider, Log);
			Log.Debug("Lock value provider: {0}", lockConfig.ValueProvider);

			lockConfig.RetryDelayProvider = GetInstance(RedisEnvironment.LockRetryDelayProvider, properties, lockConfig.RetryDelayProvider, Log);
			Log.Debug("Lock retry delay provider: {0}", lockConfig.RetryDelayProvider);

			lockConfig.KeySuffix = GetString(RedisEnvironment.LockKeySuffix, properties, lockConfig.KeySuffix);
			Log.Debug("Lock key suffix: {0}", lockConfig.KeySuffix);
		}
	}
}
