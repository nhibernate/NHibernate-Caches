using System;
using System.Collections.Generic;
using System.Threading;
using NHibernate.Cache;
using NUnit.Framework;

namespace NHibernate.Caches.Common.Tests
{
	[TestFixture]
	public abstract partial class CacheFixture : Fixture
	{
		protected virtual bool SupportsSlidingExpiration => false;
		protected virtual bool SupportsLocking => false;
		protected virtual bool SupportsDistinguishingKeysWithSameStringRepresentationAndHashcode => true;
		protected virtual bool SupportsClear => true;

		[Test]
		public void TestPut()
		{
			const string key = "keyTestPut";
			const string value = "valuePut";

			var cache = GetDefaultCache();
			// Due to async version, it may already be there.
			cache.Remove(key);

			Assert.That(cache.Get(key), Is.Null, "cache returned an item we didn't add !?!");

			cache.Put(key, value);
			var item = cache.Get(key);
			Assert.That(item, Is.Not.Null, "Unable to retrieve cached item");
			Assert.That(item, Is.EqualTo(value), "didn't return the item we added");
		}

		[Test]
		public void TestRemove()
		{
			const string key = "keyTestRemove";
			const string value = "valueRemove";

			var cache = GetDefaultCache();

			// add the item
			cache.Put(key, value);

			// make sure it's there
			var item = cache.Get(key);
			Assert.That(item, Is.Not.Null, "item just added is not there");

			// remove it
			cache.Remove(key);

			// make sure it's not there
			item = cache.Get(key);
			Assert.That(item, Is.Null, "item still exists in cache after remove");
		}

		[Test]
		public void TestLockUnlock()
		{
			if (!SupportsLocking)
				Assert.Ignore("Test not supported by provider");

			const string key = "keyTestLock";
			const string value = "valueLock";

			var cache = GetDefaultCache();

			// add the item
			cache.Put(key, value);

			cache.Lock(key);
			Assert.Throws<CacheException>(() => cache.Lock(key));

			Thread.Sleep(cache.Timeout / Timestamper.OneMs);

			for (var i = 0; i < 2; i++)
			{
				cache.Lock(key);
				cache.Unlock(key);
			}
		}

		[Test]
		public void TestConcurrentLockUnlock()
		{
			if (!SupportsLocking)
				Assert.Ignore("Test not supported by provider");

			const string value = "value";
			const string key = "keyToLock";

			var cache = GetDefaultCache();

			cache.Put(key, value);
			Assert.That(cache.Get(key), Is.EqualTo(value), "Unable to retrieved cached object for key");

			// Simulate NHibernate ReadWriteCache behavior with multiple concurrent threads
			// Thread 1
			cache.Lock(key);

			// Thread 2
			try
			{
				Assert.Throws<CacheException>(() => cache.Lock(key), "The key should be locked");
			}
			finally
			{
				cache.Unlock(key);
			}

			// Thread 3
			try
			{
				Assert.Throws<CacheException>(() => cache.Lock(key), "The key should still be locked");
			}
			finally
			{
				cache.Unlock(key);
			}
			
			// Thread 1
			cache.Unlock(key);

			Assert.DoesNotThrow(() => cache.Lock(key), "The key should be unlocked");
			cache.Unlock(key);

			cache.Remove(key);
		}

		[Test]
		public void TestClear()
		{
			if (!SupportsClear)
				Assert.Ignore("Test not supported by provider");

			const string key = "keyTestClear";
			const string value = "valueClear";

			var cache = GetDefaultCache();

			// add the item
			cache.Put(key, value);

			// make sure it's there
			var item = cache.Get(key);
			Assert.That(item, Is.Not.Null, "couldn't find item in cache");

			// clear the cache
			cache.Clear();

			// make sure we don't get an item
			item = cache.Get(key);
			Assert.That(item, Is.Null, "item still exists in cache after clear");
		}

		[Test]
		public virtual void TestEmptyProperties()
		{
			var cache = DefaultProvider.BuildCache("TestEmptyProperties", new Dictionary<string, string>());
			Assert.That(cache, Is.Not.Null);
		}

		[Test]
		public void TestNullKeyPut()
		{
			var cache = GetDefaultCache();
			Assert.Throws<ArgumentNullException>(() => cache.Put(null, null));
		}

