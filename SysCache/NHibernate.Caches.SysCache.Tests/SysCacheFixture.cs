#region License

//
//  SysCache - A cache provider for NHibernate using System.Web.Caching.Cache.
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
using System.Collections;
using System.Collections.Generic;
using System.Web;
using log4net.Config;
using NHibernate.Cache;
using NHibernate.Caches.Common.Tests;
using NUnit.Framework;

namespace NHibernate.Caches.SysCache.Tests
{
	[TestFixture]
	public partial class SysCacheFixture : CacheFixture
	{
		protected override bool SupportsSlidingExpiration => true;

		protected override void Configure(Dictionary<string, string> defaultProperties)
		{
			XmlConfigurator.Configure();
			base.Configure(defaultProperties);
			defaultProperties.Add("priority", 4.ToString());
		}

		protected override Func<ICacheProvider> ProviderBuilder =>
			() => new SysCacheProvider();

		[Test]
		public void TestDefaultConstructor()
		{
			Assert.That(() => new SysCache(), Throws.Nothing);
		}

		[Test]
		public void TestNoPropertiesConstructor()
		{
			Assert.That(() => new SysCache("TestNoPropertiesConstructor"), Throws.Nothing);
		}

		[Test]
		public void TestPriorityOutOfRange()
		{
			var props = GetDefaultProperties();
			props["priority"] = 7.ToString();
			Assert.That(() => DefaultProvider.BuildCache("TestPriorityOutOfRange", props),
				Throws.TypeOf<IndexOutOfRangeException>());
		}

		[Test]
		public void TestAfterClearCanPut()
		{
			const string key = "keyTestAfterClearCanPut";
			const string value = "value";

			var cache = GetDefaultCache();
			Assert.That(cache, Is.Not.Null, "no cache returned");

			// add the item
			cache.Put(key, value);

			Assert.That(HttpRuntime.Cache.Count, Is.GreaterThan(0), "cache is empty");

			// clear the System.Web.HttpRuntime.Cache
			var keys = new List<string>();

			foreach (DictionaryEntry entry in HttpRuntime.Cache)
			{
				keys.Add(entry.Key.ToString());
			}

			foreach (var cachekey in keys)
			{
				HttpRuntime.Cache.Remove(cachekey);
			}

			Assert.That(HttpRuntime.Cache.Count, Is.EqualTo(0), "cache isn't empty");

			// make sure we don't get an item
			var item = cache.Get(key);
			Assert.That(item, Is.Null, "item still exists in cache");

			// add the item again
			cache.Put(key, value);

			item = cache.Get(key);
			Assert.That(item, Is.Not.Null, "couldn't find item in cache");
		}
	}
}
