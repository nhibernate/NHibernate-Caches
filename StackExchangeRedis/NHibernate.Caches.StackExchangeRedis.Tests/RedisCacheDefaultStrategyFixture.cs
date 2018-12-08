using System;
using System.Threading;
using NHibernate.Cache;
using NUnit.Framework;

namespace NHibernate.Caches.StackExchangeRedis.Tests
{
	[TestFixture]
	public partial class RedisCacheDefaultStrategyFixture : RedisCacheFixture
	{
		[Test]
		public void TestNoExpiration()
		{
			var props = GetDefaultProperties();
			props["expiration"] = "0";
			Assert.Throws<CacheException>(() => DefaultProvider.BuildCache(DefaultRegion, props), 
				"default region strategy should not allow to have no expiration");
		}

		[Test]
		public void TestMaxAllowedVersion()
		{
			var cache = (RedisCache) GetDefaultCache();
			var strategy = (DefaultRegionStrategy)cache.RegionStrategy;
			var version = strategy.CurrentVersion;

			var props = GetDefaultProperties();
			props.Add("cache.region_strategy.default.max_allowed_version", version.ToString());
			cache = (RedisCache) DefaultProvider.BuildCache(DefaultRegion, props);
			strategy = (DefaultRegionStrategy) cache.RegionStrategy;

			cache.Clear();

			Assert.That(strategy.CurrentVersion, Is.EqualTo(1L), "the version was not reset to 1");
		}

		[Test]
		public void TestClearWithMultipleClientsAndPubSub()
		{
			const string key = "keyTestClear";
			const string value = "valueClear";

			var cache = (RedisCache)GetDefaultCache();
			var strategy = (DefaultRegionStrategy)cache.RegionStrategy;
			var cache2 = (RedisCache) GetDefaultCache();
			var strategy2 = (DefaultRegionStrategy) cache2.RegionStrategy;

			// add the item
			cache.Put(key, value);

			// make sure it's there
			var item = cache.Get(key);
			Assert.That(item, Is.Not.Null, "couldn't find item in cache");

			item = cache2.Get(key);
			Assert.That(item, Is.Not.Null, "couldn't find item in second cache");

			var version = strategy.CurrentVersion;

			// clear the cache
			cache.Clear();

			Assert.That(strategy.CurrentVersion, Is.EqualTo(version + 1), "the version has not been updated");
			Thread.Sleep(TimeSpan.FromSeconds(2));
			Assert.That(strategy2.CurrentVersion, Is.EqualTo(version + 1), "the version should be updated with the pub/sub api");

			// make sure we don't get an item
			item = cache.Get(key);
			Assert.That(item, Is.Null, "item still exists in cache after clear");

			item = cache2.Get(key);
			Assert.That(item, Is.Null, "item still exists in the second cache after clear");
		}

		[Test]
		public void TestClearWithMultipleClientsAndNoPubSub()
		{
			const string key = "keyTestClear";
			const string value = "valueClear";

			var props = GetDefaultProperties();
			props.Add("cache.region_strategy.default.use_pubsub", "false");

			var cache = (RedisCache) DefaultProvider.BuildCache(DefaultRegion, props);
			var strategy = (DefaultRegionStrategy) cache.RegionStrategy;
			var cache2 = (RedisCache) DefaultProvider.BuildCache(DefaultRegion, props);
			var strategy2 = (DefaultRegionStrategy) cache2.RegionStrategy;

			// add the item
			cache.Put(key, value);

			// make sure it's there
			var item = cache.Get(key);
			Assert.That(item, Is.Not.Null, "couldn't find item in cache");

			item = cache2.Get(key);
			Assert.That(item, Is.Not.Null, "couldn't find item in second cache");

			var version = strategy.CurrentVersion;

			// clear the cache
			cache.Clear();

			Assert.That(strategy.CurrentVersion, Is.EqualTo(version + 1), "the version has not been updated");
			Thread.Sleep(TimeSpan.FromSeconds(2));
			Assert.That(strategy2.CurrentVersion, Is.EqualTo(version), "the version should not be updated");

			// make sure we don't get an item
			item = cache.Get(key);
			Assert.That(item, Is.Null, "item still exists in cache after clear");

			item = cache2.Get(key);
			Assert.That(item, Is.Null, "item still exists in the second cache after clear");
		}
	}
}
