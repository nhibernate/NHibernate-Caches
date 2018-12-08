using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
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

namespace NHibernate.Caches.StackExchangeRedis.Tests
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
		protected class CustomCacheKey
		{
			private readonly int _hashCode;

			public CustomCacheKey(object id, string entityName, bool useIdHashCode)
			{
				Id = id;
				EntityName = entityName;
				_hashCode = useIdHashCode ? id.GetHashCode() : base.GetHashCode();
			}

			public object Id { get; }

			public string EntityName { get; }

			public override string ToString()
			{
				return EntityName + "#" + Id;
			}

			public override bool Equals(object obj)
			{
				if (!(obj is CustomCacheKey other))
				{
					return false;
				}
				return Equals(other.Id, Id) && Equals(other.EntityName, EntityName);
			}

			public override int GetHashCode()
			{
				return _hashCode.GetHashCode();
			}
		}

		[Test]
		public void TestEqualObjectsWithDifferentHashCode()
		{
			var value = "value";
			var obj1 = new CustomCacheKey(1, "test", false);
			var obj2 = new CustomCacheKey(1, "test", false);

			var cache = GetDefaultCache();

			cache.Put(obj1, value);
			Assert.That(cache.Get(obj1), Is.EqualTo(value), "Unable to retrieved cached object for key obj1");
			Assert.That(cache.Get(obj2), Is.EqualTo(value), "Unable to retrieved cached object for key obj2");
			cache.Remove(obj1);
		}

		[Test]
		public void TestEqualObjectsWithDifferentHashCodeAndUseHashCodeGlobalConfiguration()
		{
			var value = "value";
			var obj1 = new CustomCacheKey(1, "test", false);
			var obj2 = new CustomCacheKey(1, "test", false);

			var props = GetDefaultProperties();
			var cacheProvider = ProviderBuilder();
			props[RedisEnvironment.AppendHashcode] = "true";
			cacheProvider.Start(props);
			var cache = cacheProvider.BuildCache(DefaultRegion, props);

			cache.Put(obj1, value);
			Assert.That(cache.Get(obj1), Is.EqualTo(value), "Unable to retrieved cached object for key obj1");
			Assert.That(cache.Get(obj2), Is.Null, "The hash code should be used in the cache key");
			cache.Remove(obj1);
		}

		[Test]
		public void TestEqualObjectsWithDifferentHashCodeAndUseHashCodeRegionConfiguration()
		{
			var value = "value";
			var obj1 = new CustomCacheKey(1, "test", false);
			var obj2 = new CustomCacheKey(1, "test", false);

			var props = GetDefaultProperties();
			var cacheProvider = ProviderBuilder();
			cacheProvider.Start(props);
			props["append-hashcode"] = "true";
			var cache = cacheProvider.BuildCache(DefaultRegion, props);

			cache.Put(obj1, value);
			Assert.That(cache.Get(obj1), Is.EqualTo(value), "Unable to retrieved cached object for key obj1");
			Assert.That(cache.Get(obj2), Is.Null, "The hash code should be used in the cache key");
			cache.Remove(obj1);
		}

		[Serializable]
		protected class ObjectEqualToString
		{
			public ObjectEqualToString(int id)
			{
				Id = id;
			}

			public int Id { get; }

			public override int GetHashCode()
			{
				return Id.GetHashCode();
			}

			public override string ToString()
			{
				return nameof(ObjectEqualToString);
			}

			public override bool Equals(object obj)
			{
				if (!(obj is ObjectEqualToString other))
				{
					return false;
				}

				return other.Id == Id;
			}
		}

		[Test]
		public void TestNonEqualObjectsWithEqualToString()
		{
			var value = "value";
			var obj1 = new CustomCacheKey(new ObjectEqualToString(1), "test", true);
			var obj2 = new CustomCacheKey(new ObjectEqualToString(2), "test", true);

			var cache = GetDefaultCache();

			cache.Put(obj1, value);
			Assert.That(cache.Get(obj1), Is.EqualTo(value), "Unable to retrieved cached object for key obj1");
			Assert.That(cache.Get(obj2), Is.EqualTo(value), "Unable to retrieved cached object for key obj2");
			cache.Remove(obj1);
		}

		[Test]
		public void TestNonEqualObjectsWithEqualToStringUseHashCode()
		{
			var value = "value";
			var obj1 = new CustomCacheKey(new ObjectEqualToString(1), "test", true);
			var obj2 = new CustomCacheKey(new ObjectEqualToString(2), "test", true);

			var props = GetDefaultProperties();
			var cacheProvider = ProviderBuilder();
			props[RedisEnvironment.AppendHashcode] = "true";
			cacheProvider.Start(props);
			var cache = cacheProvider.BuildCache(DefaultRegion, props);

			cache.Put(obj1, value);
			Assert.That(cache.Get(obj1), Is.EqualTo(value), "Unable to retrieved cached object for key obj1");
			Assert.That(cache.Get(obj2), Is.Null, "Unexpectedly found a cache entry for key obj2 after obj1 put");
			cache.Remove(obj1);
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
	}
}
