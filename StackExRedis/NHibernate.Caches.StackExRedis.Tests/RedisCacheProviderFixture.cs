using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using NHibernate.Bytecode;
using NHibernate.Cache;
using NHibernate.Caches.Common.Tests;
using NSubstitute;
using NSubstitute.Extensions;
using NUnit.Framework;
using StackExchange.Redis;

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
		public void TestUserProvidedObjectsFactory()
		{
			// TODO: update when upgraded to NH 5.2
			var field = typeof(AbstractBytecodeProvider).GetField("objectsFactory",
				BindingFlags.Instance | BindingFlags.NonPublic);

			var customObjectsFactory = new CustomObjectsFactory();
			var regionStrategyFactory = Substitute.For<ICacheRegionStrategyFactory>();
			var serialzer = Substitute.For<IRedisSerializer>();
			var lockValueProvider = Substitute.For<ICacheLockValueProvider>();
			var retryDelayProvider = Substitute.For<ICacheLockRetryDelayProvider>();
			var databaseProvider = Substitute.For<IDatabaseProvider>();
			var connectionProvider = Substitute.For<IConnectionMultiplexerProvider>();

			customObjectsFactory.RegisterSingleton(serialzer);
			customObjectsFactory.RegisterSingleton(lockValueProvider);
			customObjectsFactory.RegisterSingleton(regionStrategyFactory);
			customObjectsFactory.RegisterSingleton(retryDelayProvider);
			customObjectsFactory.RegisterSingleton(databaseProvider);
			customObjectsFactory.RegisterSingleton(connectionProvider);

			field.SetValue(Cfg.Environment.BytecodeProvider, customObjectsFactory);

			var provider = (RedisCacheProvider)GetNewProvider();
			var config = provider.CacheConfiguration;

			Assert.That(regionStrategyFactory, Is.EqualTo(config.RegionStrategyFactory));
			Assert.That(serialzer, Is.EqualTo(config.Serializer));
			Assert.That(lockValueProvider, Is.EqualTo(config.LockConfiguration.ValueProvider));
			Assert.That(retryDelayProvider, Is.EqualTo(config.LockConfiguration.RetryDelayProvider));
			Assert.That(databaseProvider, Is.EqualTo(config.DatabaseProvider));
			Assert.That(connectionProvider, Is.EqualTo(config.ConnectionMultiplexerProvider));

			field.SetValue(Cfg.Environment.BytecodeProvider, new ActivatorObjectsFactory());
		}

	}
}
