#region License

//
//  RtMemoryCache - A cache provider for NHibernate using System.Runtime.Caching.MemoryCache.
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
using System.Runtime.Caching;
using log4net.Config;
using NHibernate.Cache;
using NHibernate.Caches.Common.Tests;
using NUnit.Framework;

namespace NHibernate.Caches.RtMemoryCache.Tests
{
	[TestFixture]
	public partial class RtMemoryCacheFixture : CacheFixture
	{
		protected override void Configure(Dictionary<string, string> defaultProperties)
		{
			XmlConfigurator.Configure();
			base.Configure(defaultProperties);
			defaultProperties.Add("priority", 1.ToString());
		}

		protected override Func<ICacheProvider> ProviderBuilder =>
			() => new RtMemoryCacheProvider();

		[Test]
		public void TestDefaultConstructor()
		{
			Assert.That(() => new RtMemoryCache(), Throws.Nothing);
		}

		[Test]
		public void TestNoPropertiesConstructor()
		{
			Assert.That(() => new RtMemoryCache("TestNoPropertiesConstructor"), Throws.Nothing);
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

			Assert.That(MemoryCache.Default, Is.Not.Empty, "cache is empty");

			// clear the System.Runtime.Caching.MemoryCache
			var keys = new List<string>();

			foreach (var entry in MemoryCache.Default)
			{
				keys.Add(entry.Key);
			}

			foreach (var cachekey in keys)
			{
				MemoryCache.Default.Remove(cachekey);
			}

			Assert.That(MemoryCache.Default, Is.Empty, "cache isn't empty");

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
