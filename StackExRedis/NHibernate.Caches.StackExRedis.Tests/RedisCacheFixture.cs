using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Cache;
using NHibernate.Caches.Common.Tests;
using NUnit.Framework;

namespace NHibernate.Caches.StackExRedis.Tests
{
	[TestFixture]
	public abstract partial class RedisCacheFixture : CacheFixture
	{
		protected override bool SupportsSlidingExpiration => true;
		protected override bool SupportsLocking => true;
		protected override bool SupportsDistinguishingKeysWithSameStringRepresentationAndHashcode => false;

		protected override Func<ICacheProvider> ProviderBuilder =>
			() => new RedisCacheProvider();

		[Test]
		public void TestEnvironmentName()
		{
			var props = GetDefaultProperties();

			var developProvider = ProviderBuilder();
			props[RedisEnvironment.EnvironmentName] = "develop";
			developProvider.Start(props);
			var developCache = developProvider.BuildCache(DefaultRegion, props);

			var releaseProvider = ProviderBuilder();
			props[RedisEnvironment.EnvironmentName] = "release";
			releaseProvider.Start(props);
			var releaseCache = releaseProvider.BuildCache(DefaultRegion, props);

			const string key = "testKey";
			const string value = "testValue";

			developCache.Put(key, value);

			Assert.That(releaseCache.Get(key), Is.Null, "release environment should be separate from develop");

			developCache.Remove(key);
			releaseCache.Put(key, value);

			Assert.That(developCache.Get(key), Is.Null, "develop environment should be separate from release");

			releaseCache.Remove(key);

			developProvider.Stop();
			releaseProvider.Stop();
		}

		[Test]
		public void TestPutMany()
		{
			var keys = new object[10];
			var values = new object[10];
			for (var i = 0; i < keys.Length; i++)
			{
				keys[i] = $"keyTestPut{i}";
				values[i] = $"valuePut{i}";
			}

			var cache = (RedisCache) GetDefaultCache();
			// Due to async version, it may already be there.
			cache.RemoveMany(keys);

			Assert.That(cache.GetMany(keys), Is.EquivalentTo(new object[10]), "cache returned items we didn't add !?!");

			cache.PutMany(keys, values);
			var items = cache.GetMany(keys);

			for (var i = 0; i < items.Length; i++)
			{
				var item = items[i];
				Assert.That(item, Is.Not.Null, "unable to retrieve cached item");
				Assert.That(item, Is.EqualTo(values[i]), "didn't return the item we added");
			}
		}

		[Test]
		public void TestRemoveMany()
		{
			var keys = new object[10];
			var values = new object[10];
			for (var i = 0; i < keys.Length; i++)
			{
				keys[i] = $"keyTestRemove{i}";
				values[i] = $"valueRemove{i}";
			}

			var cache = (RedisCache) GetDefaultCache();

			// add the item
			cache.PutMany(keys, values);

			// make sure it's there
			var items = cache.GetMany(keys);
			Assert.That(items, Is.EquivalentTo(values), "items just added are not there");

			// remove it
			cache.RemoveMany(keys);

			// make sure it's not there
			items = cache.GetMany(keys);
			Assert.That(items, Is.EquivalentTo(new object[10]), "items still exists in cache after remove");
		}

		[Test]
		public void TestLockUnlockMany()
		{
			if (!SupportsLocking)
				Assert.Ignore("Test not supported by provider");

			var keys = new object[10];
			var values = new object[10];
			for (var i = 0; i < keys.Length; i++)
			{
				keys[i] = $"keyTestLock{i}";
				values[i] = $"valueLock{i}";
			}

			var cache = (RedisCache)GetDefaultCache();

			// add the item
			cache.PutMany(keys, values);
			cache.LockMany(keys);
			Assert.Throws<CacheException>(() => cache.LockMany(keys), "all items should be locked");

			Thread.Sleep(cache.Timeout / Timestamper.OneMs);

			for (var i = 0; i < 2; i++)
			{
				Assert.DoesNotThrow(() =>
				{
					cache.UnlockMany(keys, cache.LockMany(keys));
				}, "the items should be unlocked");
			}

			// Test partial locks by locking the first 5 keys and afterwards try to lock last 6 keys.
			var lockValue = cache.LockMany(keys.Take(5).ToArray());

			Assert.Throws<CacheException>(() => cache.LockMany(keys.Skip(4).ToArray()), "the fifth key should be locked");

			Assert.DoesNotThrow(() =>
			{
				cache.UnlockMany(keys, cache.LockMany(keys.Skip(5).ToArray()));
			}, "the last 5 keys should not be locked.");

			// Unlock the first 5 keys
			cache.UnlockMany(keys, lockValue);

			Assert.DoesNotThrow(() =>
			{
				lockValue = cache.LockMany(keys);
				cache.UnlockMany(keys, lockValue);
			}, "the first 5 keys should not be locked.");
		}

		[Test]
		public void TestNullKeyPutMany()
		{
			var cache = (RedisCache) GetDefaultCache();
			Assert.Throws<ArgumentNullException>(() => cache.PutMany(null, null));
		}

		[Test]
		public void TestNullValuePutMany()
		{
			var cache = (RedisCache) GetDefaultCache();
			Assert.Throws<ArgumentNullException>(() => cache.PutMany(new object[] { "keyTestNullValuePut" }, null));
		}

		[Test]
		public void TestNullKeyGetMany()
		{
			var cache = (RedisCache) GetDefaultCache();
			cache.Put("keyTestNullKeyGet", "value");
			var items = cache.GetMany(null);
			Assert.IsNull(items);
		}

		[Test]
		public void TestNullKeyRemoveMany()
		{
			var cache = (RedisCache) GetDefaultCache();
			Assert.Throws<ArgumentNullException>(() => cache.RemoveMany(null));
		}
	}
}
