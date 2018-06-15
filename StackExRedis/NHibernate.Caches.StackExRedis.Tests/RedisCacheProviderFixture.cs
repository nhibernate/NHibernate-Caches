using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Cache;
using NHibernate.Caches.Common.Tests;
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
			Assert.That(strategy, Is.TypeOf<DefaultRegionStrategy>(), "Unexpected strategy type for foo region");
			Assert.That(strategy.Expiration, Is.EqualTo(TimeSpan.FromSeconds(500)), "Unexpected expiration value for foo region");

			cache = (RedisCache) DefaultProvider.BuildCache("noExplicitExpiration", null);
			Assert.That(cache.RegionStrategy.Expiration, Is.EqualTo(TimeSpan.FromSeconds(300)),
				"Unexpected expiration value for noExplicitExpiration region");
			Assert.That(cache.RegionStrategy.UseSlidingExpiration, Is.True, "Unexpected sliding value for noExplicitExpiration region");

			cache = (RedisCache) DefaultProvider
				.BuildCache("noExplicitExpiration", new Dictionary<string, string> { { "expiration", "100" } });
			Assert.That(cache.RegionStrategy.Expiration, Is.EqualTo(TimeSpan.FromSeconds(100)),
				"Unexpected expiration value for noExplicitExpiration region with default expiration");

			cache = (RedisCache) DefaultProvider
				.BuildCache("noExplicitExpiration", new Dictionary<string, string> { { Cfg.Environment.CacheDefaultExpiration, "50" } });
			Assert.That(cache.RegionStrategy.Expiration, Is.EqualTo(TimeSpan.FromSeconds(50)),
				"Unexpected expiration value for noExplicitExpiration region with cache.default_expiration");
		}

	}
}
