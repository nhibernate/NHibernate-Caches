#region License

//
//  MemCache - A cache provider for NHibernate using the .NET client
//  (http://sourceforge.net/projects/memcacheddotnet) for memcached,
//  which is located at http://www.danga.com/memcached/.
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
// CLOVER:OFF
//

#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using log4net.Config;
using NHibernate.Cache;
using NHibernate.Caches.Common.Tests;
using NUnit.Framework;

namespace NHibernate.Caches.MemCache.Tests
{
	[TestFixture]
	public partial class MemCacheFixture : CacheFixture
	{
		protected override bool SupportsDistinguishingKeysWithSameStringRepresentationAndHashcode => false;

		protected override void Configure(Dictionary<string, string> defaultProperties)
		{
			XmlConfigurator.Configure();
			base.Configure(defaultProperties);
			defaultProperties.Add("compression_enabled", "false");
		}

		protected override Func<ICacheProvider> ProviderBuilder =>
			() => new MemCacheProvider();

		[Test]
		public void TestDefaultConstructor()
		{
			Assert.That(() => new MemCacheClient(), Throws.Nothing);
		}

		[Test]
		public void TestNoPropertiesConstructor()
		{
			Assert.That(() => new MemCacheClient("TestNoPropertiesConstructor"), Throws.Nothing);
		}

		[Test]
		public void TestRemove144()
		{
			string key = "keyTestRemove144";
			string value = "value";

			//memcached 1.4+ drops support for expiration time specified for Delete operations
			//therefore if you install memcached 1.4.4 this test will fail unless corresponding fix is implemented in MemCacheClient.cs
			//the test will fail because Remove won't actually delete the item from the cache!
			//the error you will see in the log is: "Error deleting key: nunit@key1.  Server response: CLIENT_ERROR bad command line format.  Usage: delete <key> [noreply]"

			//Now, Memcached.ClientLibrary incorrectly divides expiration time for Delete operation by 1000
			//(for Add and Set operations the expiration time is calculated correctly)
			//that's why we need to set expiration to 20000, otherwise it will be treated as 20ms which is too small to be sent to server (the minimum value is 1 second)
			var props = GetDefaultProperties();
			props["expiration"] = "20000";

			//disabling lingering delete will cause the item to get immediately deleted
			//this parameter is NEW and the code to make it work is part of the proposed fix
			props.Add("lingering_delete_disabled", "true");

			var cache = DefaultProvider.BuildCache("TestRemove144", props);
			Assert.That(cache, Is.Not.Null, "no cache returned");

			// add the item
			cache.Put(key, value);
			Thread.Sleep(1000);

			// make sure it's there
			var item = cache.Get(key);
			Assert.That(item, Is.Not.Null, "item just added is not there");

			// remove it
			cache.Remove(key);

			// make sure it's not there
			item = cache.Get(key);
			Assert.That(item, Is.Null, "item still exists in cache");
		}
	}
}