		[Test]
		public void TestNullValuePut()
		{
			var cache = GetDefaultCache();
			Assert.Throws<ArgumentNullException>(() => cache.Put("keyTestNullValuePut", null));
		}

		[Test]
		public void TestNullKeyGet()
		{
			var cache = GetDefaultCache();
			cache.Put("keyTestNullKeyGet", "value");
			var item = cache.Get(null);
			Assert.IsNull(item);
		}

		[Test]
		public void TestNullKeyRemove()
		{
			var cache = GetDefaultCache();
			Assert.Throws<ArgumentNullException>(() => cache.Remove(null));
		}

		[Test]
		public void TestRegions()
		{
			const string key = "keyTestRegions";
			var props = GetDefaultProperties();
			var cache1 = DefaultProvider.BuildCache("TestRegions1", props);
			var cache2 = DefaultProvider.BuildCache("TestRegions2", props);
			const string s1 = "test1";
			const string s2 = "test2";
			cache1.Put(key, s1);
			cache2.Put(key, s2);
			var get1 = cache1.Get(key);
			var get2 = cache2.Get(key);
			Assert.That(get1, Is.EqualTo(s1), "Unexpected value in cache1");
			Assert.That(get2, Is.EqualTo(s2), "Unexpected value in cache2");
		}

		[Serializable]
		protected class SomeObject
		{
			public int Id;

			public override int GetHashCode()
			{
				return 1;
			}

			public override string ToString()
			{
				return "TestObject";
			}

			public override bool Equals(object obj)
			{
				if (!(obj is SomeObject other))
				{
					return false;
				}

				return other.Id == Id;
			}
		}

		[Test]
		public void TestNonEqualObjectsWithEqualHashCodeAndToString()
		{
			if (!SupportsDistinguishingKeysWithSameStringRepresentationAndHashcode)
				Assert.Ignore("Test not supported by provider");

			var obj1 = new SomeObject();
			var obj2 = new SomeObject();

			obj1.Id = 1;
			obj2.Id = 2;

			var cache = GetDefaultCache();

			Assert.That(cache.Get(obj2), Is.Null, "Unexectedly found a cache entry for key obj2");
			cache.Put(obj1, obj1);
			Assert.That(cache.Get(obj1), Is.EqualTo(obj1), "Unable to retrieved cached object for key obj1");
			Assert.That(cache.Get(obj2), Is.Null, "Unexectedly found a cache entry for key obj2 after obj1 put");
		}

		[Test]
		public void TestBadRelativeExpiration([ValueSource(nameof(ExpirationSettingNames))] string expirationSetting)
		{
			if (!SupportsDefaultExpiration)
				Assert.Ignore("Provider does not support default expiration settings");

			var props = GetPropertiesForExpiration(expirationSetting, "foobar");
			Assert.That(
				() => DefaultProvider.BuildCache("TestBadRelativeExpiration", props),
				Throws.ArgumentException.Or.TypeOf<FormatException>(),
				expirationSetting);
		}

		[Test]
		public void TestObjectExpiration([ValueSource(nameof(ExpirationSettingNames))] string expirationSetting)
		{
			if (!SupportsDefaultExpiration)
				Assert.Ignore("Provider does not support default expiration settings");

			const int expirySeconds = 3;
			const string key = "keyTestObjectExpiration";
			var obj = new SomeObject { Id = 2 };

			var cache = GetCacheForExpiration("TestObjectExpiration", expirationSetting, expirySeconds);

			Assert.That(cache.Get(key), Is.Null, "Unexpected entry for key");
			cache.Put(key, obj);
			// Wait up to 1 sec before expiration
			Thread.Sleep(TimeSpan.FromSeconds(expirySeconds - 1));
			Assert.That(cache.Get(key), Is.Not.Null, "Missing entry for key");

			// Wait expiration
			Thread.Sleep(TimeSpan.FromSeconds(2));

			// Check it expired
			Assert.That(cache.Get(key), Is.Null, "Unexpected entry for key after expiration");
		}

