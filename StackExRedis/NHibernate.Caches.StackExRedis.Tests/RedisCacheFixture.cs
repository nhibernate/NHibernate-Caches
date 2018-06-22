using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using NHibernate.Cache;
using NHibernate.Cache.Entry;
using NHibernate.Caches.Common.Tests;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Persister.Entity;
using NHibernate.Type;
using NSubstitute;
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

		[Serializable]
		public class MyEntity
		{
			public int Id { get; set; }
		}

		[Test]
		public void TestNHibernateAnyTypeSerialization()
		{
			var objectTypeCacheEntryType = typeof(AnyType.ObjectTypeCacheEntry);
			var entityNameField = objectTypeCacheEntryType.GetField("entityName", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(entityNameField, Is.Not.Null, "field entityName in NHibernate.Type.AnyType.ObjectTypeCacheEntry was not found");
			var idField = objectTypeCacheEntryType.GetField("id", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(idField, Is.Not.Null, "field id in NHibernate.Type.AnyType.ObjectTypeCacheEntry was not found");

			var entityName = nameof(MyEntity);
			var propertyValues = new Dictionary<IType, object>
			{
				{NHibernateUtil.Object, new MyEntity{Id = 2}}
			};

			var sfImpl = Substitute.For<ISessionFactoryImplementor>();
			var sessionImpl = Substitute.For<ISessionImplementor>();
			sessionImpl.BestGuessEntityName(Arg.Any<object>()).Returns(o => o[0].GetType().Name);
			sessionImpl.GetContextEntityIdentifier(Arg.Is<object>(o => o is MyEntity)).Returns(o => ((MyEntity) o[0]).Id);
			var entityPersister = Substitute.For<IEntityPersister>();
			entityPersister.EntityName.Returns(entityName);
			entityPersister.IsLazyPropertiesCacheable.Returns(false);
			entityPersister.PropertyTypes.Returns(propertyValues.Keys.ToArray());

			var cacheKey = new CacheKey(1, NHibernateUtil.Int32, entityName, sfImpl);
			var cacheEntry = new CacheEntry(propertyValues.Values.ToArray(), entityPersister, false, null, sessionImpl, null);

			Assert.That(cacheEntry.DisassembledState, Has.Length.EqualTo(1));
			var anyObject = cacheEntry.DisassembledState[0];
			Assert.That(anyObject, Is.TypeOf(objectTypeCacheEntryType));
			Assert.That(entityNameField.GetValue(anyObject), Is.EqualTo(nameof(MyEntity)));
			Assert.That(idField.GetValue(anyObject), Is.EqualTo(2));

			var cache = GetDefaultCache();
			cache.Put(cacheKey, cacheEntry);
			var value = cache.Get(cacheKey);

			Assert.That(value, Is.TypeOf<CacheEntry>());
			var retrievedCacheEntry = (CacheEntry) value;
			Assert.That(retrievedCacheEntry.DisassembledState, Has.Length.EqualTo(1));
			var retrievedAnyObject = retrievedCacheEntry.DisassembledState[0];
			Assert.That(retrievedAnyObject, Is.TypeOf(objectTypeCacheEntryType));
			Assert.That(entityNameField.GetValue(retrievedAnyObject), Is.EqualTo(nameof(MyEntity)),
				"entityName is different from the original AnyType.ObjectTypeCacheEntry");
			Assert.That(idField.GetValue(retrievedAnyObject), Is.EqualTo(2),
				"id is different from the original AnyType.ObjectTypeCacheEntry");
		}

		[Test]
		public void TestNHibernateStandardTypesSerialization()
		{
			var entityName = nameof(MyEntity);
			var xmlDoc = new XmlDocument();
			xmlDoc.LoadXml("<Root>XmlDoc</Root>");
			var propertyValues = new Dictionary<IType, object>
			{
				{NHibernateUtil.AnsiString, "test"},
				{NHibernateUtil.Binary, new byte[] {1, 2, 3, 4}},
				{NHibernateUtil.BinaryBlob, new byte[] {1, 2, 3, 4}},
				{NHibernateUtil.Boolean, true},
				{NHibernateUtil.Byte, (byte) 1},
				{NHibernateUtil.Character, 'a'},
				{NHibernateUtil.CultureInfo, CultureInfo.CurrentCulture},
				{NHibernateUtil.DateTime, DateTime.Now},
				{NHibernateUtil.DateTimeNoMs, DateTime.Now},
				{NHibernateUtil.LocalDateTime, DateTime.Now},
				{NHibernateUtil.UtcDateTime, DateTime.UtcNow},
				{NHibernateUtil.LocalDateTimeNoMs, DateTime.Now},
				{NHibernateUtil.UtcDateTimeNoMs, DateTime.UtcNow},
				{NHibernateUtil.DateTimeOffset, DateTimeOffset.Now},
				{NHibernateUtil.Date, DateTime.Today},
				{NHibernateUtil.Decimal, 2.5m},
				{NHibernateUtil.Double, 2.5d},
				{NHibernateUtil.Currency, 2.5m},
				{NHibernateUtil.Guid, Guid.NewGuid()},
				{NHibernateUtil.Int16, (short) 1},
				{NHibernateUtil.Int32, 3},
				{NHibernateUtil.Int64, 3L},
				{NHibernateUtil.SByte, (sbyte) 1},
				{NHibernateUtil.UInt16, (ushort) 1},
				{NHibernateUtil.UInt32, (uint) 1},
				{NHibernateUtil.UInt64, (ulong) 1},
				{NHibernateUtil.Single, 1.1f},
				{NHibernateUtil.String, "test"},
				{NHibernateUtil.StringClob, "test"},
				{NHibernateUtil.Time, DateTime.Now},
				{NHibernateUtil.Ticks, DateTime.Now},
				{NHibernateUtil.TimeAsTimeSpan, TimeSpan.FromMilliseconds(15)},
				{NHibernateUtil.TimeSpan, TimeSpan.FromMilliseconds(1234)},
				{NHibernateUtil.DbTimestamp, DateTime.Now},
				{NHibernateUtil.TrueFalse, false},
				{NHibernateUtil.YesNo, true},
				{NHibernateUtil.Class, typeof(IType)},
				{NHibernateUtil.ClassMetaType, entityName},
				{NHibernateUtil.Serializable, new MyEntity {Id = 1}},
				{NHibernateUtil.AnsiChar, 'a'},
				{NHibernateUtil.XmlDoc, xmlDoc},
				{NHibernateUtil.XDoc, XDocument.Parse("<Root>XDoc</Root>")},
				{NHibernateUtil.Uri, new Uri("http://test.com")}
			};

			var sfImpl = Substitute.For<ISessionFactoryImplementor>();
			var sessionImpl = Substitute.For<ISessionImplementor>();
			var entityPersister = Substitute.For<IEntityPersister>();
			entityPersister.EntityName.Returns(entityName);
			entityPersister.IsLazyPropertiesCacheable.Returns(false);
			entityPersister.PropertyTypes.Returns(propertyValues.Keys.ToArray());

			var cacheKey = new CacheKey(1, NHibernateUtil.Int32, entityName, sfImpl);
			var cacheEntry = new CacheEntry(propertyValues.Values.ToArray(), entityPersister, false, null, sessionImpl, null);

			var cache = GetDefaultCache();
			cache.Put(cacheKey, cacheEntry);
			var value = cache.Get(cacheKey);

			Assert.That(value, Is.TypeOf<CacheEntry>());
			var retrievedCacheEntry = (CacheEntry) value;
			Assert.That(retrievedCacheEntry.DisassembledState, Is.EquivalentTo(cacheEntry.DisassembledState),
				"DisassembledState is different from the original CacheEntry");
		}

		[Test]
		public void TestNHibernateCacheEntrySerialization()
		{
			var entityName = nameof(MyEntity);
			var propertyValues = new Dictionary<IType, object>
			{
				{NHibernateUtil.String, "test"}
			};

			var sfImpl = Substitute.For<ISessionFactoryImplementor>();
			var sessionImpl = Substitute.For<ISessionImplementor>();
			var entityPersister = Substitute.For<IEntityPersister>();
			entityPersister.EntityName.Returns(entityName);
			entityPersister.IsLazyPropertiesCacheable.Returns(false);
			entityPersister.PropertyTypes.Returns(propertyValues.Keys.ToArray());

			var cacheKey = new CacheKey(1, NHibernateUtil.Int32, entityName, sfImpl);
			var cacheEntry = new CacheEntry(propertyValues.Values.ToArray(), entityPersister, true, 4, sessionImpl, null);

			var cache = GetDefaultCache();
			cache.Put(cacheKey, cacheEntry);
			var value = cache.Get(cacheKey);

			Assert.That(value, Is.TypeOf<CacheEntry>());
			var retrievedCacheEntry = (CacheEntry) value;
			Assert.That(retrievedCacheEntry.AreLazyPropertiesUnfetched, Is.EqualTo(cacheEntry.AreLazyPropertiesUnfetched),
				"AreLazyPropertiesUnfetched is different from the original CacheEntry");
			Assert.That(retrievedCacheEntry.DisassembledState, Is.EquivalentTo(cacheEntry.DisassembledState),
				"DisassembledState is different from the original CacheEntry");
			Assert.That(retrievedCacheEntry.Subclass, Is.EqualTo(cacheEntry.Subclass),
				"Subclass is different from the original CacheEntry");
			Assert.That(retrievedCacheEntry.Version, Is.EqualTo(cacheEntry.Version),
				"Version is different from the original CacheEntry");
		}

		[Test]
		public void TestNHibernateCollectionCacheEntrySerialization()
		{
			var sfImpl = Substitute.For<ISessionFactoryImplementor>();
			var collection = Substitute.For<IPersistentCollection>();
			collection.Disassemble(null).Returns(o => new object[] {"test"});

			var cacheKey = new CacheKey(1, NHibernateUtil.Int32, "MyCollection", sfImpl);
			var cacheEntry = new CollectionCacheEntry(collection, null);
			Assert.That(cacheEntry.State, Has.Length.EqualTo(1));

			var cache = GetDefaultCache();
			cache.Put(cacheKey, cacheEntry);
			var value = cache.Get(cacheKey);

			Assert.That(value, Is.TypeOf<CollectionCacheEntry>());
			var retrievedCacheEntry = (CollectionCacheEntry) value;
			Assert.That(retrievedCacheEntry.State, Has.Length.EqualTo(1));
			Assert.That(retrievedCacheEntry.State[0], Is.EquivalentTo("test"),
				"State is different from the original CollectionCacheEntry");
		}

		[Test]
		public void TestNHibernateCacheLockSerialization()
		{
			var sfImpl = Substitute.For<ISessionFactoryImplementor>();
			var cacheKey = new CacheKey(1, NHibernateUtil.Int32, "CacheLock", sfImpl);
			var cacheEntry = new CacheLock(1234, 1, 5);
			cacheEntry.Lock(123, 2);

			var cache = GetDefaultCache();
			cache.Put(cacheKey, cacheEntry);
			var value = cache.Get(cacheKey);

			Assert.That(value, Is.TypeOf<CacheLock>());
			var retrievedCacheEntry = (CacheLock) value;
			Assert.That(retrievedCacheEntry.Id, Is.EqualTo(cacheEntry.Id),
				"Id is different from the original CacheLock");
			Assert.That(retrievedCacheEntry.IsLock, Is.EqualTo(cacheEntry.IsLock),
				"IsLock is different from the original CacheLock");
			Assert.That(retrievedCacheEntry.WasLockedConcurrently, Is.EqualTo(cacheEntry.WasLockedConcurrently),
				"WasLockedConcurrently is different from the original CacheLock");
			Assert.That(retrievedCacheEntry.ToString(), Is.EqualTo(cacheEntry.ToString()),
				"ToString() is different from the original CacheLock");
		}

		[Test]
		public void TestNHibernateCachedItemSerialization()
		{
			var sfImpl = Substitute.For<ISessionFactoryImplementor>();
			var cacheKey = new CacheKey(1, NHibernateUtil.Int32, "CachedItem", sfImpl);
			var cacheEntry = new CachedItem("test", 111, 5);
			cacheEntry.Lock(123, 2);

			var cache = GetDefaultCache();
			cache.Put(cacheKey, cacheEntry);
			var value = cache.Get(cacheKey);

			Assert.That(value, Is.TypeOf<CachedItem>());
			var retrievedCacheEntry = (CachedItem) value;
			Assert.That(retrievedCacheEntry.FreshTimestamp, Is.EqualTo(cacheEntry.FreshTimestamp),
				"FreshTimestamp is different from the original CachedItem");
			Assert.That(retrievedCacheEntry.IsLock, Is.EqualTo(cacheEntry.IsLock),
				"IsLock is different from the original CachedItem");
			Assert.That(retrievedCacheEntry.Value, Is.EqualTo(cacheEntry.Value),
				"Value is different from the original CachedItem");
			Assert.That(retrievedCacheEntry.ToString(), Is.EqualTo(cacheEntry.ToString()),
				"ToString() is different from the original CachedItem");
		}

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
