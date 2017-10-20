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
using System.Threading;
using System.Web;
using NHibernate.Cache;
using NUnit.Framework;

namespace NHibernate.Caches.SysCache2.Tests
{
	[TestFixture]
	public class SysCacheFixture
	{
		private SysCacheProvider provider;
		private Dictionary<string, string> props;

		[OneTimeSetUp]
		public void FixtureSetup()
		{
			props = new Dictionary<string, string> { { "expiration", 120.ToString() }, { "priority", 4.ToString() } };
			provider = new SysCacheProvider();
		}

		[Test]
		public void TestPut()
		{
			const string key = "key1";
			const string value = "value";

			ICache cache = provider.BuildCache("nunit", props);
			Assert.IsNotNull(cache, "no cache returned");

			Assert.IsNull(cache.Get(key), "cache returned an item we didn't add !?!");

			cache.Put(key, value);
			object item = cache.Get(key);
			Assert.IsNotNull(item);
			Assert.AreEqual(value, item, "didn't return the item we added");
		}

		[Test]
		public void TestRemove()
		{
			const string key = "key1";
			const string value = "value";

			ICache cache = provider.BuildCache("nunit", props);
			Assert.IsNotNull(cache, "no cache returned");

			// add the item
			cache.Put(key, value);

			// make sure it's there
			object item = cache.Get(key);
			Assert.IsNotNull(item, "item just added is not there");

			// remove it
			cache.Remove(key);

			// make sure it's not there
			item = cache.Get(key);
			Assert.IsNull(item, "item still exists in cache");
		}

		[Test]
		public void TestClear()
		{
			const string key = "key1";
			const string value = "value";

			ICache cache = provider.BuildCache("nunit", props);
			Assert.IsNotNull(cache, "no cache returned");

			// add the item
			cache.Put(key, value);

			// make sure it's there
			object item = cache.Get(key);
			Assert.IsNotNull(item, "couldn't find item in cache");

			// clear the cache
			cache.Clear();

			// make sure we don't get an item
			item = cache.Get(key);
			Assert.IsNull(item, "item still exists in cache");
		}

		[Test]
		public void TestDefaultConstructor()
		{
			ICache cache = new SysCacheRegion();
			Assert.IsNotNull(cache);
		}

		[Test]
		public void TestEmptyProperties()
		{
			ICache cache = new SysCacheRegion("nunit", new Dictionary<string, string>());
			Assert.IsNotNull(cache);
		}

		[Test]
		public void TestNullKeyPut()
		{
			ICache cache = new SysCacheRegion();
			Assert.Throws<ArgumentNullException>(() => cache.Put(null, null));
		}

		[Test]
		public void TestNullValuePut()
		{
			ICache cache = new SysCacheRegion();
			Assert.Throws<ArgumentNullException>(() => cache.Put("nunit", null));
		}

		[Test]
		public void TestNullKeyGet()
		{
			ICache cache = new SysCacheRegion();
			cache.Put("nunit", "value");
			object item = cache.Get(null);
			Assert.IsNull(item);
		}

		[Test]
		public void TestNullKeyRemove()
		{
			ICache cache = new SysCacheRegion();
			Assert.Throws<ArgumentNullException>(() => cache.Remove(null));
		}

		[Test]
		public void TestRegions()
		{
			const string key = "key";
			ICache cache1 = provider.BuildCache("nunit1", props);
			ICache cache2 = provider.BuildCache("nunit2", props);
			const string s1 = "test1";
			const string s2 = "test2";
			cache1.Put(key, s1);
			cache2.Put(key, s2);
			object get1 = cache1.Get(key);
			object get2 = cache2.Get(key);
			Assert.IsFalse(get1 == get2);
		}

		private class SomeObject
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
				var other = obj as SomeObject;

				return other?.Id == Id;
			}
		}

		[Test]
		public void TestNonEqualObjectsWithEqualHashCodeAndToString()
		{
			var obj1 = new SomeObject();
			var obj2 = new SomeObject();

			obj1.Id = 1;
			obj2.Id = 2;

			ICache cache = provider.BuildCache("nunit", props);

			Assert.IsNull(cache.Get(obj2));
			cache.Put(obj1, obj1);
			Assert.AreEqual(obj1, cache.Get(obj1));
			Assert.IsNull(cache.Get(obj2));
		}

		[Test]
		public void TestAfterClearCanPut()
		{
			const string key = "key1";
			const string value = "value";

			ICache cache = provider.BuildCache("nunit", props);
			Assert.IsNotNull(cache, "no cache returned");

			// add the item
			cache.Put(key, value);

			Assert.IsTrue(HttpRuntime.Cache.Count > 0, "cache is empty");

			// clear the System.Web.HttpRuntime.Cache
			IList keys = new ArrayList();

			foreach (DictionaryEntry entry in HttpRuntime.Cache)
			{
				keys.Add(entry.Key.ToString());
			}

			foreach (string cachekey in keys)
			{
				HttpRuntime.Cache.Remove(cachekey);
			}

			Assert.AreEqual(0, HttpRuntime.Cache.Count, "cache isn't empty");

			// make sure we don't get an item
			object item = cache.Get(key);
			Assert.IsNull(item, "item still exists in cache");

			// add the item again
			cache.Put(key, value);

			item = cache.Get(key);
			Assert.IsNotNull(item, "couldn't find item in cache");
		}
	}
}
