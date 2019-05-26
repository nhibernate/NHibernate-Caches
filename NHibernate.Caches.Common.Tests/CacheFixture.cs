using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
		protected virtual bool SupportsDistributedCache => false;
		protected virtual TimeSpan DistributedSynhronizationTime => TimeSpan.Zero;
		protected virtual int ReadStressTestTaskCount => Environment.ProcessorCount;
		protected virtual int WriteStressTestTaskCount => Environment.ProcessorCount;

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
		public void TestDistributedPut()
		{
			if (!SupportsDistributedCache)
			{
				Assert.Ignore("Test not supported by provider");
			}

			const string key = "keyTestPut";
			const string value = "valuePut";

			var cache = GetDefaultCache();
			var cache2 = (CacheBase) GetNewProvider().BuildCache(DefaultRegion, GetDefaultProperties());
			var cache3 = (CacheBase) GetNewProvider().BuildCache(DefaultRegion, GetDefaultProperties());
			// Due to async version, it may already be there.
			cache.Remove(key);
			Thread.Sleep(DistributedSynhronizationTime);
			Assert.That(cache.Get(key), Is.Null, "cache returned an item we didn't add !?!");
			Assert.That(cache2.Get(key), Is.Null, "cache returned an item we didn't add !?!");
			Assert.That(cache3.Get(key), Is.Null, "cache returned an item we didn't add !?!");

			cache.Put(key, value);
			Thread.Sleep(DistributedSynhronizationTime);
			AssertItem(cache.Get(key));
			AssertItem(cache2.Get(key));
			AssertItem(cache3.Get(key));

			void AssertItem(object item)
			{
				Assert.That(item, Is.Not.Null, "Unable to retrieve cached item");
				Assert.That(item, Is.EqualTo(value), "didn't return the item we added");
			}
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

		[Test, Repeat(2)]
		public void TestDistributedRemove()
		{
			if (!SupportsDistributedCache)
			{
				Assert.Ignore("Test not supported by provider");
			}

			const string key = "keyTestRemove";
			const string value = "valueRemove";

			var cache = GetDefaultCache();
			var cache2 = (CacheBase) GetNewProvider().BuildCache(DefaultRegion, GetDefaultProperties());
			var cache3 = (CacheBase) GetNewProvider().BuildCache(DefaultRegion, GetDefaultProperties());

			cache.Remove(key);
			Thread.Sleep(DistributedSynhronizationTime);
			Assert.That(cache.Get(key), Is.Null, "item still exists in cache after remove");
			Assert.That(cache2.Get(key), Is.Null, "item still exists in cache after remove");
			Assert.That(cache3.Get(key), Is.Null, "item still exists in cache after remove");

			cache.Put(key, value);
			Thread.Sleep(DistributedSynhronizationTime);
			Assert.That(cache.Get(key), Is.Not.Null, "item just added is not there");
			Assert.That(cache2.Get(key), Is.Not.Null, "item just added is not there");
			Assert.That(cache3.Get(key), Is.Not.Null, "item just added is not there");

			cache.Remove(key);
			Thread.Sleep(DistributedSynhronizationTime);
			Assert.That(cache.Get(key), Is.Null, "item still exists in cache after remove");
			Assert.That(cache2.Get(key), Is.Null, "item still exists in cache after remove");
			Assert.That(cache3.Get(key), Is.Null, "item still exists in cache after remove");

			cache.Put(key, value);
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
				var lockValue = cache.Lock(key);
				cache.Unlock(key, lockValue);
			}
		}

		[Test]
		public void TestDistributedLockUnlock()
		{
			if (!SupportsLocking || !SupportsDistributedCache)
			{
				Assert.Ignore("Test not supported by provider");
			}

			const string key = "keyTestLock";
			const string value = "valueLock";

			var cache = GetDefaultCache();
			var cache2 = (CacheBase) GetNewProvider().BuildCache(DefaultRegion, GetDefaultProperties());
			var cache3 = (CacheBase) GetNewProvider().BuildCache(DefaultRegion, GetDefaultProperties());

			// add the item
			cache.Put(key, value);
			Thread.Sleep(DistributedSynhronizationTime);

			var lockValue = cache.Lock(key);
			Assert.Throws<CacheException>(() => cache.Lock(key), "The key should be locked");
			Assert.Throws<CacheException>(() => cache2.Lock(key), "The key should be locked");
			Assert.Throws<CacheException>(() => cache3.Lock(key), "The key should be locked");
			cache.Unlock(key, lockValue);

			lockValue = cache2.Lock(key);
			Assert.Throws<CacheException>(() => cache.Lock(key), "The key should be locked");
			Assert.Throws<CacheException>(() => cache2.Lock(key), "The key should be locked");
			Assert.Throws<CacheException>(() => cache3.Lock(key), "The key should be locked");
			cache2.Unlock(key, lockValue);
			Thread.Sleep(DistributedSynhronizationTime);

			for (var i = 0; i < 2; i++)
			{
				lockValue = cache.Lock(key);
				cache.Unlock(key, lockValue);

				lockValue = cache2.Lock(key);
				cache2.Unlock(key, lockValue);

				lockValue = cache3.Lock(key);
				cache3.Unlock(key, lockValue);
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
			var lockValue = cache.Lock(key);
			// Thread 2
			Assert.Throws<CacheException>(() => cache.Lock(key), "The key should be locked");
			// Thread 3
			Assert.Throws<CacheException>(() => cache.Lock(key), "The key should still be locked");

			// Thread 1
			cache.Unlock(key, lockValue);

			Assert.DoesNotThrow(() => lockValue = cache.Lock(key), "The key should be unlocked");
			cache.Unlock(key, lockValue);

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
		public void TestDistributedClear()
		{
			if (!SupportsClear || !SupportsDistributedCache)
			{
				Assert.Ignore("Test not supported by provider");
			}

			const string key = "keyTestClear";
			const string value = "valueClear";

			var cache = GetDefaultCache();
			var cache2 = (CacheBase) GetNewProvider().BuildCache(DefaultRegion, GetDefaultProperties());
			var cache3 = (CacheBase) GetNewProvider().BuildCache(DefaultRegion, GetDefaultProperties());

			// add the item
			cache.Put(key, value);
			Thread.Sleep(DistributedSynhronizationTime);

			// make sure it's there
			Assert.That(cache.Get(key), Is.Not.Null, "couldn't find item in cache");
			Assert.That(cache2.Get(key), Is.Not.Null, "couldn't find item in cache");
			Assert.That(cache3.Get(key), Is.Not.Null, "couldn't find item in cache");

			// clear the cache
			cache.Clear();
			Thread.Sleep(DistributedSynhronizationTime);

			// make sure we don't get an item
			Assert.That(cache.Get(key), Is.Null, "item still exists in cache after clear");
			Assert.That(cache2.Get(key), Is.Null, "item still exists in cache after clear");
			Assert.That(cache3.Get(key), Is.Null, "item still exists in cache after clear");
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

			var cache = GetDefaultCache();
			// Due to async version, it may already be there.
			foreach (var key in keys)
				cache.Remove(key);

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
		public void TestDistributedPutMany()
		{
			if (!SupportsDistributedCache)
			{
				Assert.Ignore("Test not supported by provider");
			}

			var keys = new object[10];
			var values = new object[10];
			for (var i = 0; i < keys.Length; i++)
			{
				keys[i] = $"keyTestPut{i}";
				values[i] = $"valuePut{i}";
			}

			var cache = GetDefaultCache();
			var cache2 = (CacheBase) GetNewProvider().BuildCache(DefaultRegion, GetDefaultProperties());
			var cache3 = (CacheBase) GetNewProvider().BuildCache(DefaultRegion, GetDefaultProperties());
			// Due to async version, it may already be there.
			foreach (var key in keys)
				cache.Remove(key);

			Thread.Sleep(DistributedSynhronizationTime);
			Assert.That(cache.GetMany(keys), Is.EquivalentTo(new object[10]), "cache returned items we didn't add !?!");
			Assert.That(cache2.GetMany(keys), Is.EquivalentTo(new object[10]), "cache returned items we didn't add !?!");
			Assert.That(cache3.GetMany(keys), Is.EquivalentTo(new object[10]), "cache returned items we didn't add !?!");

			cache.PutMany(keys, values);
			Thread.Sleep(DistributedSynhronizationTime);

			AssertNotEmpty(cache.GetMany(keys));
			AssertNotEmpty(cache2.GetMany(keys));
			AssertNotEmpty(cache3.GetMany(keys));

			void AssertNotEmpty(object[] items)
			{
				for (var i = 0; i < items.Length; i++)
				{
					var item = items[i];
					Assert.That(item, Is.Not.Null, "unable to retrieve cached item");
					Assert.That(item, Is.EqualTo(values[i]), "didn't return the item we added");
				}
			}
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

			var cache = GetDefaultCache();

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
		public void TestDistributedLockUnlockMany()
		{
			if (!SupportsDistributedCache)
			{
				Assert.Ignore("Test not supported by provider");
			}

			var keys = new object[10];
			var values = new object[10];
			for (var i = 0; i < keys.Length; i++)
			{
				keys[i] = $"keyTestLock{i}";
				values[i] = $"valueLock{i}";
			}

			var cache = GetDefaultCache();
			var cache2 = (CacheBase) GetNewProvider().BuildCache(DefaultRegion, GetDefaultProperties());
			var cache3 = (CacheBase) GetNewProvider().BuildCache(DefaultRegion, GetDefaultProperties());

			// add the item
			cache.PutMany(keys, values);
			cache.LockMany(keys);

			Assert.Throws<CacheException>(() => cache.LockMany(keys), "all items should be locked");
			Assert.Throws<CacheException>(() => cache2.LockMany(keys), "all items should be locked");
			Assert.Throws<CacheException>(() => cache3.LockMany(keys), "all items should be locked");

			Thread.Sleep(cache.Timeout / Timestamper.OneMs);

			for (var i = 0; i < 2; i++)
			{
				Assert.DoesNotThrow(() =>
				{
					cache.UnlockMany(keys, cache.LockMany(keys));
					cache2.UnlockMany(keys, cache2.LockMany(keys));
					cache3.UnlockMany(keys, cache3.LockMany(keys));
				}, "the items should be unlocked");
			}

			// Test partial locks by locking the first 5 keys and afterwards try to lock last 6 keys.
			var lockValue = cache.LockMany(keys.Take(5).ToArray());

			Assert.Throws<CacheException>(() => cache.LockMany(keys.Skip(4).ToArray()), "the fifth key should be locked");
			Assert.Throws<CacheException>(() => cache2.LockMany(keys.Skip(4).ToArray()), "the fifth key should be locked");
			Assert.Throws<CacheException>(() => cache3.LockMany(keys.Skip(4).ToArray()), "the fifth key should be locked");

			Assert.DoesNotThrow(() =>
			{
				cache.UnlockMany(keys, cache.LockMany(keys.Skip(5).ToArray()));
				cache2.UnlockMany(keys, cache2.LockMany(keys.Skip(5).ToArray()));
				cache3.UnlockMany(keys, cache3.LockMany(keys.Skip(5).ToArray()));
			}, "the last 5 keys should not be locked.");

			// Unlock the first 5 keys
			cache.UnlockMany(keys, lockValue);

			Assert.DoesNotThrow(() =>
			{
				cache.UnlockMany(keys, cache.LockMany(keys));
				cache2.UnlockMany(keys, cache2.LockMany(keys));
				cache3.UnlockMany(keys, cache3.LockMany(keys));
			}, "the first 5 keys should not be locked.");
		}

		[Test]
		public void TestNullKeyPutMany()
		{
			var cache = GetDefaultCache();
			Assert.Throws<ArgumentNullException>(() => cache.PutMany(null, null));
		}

		[Test]
		public void TestNullValuePutMany()
		{
			var cache = GetDefaultCache();
			Assert.Throws<ArgumentNullException>(() => cache.PutMany(new object[] { "keyTestNullValuePut" }, null));
		}

		[Test]
		public void TestNullKeyGetMany()
		{
			var cache = GetDefaultCache();
			cache.Put("keyTestNullKeyGet", "value");
			Assert.Throws<ArgumentNullException>(() => cache.GetMany(null));
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

		[TestCase(1000), Repeat(10), Explicit]
		public virtual Task StressTestGetAsync(int totalKeys)
		{
			return StressTestActionAsync(totalKeys, Get, GetAsync, configureCachesAction: ConfigureCaches, readOperations: true);

			Task ConfigureCaches(CacheBase[] caches, object[] keys)
			{
				var values = new object[keys.Length];
				for (var i = 0; i < values.Length; i++)
				{
					values[i] = Guid.NewGuid().ToString();
				}

				caches[0].PutMany(keys, values);
				return Task.Delay(TimeSpan.FromSeconds(1));
			}

			void Get(object[] keys, Random random, CacheBase cache)
			{
				cache.Get(keys[random.Next(totalKeys)]);
			}

			Task GetAsync(object[] keys, Random random, CacheBase cache)
			{
				return cache.GetAsync(keys[random.Next(totalKeys)], CancellationToken.None);
			}
		}

		[TestCase(1000, 10), Repeat(10), Explicit]
		public virtual Task StressTestGetManyAsync(int totalKeys, int batchSize)
		{
			return StressTestActionAsync(totalKeys, GetMany, GetManyAsync, configureCachesAction: ConfigureCaches, readOperations: true);

			Task ConfigureCaches(CacheBase[] caches, object[] keys)
			{
				var values = new object[keys.Length];
				for (var i = 0; i < values.Length; i++)
				{
					values[i] = Guid.NewGuid().ToString();
				}

				caches[0].PutMany(keys, values);
				return Task.Delay(TimeSpan.FromSeconds(1));
			}

			void GetMany(object[] allKeys, Random random, CacheBase cache)
			{
				var keys = new object[batchSize];
				for (var i = 0; i < keys.Length; i++)
				{
					keys[i] = allKeys[random.Next(totalKeys)];
				}

				cache.GetMany(keys);
			}

			Task GetManyAsync(object[] allKeys, Random random, CacheBase cache)
			{
				var keys = new object[batchSize];
				for (var i = 0; i < keys.Length; i++)
				{
					keys[i] = allKeys[random.Next(totalKeys)];
				}

				return cache.GetAsync(keys, CancellationToken.None);
			}
		}

		[TestCase(1000), Repeat(10), Explicit]
		public virtual Task StressTestPutAsync(int totalKeys)
		{
			return StressTestActionAsync(totalKeys, Put, PutAsync);

			void Put(object[] keys, Random random, CacheBase cache)
			{
				cache.Put(keys[random.Next(totalKeys)], Guid.NewGuid().ToString());
			}

			Task PutAsync(object[] keys, Random random, CacheBase cache)
			{
				return cache.PutAsync(keys[random.Next(totalKeys)], Guid.NewGuid().ToString(), CancellationToken.None);
			}
		}

		[TestCase(1000, 10), Repeat(10), Explicit]
		public virtual Task StressTestPutManyAsync(int totalKeys, int batchSize)
		{
			return StressTestActionAsync(totalKeys, PutMany, PutManyAsync);

			void PutMany(object[] allKeys, Random random, CacheBase cache)
			{
				var values = new object[batchSize];
				var keys = new object[batchSize];
				for (var i = 0; i < values.Length; i++)
				{
					keys[i] = allKeys[random.Next(totalKeys)];
					values[i] = Guid.NewGuid().ToString();
				}

				cache.PutMany(keys, values);
			}

			Task PutManyAsync(object[] allKeys, Random random, CacheBase cache)
			{
				var values = new object[batchSize];
				var keys = new object[batchSize];
				for (var i = 0; i < values.Length; i++)
				{
					keys[i] = allKeys[random.Next(totalKeys)];
					values[i] = Guid.NewGuid().ToString();
				}

				return cache.PutManyAsync(keys, values, CancellationToken.None);
			}
		}

		[TestCase(1000), Repeat(10), Explicit]
		public virtual Task StressTestLockUnlockAsync(int totalKeys)
		{
			var index = -1;
			return StressTestActionAsync(totalKeys, Lock, LockAsync);

			void Lock(object[] keys, Random random, CacheBase cache)
			{
				var keyIndex = Interlocked.Increment(ref index) % totalKeys;
				var key = keys[keyIndex];
				cache.Unlock(key, cache.Lock(key));
			}

			async Task LockAsync(object[] keys, Random random, CacheBase cache)
			{
				var keyIndex = Interlocked.Increment(ref index) % totalKeys;
				var key = keys[keyIndex];
				await cache.UnlockAsync(
					key,
					await cache.LockAsync(key, CancellationToken.None).ConfigureAwait(false),
					CancellationToken.None);
			}
		}

		[TestCase(1000, 10), Repeat(10), Explicit]
		public virtual Task StressTestLockUnlockManyAsync(int totalKeys, int batchSize)
		{
			var index = -1;
			return StressTestActionAsync(totalKeys, Lock, LockAsync);

			void Lock(object[] allKeys, Random random, CacheBase cache)
			{
				var keys = new object[batchSize];
				for (var i = 0; i < keys.Length; i++)
				{
					var keyIndex = Interlocked.Increment(ref index) % totalKeys;
					keys[i] = allKeys[keyIndex];
				}

				cache.UnlockMany(keys, cache.LockMany(keys));
			}

			async Task LockAsync(object[] allKeys, Random random, CacheBase cache)
			{
				var keys = new object[batchSize];
				for (var i = 0; i < keys.Length; i++)
				{
					var keyIndex = Interlocked.Increment(ref index) % totalKeys;
					keys[i] = allKeys[keyIndex];
				}

				await cache.UnlockManyAsync(
					keys,
					await cache.LockManyAsync(keys, CancellationToken.None).ConfigureAwait(false),
					CancellationToken.None);
			}
		}

		[TestCase(1000, 20, 80), Repeat(10), Explicit]
		public virtual Task StressTestPutAndGetAsync(int totalKeys, int putWeight, int getWeight)
		{
			var index = -1;
			var weightRandomizer = new WeightRandommizer(putWeight, getWeight);
			return StressTestActionAsync(totalKeys, PutOrGet, PutOrGetAsync);

			void PutOrGet(object[] keys, Random random, CacheBase cache)
			{
				if (weightRandomizer.GetWeight(random) == putWeight)
				{
					var keyIndex = Interlocked.Increment(ref index);
					cache.Put(keys[keyIndex < keys.Length ? keyIndex : random.Next(totalKeys)], Guid.NewGuid().ToString());
				}
				else
				{
					cache.Get(keys[random.Next(totalKeys)]);
				}
			}

			Task PutOrGetAsync(object[] keys, Random random, CacheBase cache)
			{
				if (weightRandomizer.GetWeight(random) == putWeight)
				{
					var keyIndex = Interlocked.Increment(ref index);
					return cache.PutAsync(
						keys[keyIndex < keys.Length ? keyIndex : random.Next(totalKeys)],
						Guid.NewGuid().ToString(),
						CancellationToken.None);
				}

				return cache.GetAsync(keys[random.Next(totalKeys)], CancellationToken.None);
			}
		}

		[TestCase(1000, 80, 20), Repeat(10), Explicit]
		public virtual Task StressTestPutAndRemoveAsync(int totalKeys, int putWeight, int removeWeight)
		{
			var weightRandomizer = new WeightRandommizer(putWeight, removeWeight);

			return StressTestActionAsync(totalKeys, PutOrRemove, PutOrRemoveAsync);

			void PutOrRemove(object[] keys, Random random, CacheBase cache)
			{
				if (weightRandomizer.GetWeight(random) == putWeight)
				{
					cache.Put(keys[random.Next(totalKeys)], Guid.NewGuid().ToString());
				}
				else
				{
					cache.Remove(keys[random.Next(totalKeys)]);
				}
			}

			Task PutOrRemoveAsync(object[] keys, Random random, CacheBase cache)
			{
				return weightRandomizer.GetWeight(random) == putWeight
					? cache.PutAsync(keys[random.Next(totalKeys)], Guid.NewGuid().ToString(), CancellationToken.None)
					: cache.RemoveAsync(keys[random.Next(totalKeys)], CancellationToken.None);
			}
		}

		[TestCase(1000, 99, 1), Repeat(10), Explicit]
		public virtual Task StressTestPutAndClearAsync(int totalKeys, int putWeight, int clearWeight)
		{
			if (!SupportsClear)
			{
				Assert.Ignore("Provider does not support clear operation");
			}

			var weightRandomizer = new WeightRandommizer(putWeight, clearWeight);

			return StressTestActionAsync(totalKeys, PutOrClear, PutOrClearAsync, allowEmptyValues: SupportsDistributedCache);

			void PutOrClear(object[] keys, Random random, CacheBase cache)
			{
				if (weightRandomizer.GetWeight(random) == putWeight)
				{
					cache.Put(keys[random.Next(totalKeys)], Guid.NewGuid().ToString());
				}
				else
				{
					cache.Clear();
				}
			}

			Task PutOrClearAsync(object[] keys, Random random, CacheBase cache)
			{
				return weightRandomizer.GetWeight(random) == putWeight
					? cache.PutAsync(keys[random.Next(totalKeys)], Guid.NewGuid().ToString(), CancellationToken.None)
					: cache.ClearAsync(CancellationToken.None);
			}
		}

		[TestCase(1000, 19, 80, 1), Repeat(10), Explicit]
		public virtual Task StressTestPutGetAndClearAsync(int totalKeys, int putWeight,  int getWeight, int clearWeight)
		{
			if (!SupportsClear)
			{
				Assert.Ignore("Provider does not support clear operation");
			}

			var weightRandomizer = new WeightRandommizer(getWeight, putWeight, clearWeight);

			return StressTestActionAsync(totalKeys, PutGetOrClear, PutGetOrClearAsync, allowEmptyValues: SupportsDistributedCache);

			void PutGetOrClear(object[] keys, Random random, CacheBase cache)
			{
				var weight = weightRandomizer.GetWeight(random);
				if (weight == getWeight)
				{
					cache.Get(keys[random.Next(totalKeys)]);
				}
				else if (weight == putWeight)
				{
					cache.Put(keys[random.Next(totalKeys)], Guid.NewGuid().ToString());
				}
				else
				{
					cache.Clear();
				}
			}

			Task PutGetOrClearAsync(object[] keys, Random random, CacheBase cache)
			{
				var weight = weightRandomizer.GetWeight(random);
				if (weight == getWeight)
				{
					return cache.GetAsync(keys[random.Next(totalKeys)], CancellationToken.None);
				}

				if (weight == putWeight)
				{
					return cache.PutAsync(keys[random.Next(totalKeys)], Guid.NewGuid().ToString(), CancellationToken.None);
				}

				return cache.ClearAsync(CancellationToken.None);
			}
		}

		[TestCase(1000, 20, 75, 5), Repeat(10), Explicit]
		public virtual Task StressTestPutGetAndRemoveAsync(int totalKeys, int putWeight, int getWeight, int removeWeight)
		{
			var weightRandomizer = new WeightRandommizer(getWeight, putWeight, removeWeight);

			return StressTestActionAsync(totalKeys, PutGetOrRemove, PutGetOrRemoveAsync, allowEmptyValues: SupportsDistributedCache);

			void PutGetOrRemove(object[] keys, Random random, CacheBase cache)
			{
				var weight = weightRandomizer.GetWeight(random);
				if (weight == getWeight)
				{
					cache.Get(keys[random.Next(totalKeys)]);
				}
				else if (weight == putWeight)
				{
					cache.Put(keys[random.Next(totalKeys)], Guid.NewGuid().ToString());
				}
				else
				{
					cache.Remove(keys[random.Next(totalKeys)]);
				}
			}

			Task PutGetOrRemoveAsync(object[] keys, Random random, CacheBase cache)
			{
				var value = weightRandomizer.GetWeight(random);
				if (value == getWeight)
				{
					return cache.GetAsync(keys[random.Next(totalKeys)], CancellationToken.None);
				}

				if (value == putWeight)
				{
					return cache.PutAsync(keys[random.Next(totalKeys)], Guid.NewGuid().ToString(), CancellationToken.None);
				}

				return cache.RemoveAsync(keys[random.Next(totalKeys)], CancellationToken.None);
			}
		}

		protected async Task StressTestActionAsync(
			int totalKeys,
			Action<object[], Random, CacheBase> action,
			Func<object[], Random, CacheBase, Task> asyncAction,
			int executionTimeInSeconds = 5,
			bool allowEmptyValues = false,
			bool readOperations = false,
			Func<CacheBase[], object[], Task> configureCachesAction = null
		)
		{
			var masterRandom = new Random();
			var tasks = new Task[Math.Max(2, readOperations ? ReadStressTestTaskCount : WriteStressTestTaskCount)];
			var keys = Enumerable.Range(1, totalKeys).Select(o => (object) $"stressKey{o}").ToArray();
			var properties = GetDefaultProperties();
			var totalSyncOperations = 0;
			var totalAsyncOperations = 0;
			var stopwatch = new Stopwatch();
			var executionTime = TimeSpan.FromSeconds(executionTimeInSeconds);
			var caches = Enumerable.Range(0, Math.Max(2, tasks.Length / 2))
				.Select(o => (CacheBase) GetNewProvider().BuildCache(DefaultRegion, properties)).ToArray();

			if (SupportsClear)
			{
				caches[0].Clear();
			}
			else
			{
				foreach (var key in keys)
				{
					caches[0].Remove(key);
				}
			}

			if (SupportsDistributedCache)
			{
				await Task.Delay(TimeSpan.FromSeconds(1));
			}

			if (configureCachesAction != null)
			{
				await configureCachesAction(caches, keys);
			}

			stopwatch.Start();
			var cacheIndex = 0;
			for (var i = 0; i < tasks.Length; i++)
			{
				var cache = caches[cacheIndex];
				var random = new Random(masterRandom.Next());
				tasks[i] = i % 2 == 0
					? Task.Run(() => Action(random, cache))
					//: Task.Run(() => Action(random, cache));
					: ActionAsync(random, cache);

				cacheIndex = (cacheIndex + 1) % caches.Length;
			}

			await Task.WhenAll(tasks);
			stopwatch.Stop();
			foreach (var task in tasks)
			{
				task.Dispose();
			}

			Console.WriteLine("Execution time: {0}ms", stopwatch.ElapsedMilliseconds);
			Console.WriteLine("Total sync operations: {0}", totalSyncOperations);
			Console.WriteLine("Total async operations: {0}", totalAsyncOperations);
			Console.WriteLine("Operations per second: {0}", (totalSyncOperations + totalAsyncOperations) / (stopwatch.ElapsedMilliseconds / 1000));

			if (SupportsDistributedCache)
			{
				stopwatch.Restart();
				var synchronized = false;
				var synchronizationTime = TimeSpan.FromSeconds(30);
				var cachesValues = new object[caches.Length][];
				var waitTimeMs = 1;
				while (!synchronized && synchronizationTime > stopwatch.Elapsed)
				{
					synchronized = true;
					Parallel.For(0, caches.Length, i => { cachesValues[i] = caches[i].GetMany(keys); });
					if (AreValuesSynchronized(cachesValues, out _))
					{
						continue;
					}

					Thread.Sleep(waitTimeMs);
					synchronized = false;
					waitTimeMs *= 2;
				}

				stopwatch.Stop();
				Assert.That(synchronized, Is.True, "Caches are not synchronized");
				Console.WriteLine("Synchronized time: {0}ms", stopwatch.ElapsedMilliseconds);
			}
			else
			{
				var values = caches[0].GetMany(keys);
				foreach (var cache in caches.Skip(1))
				{
					Assert.That(values, Is.EquivalentTo(cache.GetMany(keys)));
				}
			}

			Console.WriteLine();

			void Action(Random random, CacheBase cache)
			{
				while (executionTime > stopwatch.Elapsed)
				{
					action(keys, random, cache);
					Interlocked.Increment(ref totalSyncOperations);
				}
			}

			async Task ActionAsync(Random random, CacheBase cache)
			{
				while (executionTime > stopwatch.Elapsed)
				{
					await asyncAction(keys, random, cache).ConfigureAwait(false);
					Interlocked.Increment(ref totalAsyncOperations);
				}
			}

			bool AreValuesSynchronized(object[][] cachesValues, out int? keyIndex)
			{
				keyIndex = null;
				var set = new HashSet<object>();
				for (var j = 0; j < keys.Length; j++)
				{
					set.Clear();
					for (var i = 0; i < caches.Length; i++)
					{
						var value = cachesValues[i][j];
						if (value == null && allowEmptyValues)
						{
							continue;
						}

						set.Add(value);
					}

					if (set.Count > 1)
					{
						keyIndex = j;
						return false;
					}
				}

				return true;
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

		private class WeightRandommizer
		{
			private readonly int[] _weights;
			private readonly int _totalWeight;

			public WeightRandommizer(params int[] weights)
			{
				_weights = weights.OrderBy(o => o).ToArray();
				_totalWeight = weights.Sum();
			}

			public int GetWeight(Random random)
			{
				var value = random.Next(_totalWeight);
				foreach (var weight in _weights)
				{
					if (value < weight)
					{
						return weight;
					}
				}

				return _weights.Last();
			}
		}
	}
}
