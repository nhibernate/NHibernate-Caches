using System;
using System.Collections.Generic;
using NHibernate.Cache;
using NHibernate.Caches.Common;
using NHibernate.Caches.Common.Tests;
using NSubstitute;
using NUnit.Framework;

namespace NHibernate.Caches.StackExRedis.Tests
{
	[TestFixture]
	public class RedisCacheProviderFixture : CacheProviderFixture
	{
		protected override Func<ICacheProvider> ProviderBuilder =>
			() => new RedisCacheProvider();

		[Test]
		public void TestBuildCacheFromConfig()
		{
			var cache = DefaultProvider.BuildCache("foo", null);
			Assert.That(cache, Is.Not.Null, "pre-configured cache not found");
		}

		[Test]
		public void TestExpiration()
		{
			var cache = DefaultProvider.BuildCache("foo", null) as RedisCache;
			Assert.That(cache, Is.Not.Null, "pre-configured foo cache not found");

			var strategy = cache.RegionStrategy;
			Assert.That(strategy, Is.Not.Null, "strategy was not set for the pre-configured foo cache");
			Assert.That(strategy, Is.TypeOf<DefaultRegionStrategy>(), "unexpected strategy type for foo region");
			Assert.That(strategy.Expiration, Is.EqualTo(TimeSpan.FromSeconds(500)), "unexpected expiration value for foo region");

			cache = (RedisCache) DefaultProvider.BuildCache("noExplicitExpiration", null);
			Assert.That(cache.RegionStrategy.Expiration, Is.EqualTo(TimeSpan.FromSeconds(300)),
				"unexpected expiration value for noExplicitExpiration region");
			Assert.That(cache.RegionStrategy.UseSlidingExpiration, Is.True, "unexpected sliding value for noExplicitExpiration region");

			cache = (RedisCache) DefaultProvider
				.BuildCache("noExplicitExpiration", new Dictionary<string, string> { { "expiration", "100" } });
			Assert.That(cache.RegionStrategy.Expiration, Is.EqualTo(TimeSpan.FromSeconds(100)),
				"unexpected expiration value for noExplicitExpiration region with default expiration");

			cache = (RedisCache) DefaultProvider
				.BuildCache("noExplicitExpiration", new Dictionary<string, string> { { Cfg.Environment.CacheDefaultExpiration, "50" } });
			Assert.That(cache.RegionStrategy.Expiration, Is.EqualTo(TimeSpan.FromSeconds(50)),
				"unexpected expiration value for noExplicitExpiration region with cache.default_expiration");
		}

		[Test]
		public void TestDefaultCacheConfiguration()
		{
			var connectionProvider = Substitute.For<IConnectionMultiplexerProvider>();
			var databaseProvider = Substitute.For<IDatabaseProvider>();
			var retryDelayProvider = Substitute.For<ICacheLockRetryDelayProvider>();
			var lockValueProvider = Substitute.For<ICacheLockValueProvider>();
			var regionStrategyFactory = Substitute.For<ICacheRegionStrategyFactory>();
			var serializer = Substitute.For<CacheSerializerBase>();

			var defaultConfig = RedisCacheProvider.DefaultCacheConfiguration;
			defaultConfig.ConnectionMultiplexerProvider = connectionProvider;
			defaultConfig.DatabaseProvider = databaseProvider;
			defaultConfig.LockConfiguration.ValueProvider = lockValueProvider;
			defaultConfig.LockConfiguration.RetryDelayProvider = retryDelayProvider;
			defaultConfig.RegionStrategyFactory = regionStrategyFactory;
			defaultConfig.Serializer = serializer;

			var provider = (RedisCacheProvider) GetNewProvider();
			var config = provider.CacheConfiguration;

			Assert.That(config.ConnectionMultiplexerProvider, Is.EqualTo(connectionProvider));
			Assert.That(config.DatabaseProvider, Is.EqualTo(databaseProvider));
			Assert.That(config.LockConfiguration.RetryDelayProvider, Is.EqualTo(retryDelayProvider));
			Assert.That(config.LockConfiguration.ValueProvider, Is.EqualTo(lockValueProvider));
			Assert.That(config.RegionStrategyFactory, Is.EqualTo(regionStrategyFactory));
			Assert.That(config.Serializer, Is.EqualTo(serializer));

			RedisCacheProvider.DefaultCacheConfiguration = new RedisCacheConfiguration();
		}

		[Test]
		public void TestUserProvidedObjectsFactory()
		{
			var originalObjectsFactory = Cfg.Environment.ObjectsFactory;
			try
			{
				var customObjectsFactory = new CustomObjectsFactory();
				Cfg.Environment.ObjectsFactory = customObjectsFactory;

				var connectionProvider = Substitute.For<IConnectionMultiplexerProvider>();
				var databaseProvider = Substitute.For<IDatabaseProvider>();
				var retryDelayProvider = Substitute.For<ICacheLockRetryDelayProvider>();
				var lockValueProvider = Substitute.For<ICacheLockValueProvider>();
				var regionStrategyFactory = Substitute.For<ICacheRegionStrategyFactory>();
				var serializer = Substitute.For<CacheSerializerBase>();

				customObjectsFactory.RegisterSingleton(connectionProvider);
				customObjectsFactory.RegisterSingleton(databaseProvider);
				customObjectsFactory.RegisterSingleton(retryDelayProvider);
				customObjectsFactory.RegisterSingleton(lockValueProvider);
				customObjectsFactory.RegisterSingleton(regionStrategyFactory);
				customObjectsFactory.RegisterSingleton(serializer);

				var provider = (RedisCacheProvider) GetNewProvider();
				var config = provider.CacheConfiguration;

				Assert.That(config.ConnectionMultiplexerProvider, Is.EqualTo(connectionProvider));
				Assert.That(config.DatabaseProvider, Is.EqualTo(databaseProvider));
				Assert.That(config.LockConfiguration.RetryDelayProvider, Is.EqualTo(retryDelayProvider));
				Assert.That(config.LockConfiguration.ValueProvider, Is.EqualTo(lockValueProvider));
				Assert.That(config.RegionStrategyFactory, Is.EqualTo(regionStrategyFactory));
				Assert.That(config.Serializer, Is.EqualTo(serializer));
			}
			finally
			{
				Cfg.Environment.ObjectsFactory = originalObjectsFactory;
			}
		}

	}
}
