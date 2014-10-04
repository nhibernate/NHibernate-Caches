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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using log4net.Config;
using NHibernate.Cache;
using NUnit.Framework;

namespace NHibernate.Caches.RtMemoryCache.Tests
{
	[TestFixture]
	public class RtMemoryCacheFixture
	{
		private RtMemoryCacheProvider provider;
		private Dictionary<string, string> props;

		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			XmlConfigurator.Configure();
			props = new Dictionary<string, string>();
			props.Add("expiration", 120.ToString());
			props.Add("priority", 1.ToString());
			provider = new RtMemoryCacheProvider();
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
			ICache cache = new RtMemoryCache();
			Assert.IsNotNull(cache);
		}

		[Test]
		public void TestNoPropertiesConstructor()
		{
			ICache cache = new RtMemoryCache("nunit");
			Assert.IsNotNull(cache);
		}

		[Test]
		public void TestPriorityOutOfRange()
		{
			var h = new Dictionary<string, string>();
			h.Add("priority", 7.ToString());
			Assert.Throws<IndexOutOfRangeException>(()=> new RtMemoryCache("nunit", h));
		}

		[Test]
		public void TestBadRelativeExpiration()
		{
			var h = new Dictionary<string, string>();
			h.Add("expiration", "foobar");
			Assert.Throws<ArgumentException>(() => new RtMemoryCache("nunit", h));
		}

		[Test]
		public void TestEmptyProperties()
		{
			ICache cache = new RtMemoryCache("nunit", new Dictionary<string, string>());
			Assert.IsNotNull(cache);
		}

		[Test]
		public void TestNullKeyPut()
		{
			ICache cache = new RtMemoryCache();
			Assert.Throws<ArgumentNullException>(() => cache.Put(null, null));
		}

		[Test]
		public void TestNullValuePut()
		{
			ICache cache = new RtMemoryCache();
			Assert.Throws<ArgumentNullException>(() => cache.Put("nunit", null));
		}

		[Test]
		public void TestNullKeyGet()
		{
			ICache cache = new RtMemoryCache();
			cache.Put("nunit", "value");
			object item = cache.Get(null);
			Assert.IsNull(item);
		}

		[Test]
		public void TestNullKeyRemove()
		{
			ICache cache = new RtMemoryCache();
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

				if (other == null)
				{
					return false;
				}

				return other.Id == Id;
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
		public void TestObjectExpiration()
		{
			const int expirySeconds = 3;
			const string key = "key";
			var obj = new SomeObject();

			obj.Id = 2;

			var localProps = new Dictionary<string, string>();
			localProps.Add("expiration", expirySeconds.ToString());

			ICache cache = provider.BuildCache("nunit", localProps);

			Assert.IsNull(cache.Get(obj));
			cache.Put(key, obj);

			// Wait
			Thread.Sleep(TimeSpan.FromSeconds(expirySeconds + 2));

			// Check it expired
			Assert.IsNull(cache.Get(key));
		}

		[Test]
		public void TestObjectExpirationAfterUpdate()
		{
			const int expirySeconds = 3;
			const string key = "key";
			var obj = new SomeObject();

			obj.Id = 2;

			var localProps = new Dictionary<string, string>();
			localProps.Add("expiration", expirySeconds.ToString());

			ICache cache = provider.BuildCache("nunit", localProps);

			Assert.IsNull(cache.Get(obj));
			cache.Put(key, obj);

			// This forces an object update
			cache.Put(key, obj);

			// Wait
			Thread.Sleep(TimeSpan.FromSeconds(expirySeconds + 2));

			// Check it expired
			Assert.IsNull(cache.Get(key));
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

			Assert.IsTrue(MemoryCache.Default.Any(), "cache is empty");

			// clear the System.Runtime.Caching.MemoryCache
			IList keys = new ArrayList();

			foreach (KeyValuePair<string, object> entry in MemoryCache.Default)
			{
				keys.Add(entry.Key);
			}

			foreach (string cachekey in keys)
			{
				MemoryCache.Default.Remove(cachekey);
			}

			Assert.AreEqual(0, MemoryCache.Default.Count(), "cache isn't empty");

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