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
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.Caching.SqlServer;
using NHibernate.Caches.CoreDistributedCache.Redis;
using NHibernate.Caches.CoreDistributedCache.SqlServer;
using NUnit.Framework;

namespace NHibernate.Caches.CoreDistributedCache.Tests
{
	[TestFixture]
	public class DistributedCacheFactoryFixture
	{
		private static readonly FieldInfo RedisCacheOptionsField =
			typeof(RedisFactory).GetField("_options", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly FieldInfo SqlServerCacheOptionsField =
			typeof(SqlServerFactory).GetField("_options", BindingFlags.Instance | BindingFlags.NonPublic);

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