		[Test]
		public void TestObjectExpirationAfterUpdate([ValueSource(nameof(ExpirationSettingNames))] string expirationSetting)
		{
			if (!SupportsDefaultExpiration)
				Assert.Ignore("Provider does not support default expiration settings");

			const int expirySeconds = 3;
			const string key = "keyTestObjectExpirationAfterUpdate";
			var obj = new SomeObject { Id = 2 };

			var cache = GetCacheForExpiration("TestObjectExpirationAfterUpdate", expirationSetting, expirySeconds);

			Assert.That(cache.Get(key), Is.Null, "Unexpected entry for key");
			cache.Put(key, obj);
			Assert.That(cache.Get(key), Is.Not.Null, "Missing entry for key");

			// This forces an object update
			cache.Put(key, obj);
			Assert.That(cache.Get(key), Is.Not.Null, "Missing entry for key after update");

			// Wait
			Thread.Sleep(TimeSpan.FromSeconds(expirySeconds + 2));

			// Check it expired
			Assert.That(cache.Get(key), Is.Null, "Unexpected entry for key after expiration");
		}

		[Test]
		public void TestSlidingExpiration()
		{
			if (!SupportsSlidingExpiration)
				Assert.Ignore("Provider does not support sliding expiration settings");

			const int expirySeconds = 3;
			const string key = "keyTestSlidingExpiration";
			var obj = new SomeObject { Id = 2 };

			var props = GetPropertiesForExpiration(Cfg.Environment.CacheDefaultExpiration, expirySeconds.ToString());
			props["cache.use_sliding_expiration"] = "true";
			var cache = DefaultProvider.BuildCache("TestObjectExpiration", props);

			cache.Put(key, obj);
			// Wait up to 1 sec before expiration
			Thread.Sleep(TimeSpan.FromSeconds(expirySeconds - 1));
			Assert.That(cache.Get(key), Is.Not.Null, "Missing entry for key");

			// Wait up to 1 sec before expiration again
			Thread.Sleep(TimeSpan.FromSeconds(expirySeconds - 1));
			Assert.That(cache.Get(key), Is.Not.Null, "Missing entry for key after get and wait less than expiration");

			// Wait expiration
			Thread.Sleep(TimeSpan.FromSeconds(expirySeconds + 1));

			// Check it expired
			Assert.That(cache.Get(key), Is.Null, "Unexpected entry for key after expiration");
		}

		// NHCH-43
		[Test]
		public void TestUnicode()
		{
			var keyValues = new Dictionary<string, string>
			{
				{"길동", "valuePut1"},
				{"최고", "valuePut2"},
				{"新闻", "valuePut3"},
				{"地图", "valuePut4"},
				{"ます", "valuePut5"},
				{"プル", "valuePut6"}
			};
			var cache = GetDefaultCache();

			// Troubles may specifically arise with long keys, where a hashing algorithm may be used.
			var longKeyPrefix = new string('_', 1000);
			var longKeyValueSuffix = "Long";
			foreach (var kv in keyValues)
			{
				cache.Put(kv.Key, kv.Value);
				cache.Put(longKeyPrefix + kv.Key, kv.Value + longKeyValueSuffix);
			}

			foreach (var kv in keyValues)
			{
				var item = cache.Get(kv.Key);
				Assert.That(item, Is.EqualTo(kv.Value), $"Didn't return the item we added for key {kv.Key}");
				item = cache.Get(longKeyPrefix + kv.Key);
				Assert.That(item, Is.EqualTo(kv.Value + longKeyValueSuffix), $"Didn't return the item we added for long key {kv.Key}");
			}
		}

		protected static string[] ExpirationSettingNames =>
			new[] { DefaultExpirationSetting, Cfg.Environment.CacheDefaultExpiration };

		protected IDictionary<string, string> GetPropertiesForExpiration(string expirationSetting, string value)
		{
			var props = GetDefaultProperties();
			props.Remove(DefaultExpirationSetting);
			props[expirationSetting] = value;
			return props;
		}

		protected CacheBase GetCacheForExpiration(string cacheRegion, string expirationSetting, int expirySeconds)
		{
			var props = GetPropertiesForExpiration(expirationSetting, expirySeconds.ToString());
			var cache = (CacheBase) DefaultProvider.BuildCache(cacheRegion, props);
			return cache;
		}
	}
}
