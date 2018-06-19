using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NHibernate.Cache;
using NHibernate.Util;
using StackExchange.Redis;
using static NHibernate.Caches.StackExRedis.ConfigurationHelper;

namespace NHibernate.Caches.StackExRedis
{
	/// <summary>
	/// Cache provider using the <see cref="RedisCache"/> classes.
	/// </summary>
	public class RedisCacheProvider : ICacheProvider
	{
		private static readonly INHibernateLogger Log;
		private static readonly Dictionary<string, RegionConfig> ConfiguredCacheRegions;
		private static readonly CacheConfig ConfiguredCache;

		private ConnectionMultiplexer _connectionMultiplexer;
		private RedisCacheConfiguration _defaultCacheConfiguration;

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

		/// <inheritdoc />
		public ICache BuildCache(string regionName, IDictionary<string, string> properties)
		{
			if (regionName == null)
			{
				regionName = string.Empty;
			}

			var regionConfiguration = ConfiguredCacheRegions.TryGetValue(regionName, out var regionConfig) 
				? BuildRegionConfiguration(regionConfig, properties) 
				: BuildRegionConfiguration(regionName, properties);

			Log.Debug("Building cache: {0}", regionConfiguration.ToString());

			return BuildCache(_defaultCacheConfiguration, regionConfiguration, properties);
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

			_defaultCacheConfiguration = BuildDefaultConfiguration(properties);

			Log.Debug("Default configuration: {0}", _defaultCacheConfiguration);

			TextWriter textWriter = Log.IsDebugEnabled() ? new NHibernateTextWriter(Log) : null;
			Start(configurationString, properties, textWriter);
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
				_connectionMultiplexer.Dispose();
				_connectionMultiplexer = null;
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
		/// <param name="configurationString">The Redis configuration string.</param>
		/// <param name="properties">NHibernate configuration settings.</param>
		/// <param name="textWriter">The text writer.</param>
		protected virtual void Start(string configurationString, IDictionary<string, string> properties, TextWriter textWriter)
		{
			var configuration = ConfigurationOptions.Parse(configurationString);
			_connectionMultiplexer = ConnectionMultiplexer.Connect(configuration, textWriter);
			_connectionMultiplexer.PreserveAsyncOrder = false; // Recommended setting
		}

		/// <summary>
		/// Builds the cache.
		/// </summary>
		/// <param name="defaultConfiguration">The default cache configuration.</param>
		/// <param name="regionConfiguration">The region cache configuration.</param>
		/// <param name="properties">NHibernate configuration settings.</param>
		/// <returns>The builded cache.</returns>
		protected virtual ICache BuildCache(RedisCacheConfiguration defaultConfiguration,
			RedisCacheRegionConfiguration regionConfiguration, IDictionary<string, string> properties)
		{
			var regionStrategy =
				defaultConfiguration.RegionStrategyFactory.Create(_connectionMultiplexer, regionConfiguration, properties);

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
				LockConfiguration = _defaultCacheConfiguration.LockConfiguration,
				RegionPrefix = _defaultCacheConfiguration.RegionPrefix,
				Serializer = _defaultCacheConfiguration.Serializer,
				EnvironmentName = _defaultCacheConfiguration.EnvironmentName,
				CacheKeyPrefix = _defaultCacheConfiguration.CacheKeyPrefix
			};

			config.Database = GetInteger("database", properties,
				regionConfig.Database ?? GetInteger(RedisEnvironment.Database, properties,
					_defaultCacheConfiguration.DefaultDatabase));
			Log.Debug("Database for region {0}: {1}", regionConfig.Region, config.Database);

			config.Expiration = GetTimeSpanFromSeconds("expiration", properties,
				regionConfig.Expiration ?? GetTimeSpanFromSeconds(Cfg.Environment.CacheDefaultExpiration, properties,
					_defaultCacheConfiguration.DefaultExpiration));
			Log.Debug("Expiration for region {0}: {1} seconds", regionConfig.Region, config.Expiration.TotalSeconds);

			config.RegionStrategy = GetSystemType("strategy", properties,
				regionConfig.RegionStrategy ?? GetSystemType(RedisEnvironment.RegionStrategy, properties,
					_defaultCacheConfiguration.DefaultRegionStrategy));
			Log.Debug("Region strategy for region {0}: {1}", regionConfig.Region, config.RegionStrategy);

			config.UseSlidingExpiration = GetBoolean("sliding", properties,
				regionConfig.UseSlidingExpiration ?? GetBoolean(RedisEnvironment.UseSlidingExpiration, properties,
					_defaultCacheConfiguration.DefaultUseSlidingExpiration));
			Log.Debug("Use sliding expiration for region {0}: {1}", regionConfig.Region, config.UseSlidingExpiration);

			return config;
		}

		private RedisCacheConfiguration BuildDefaultConfiguration(IDictionary<string, string> properties)
		{
			var config = new RedisCacheConfiguration();

			config.CacheKeyPrefix = GetString(RedisEnvironment.KeyPrefix, properties, config.CacheKeyPrefix);
			Log.Debug("Cache key prefix: {0}", config.CacheKeyPrefix);

			config.EnvironmentName = GetString(RedisEnvironment.EnvironmentName, properties, config.EnvironmentName);
			Log.Debug("Cache environment name: {0}", config.EnvironmentName);

			config.RegionPrefix = GetString(Cfg.Environment.CacheRegionPrefix, properties, config.RegionPrefix);
			Log.Debug("Region prefix: {0}", config.RegionPrefix);

			config.Serializer = GetInstanceOfType(RedisEnvironment.Serializer, properties, config.Serializer);
			Log.Debug("Serializer: {0}", config.Serializer);

			config.RegionStrategyFactory = GetInstanceOfType(RedisEnvironment.RegionStrategyFactory, properties, config.RegionStrategyFactory);
			Log.Debug("Region strategy factory: {0}", config.RegionStrategyFactory);

			config.DefaultExpiration = GetTimeSpanFromSeconds(Cfg.Environment.CacheDefaultExpiration, properties, config.DefaultExpiration);
			Log.Debug("Default expiration: {0} seconds", config.DefaultExpiration.TotalSeconds);

			config.DefaultDatabase = GetInteger(RedisEnvironment.Database, properties, config.DefaultDatabase);
			Log.Debug("Default database: {0}", config.DefaultDatabase);

			config.DefaultRegionStrategy = GetSystemType(RedisEnvironment.RegionStrategy, properties, config.DefaultRegionStrategy);
			Log.Debug("Default region strategy: {0}", config.DefaultRegionStrategy);

			config.DefaultUseSlidingExpiration = GetBoolean(RedisEnvironment.UseSlidingExpiration, properties,
				config.DefaultUseSlidingExpiration);
			Log.Debug("Default use sliding expiration: {0}", config.DefaultUseSlidingExpiration);

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

			lockConfig.ValueProvider = GetInstanceOfType(RedisEnvironment.LockValueProvider, properties, lockConfig.ValueProvider);
			Log.Debug("Lock value provider: {0}", lockConfig.ValueProvider);

			lockConfig.RetryDelayProvider = GetInstanceOfType(RedisEnvironment.LockRetryDelayProvider, properties, lockConfig.RetryDelayProvider);
			Log.Debug("Lock retry delay provider: {0}", lockConfig.RetryDelayProvider);

			lockConfig.KeySuffix = GetString(RedisEnvironment.LockKeySuffix, properties, lockConfig.KeySuffix);
			Log.Debug("Lock key suffix: {0}", lockConfig.KeySuffix);

			return config;
		}
	}
}
