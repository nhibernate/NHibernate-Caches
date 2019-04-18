#region License

//
//  CoreDistributedCache - A cache provider for NHibernate using Microsoft.Extensions.Caching.Distributed.IDistributedCache.
//
//  This library is free software; you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation; either
//  version 2.1 of the License, or (at your option) any later version.
//
//  This library is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//  Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//

#endregion

using System;
using System.Collections.Generic;
using System.Reflection;
using Enyim.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Caching.SqlServer;
using NHibernate.Caches.CoreDistributedCache.Memcached;
using NHibernate.Caches.CoreDistributedCache.Memory;
using NHibernate.Caches.CoreDistributedCache.Redis;
using NHibernate.Caches.CoreDistributedCache.SqlServer;
using NUnit.Framework;

namespace NHibernate.Caches.CoreDistributedCache.Tests
{
	[TestFixture]
	public class DistributedCacheFactoryFixture
	{
		[Test]
		public void MemcachedCacheFactory()
		{
			var factory =
				new MemcachedFactory(new Dictionary<string, string>
				{
					{
						"configuration", @"{
  ""Servers"": [
    {
      ""Address"": ""memcached"",
      ""Port"": 11211
    }
  ],
  ""Authentication"": {
    ""Type"": ""Enyim.Caching.Memcached.PlainTextAuthenticator"",
    ""Parameters"": {
      ""zone"": """",
      ""userName"": ""username"",
      ""password"": ""password""
    }
  }
}"
					}
				});
			var cache1 = factory.BuildCache();
			Assert.That(cache1, Is.Not.Null, "Factory has yielded null");
			Assert.That(cache1, Is.InstanceOf<MemcachedClient>(), "Unexpected cache");
			var cache2 = factory.BuildCache();
			Assert.That(cache2, Is.EqualTo(cache1),
				"The Memcached cache factory is supposed to always yield the same instance");

			var keySanitizer = factory.Constraints?.KeySanitizer;
			Assert.That(keySanitizer, Is.Not.Null, "Factory lacks a key sanitizer");
			Assert.That(keySanitizer("--abc \n\r\t\v\fdef--"), Is.EqualTo("--abc------def--"), "Unexpected key sanitization");
		}

		private static readonly FieldInfo MemoryCacheField =
			typeof(MemoryDistributedCache).GetField("_memCache", BindingFlags.Instance | BindingFlags.NonPublic);

		private static readonly FieldInfo MemoryCacheOptionsField =
			typeof(MemoryCache).GetField("_options", BindingFlags.Instance | BindingFlags.NonPublic);

		[Test]
		public void MemoryCacheFactory()
		{
			var factory =
				new MemoryFactory(new Dictionary<string, string>
				{
					{ "expiration-scan-frequency", "00:10:00" },
					{ "size-limit", "1048576" }
				});
			Assert.That(factory, Is.Not.Null, "Factory not found");
			Assert.That(factory, Is.InstanceOf<MemoryFactory>(), "Unexpected factory");
			var cache1 = factory.BuildCache();
			Assert.That(cache1, Is.Not.Null, "Factory has yielded null");
			Assert.That(cache1, Is.InstanceOf<MemoryDistributedCache>(), "Unexpected cache");
			var cache2 = factory.BuildCache();
			Assert.That(cache2, Is.EqualTo(cache1),
				"The distributed cache factory is supposed to always yield the same instance");

			var memCache = MemoryCacheField.GetValue(cache1);
			Assert.That(memCache, Is.Not.Null, "Underlying memory cache not found");
			Assert.That(memCache, Is.InstanceOf<MemoryCache>(), "Unexpected memory cache");
			var options = MemoryCacheOptionsField.GetValue(memCache);
			Assert.That(options, Is.Not.Null, "Memory cache options not found");
			Assert.That(options, Is.InstanceOf<MemoryCacheOptions>(), "Unexpected options type");
			var memOptions = (MemoryCacheOptions) options;
			Assert.That(memOptions.ExpirationScanFrequency, Is.EqualTo(TimeSpan.FromMinutes(10)));
			Assert.That(memOptions.SizeLimit, Is.EqualTo(1048576));
		}

		private static readonly FieldInfo RedisCacheOptionsField =
			typeof(RedisFactory).GetField("_options", BindingFlags.Instance | BindingFlags.NonPublic);

		[Test]
		public void RedisCacheFactory()
		{
			var factory =
				new RedisFactory(new Dictionary<string, string>
				{
					{ "configuration", "config" },
					{ "instance-name", "instance" }
				});
			var cache1 = factory.BuildCache();
			Assert.That(cache1, Is.Not.Null, "Factory has yielded null");
			Assert.That(cache1, Is.InstanceOf<RedisCache>(), "Unexpected cache");
			var cache2 = factory.BuildCache();
			Assert.That(cache2, Is.Not.EqualTo(cache1),
				"The Redis cache factory is supposed to always yield a new instance");

			var options = RedisCacheOptionsField.GetValue(factory);
			Assert.That(options, Is.Not.Null, "Factory cache options not found");
			Assert.That(options, Is.InstanceOf<RedisCacheOptions>(), "Unexpected options type");
			var redisOptions = (RedisCacheOptions) options;
			Assert.That(redisOptions.Configuration, Is.EqualTo("config"));
			Assert.That(redisOptions.InstanceName, Is.EqualTo("instance"));
		}

		private static readonly FieldInfo SqlServerCacheOptionsField =
			typeof(SqlServerFactory).GetField("_options", BindingFlags.Instance | BindingFlags.NonPublic);

		[Test]
		public void SqlServerCacheFactory()
		{
			var factory = new SqlServerFactory(new Dictionary<string, string>
			{
				{ "connection-string", "connection" },
				{ "schema-name", "schema" },
				{ "table-name", "table" },
				{ "expired-items-deletion-interval", "5" }
			});
			var cache1 = factory.BuildCache();
			Assert.That(cache1, Is.Not.Null, "Factory has yielded null");
			Assert.That(cache1, Is.InstanceOf<SqlServerCache>(), "Unexpected cache");
			var cache2 = factory.BuildCache();
			Assert.That(cache2, Is.Not.EqualTo(cache1),
				"The SQL Server cache factory is supposed to always yield a new instance");

			var options = SqlServerCacheOptionsField.GetValue(factory);
			Assert.That(options, Is.Not.Null, "Factory cache options not found");
			Assert.That(options, Is.InstanceOf<SqlServerCacheOptions>(), "Unexpected options type");
			var sqlServerOptions = (SqlServerCacheOptions) options;
			Assert.That(sqlServerOptions.ConnectionString, Is.EqualTo("connection"));
			Assert.That(sqlServerOptions.SchemaName, Is.EqualTo("schema"));
			Assert.That(sqlServerOptions.TableName, Is.EqualTo("table"));
			Assert.That(sqlServerOptions.ExpiredItemsDeletionInterval, Is.EqualTo(TimeSpan.FromMinutes(5)));
		}
	}
}
