#region License

//Microsoft Public License (Ms-PL)
//
//This license governs use of the accompanying software. If you use the software, you accept this license. If you do not accept the license, do not use the software.
//
//1. Definitions
//
//The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under U.S. copyright law.
//
//A "contribution" is the original software, or any additions or changes to the software.
//
//A "contributor" is any person that distributes its contribution under this license.
//
//"Licensed patents" are a contributor's patent claims that read directly on its contribution.
//
//2. Grant of Rights
//
//(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
//
//(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
//
//3. Conditions and Limitations
//
//(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
//
//(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
//
//(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
//
//(D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
//
//(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.

#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using log4net.Config;
using NHibernate.Cache;
using NUnit.Framework;

namespace NHibernate.Caches.Velocity.Tests
{
	[TestFixture]
	public class VelocityFixture
	{
		private VelocityProvider provider;
		private Dictionary<string, string> props;

		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			XmlConfigurator.Configure();
			props = new Dictionary<string, string>();
			provider = new VelocityProvider();
			provider.Start(props);
		}

		[TestFixtureTearDown]
		public void FixtureStop()
		{
			provider.Stop();
		}

		[Test]
		public void TestClear()
		{
			string key = "key1";
			string value = "value";

			ICache cache = provider.BuildCache("nunit", props);
			Assert.IsNotNull(cache, "no cache returned");

			// add the item
			cache.Put(key, value);
			Thread.Sleep(1000);

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
			ICache cache = new VelocityClient();
			Assert.IsNotNull(cache);
		}

		[Test]
		public void TestEmptyProperties()
		{
			ICache cache = new VelocityClient("nunit", new Dictionary<string, string>());
			Assert.IsNotNull(cache);
		}

		[Test]
		public void TestNoPropertiesConstructor()
		{
			ICache cache = new VelocityClient("nunit");
			Assert.IsNotNull(cache);
		}

		[Test]
		public void TestNullKeyGet()
		{
			ICache cache = new VelocityClient();
			cache.Put("nunit", "value");
			Thread.Sleep(1000);
			object item = cache.Get(null);
			Assert.IsNull(item);
		}

		[Test]
		public void TestNullKeyPut()
		{
			ICache cache = new VelocityClient();
			Assert.Throws<ArgumentNullException>(() => cache.Put(null, null));
		}

		[Test]
		public void TestNullKeyRemove()
		{
			ICache cache = new VelocityClient();
			Assert.Throws<ArgumentNullException>(() => cache.Remove(null));
		}

		[Test]
		public void TestNullValuePut()
		{
			ICache cache = new VelocityClient();
			Assert.Throws<ArgumentNullException>(() => cache.Put("nunit", null));
		}

		[Test]
		public void TestPut()
		{
			string key = "key1";
			string value = "value";

			ICache cache = provider.BuildCache("nunit", props);
			Assert.IsNotNull(cache, "no cache returned");

			Assert.IsNull(cache.Get(key), "cache returned an item we didn't add !?!");

			cache.Put(key, value);
			Thread.Sleep(1000);
			object item = cache.Get(key);
			Assert.IsNotNull(item);
			Assert.AreEqual(value, item, "didn't return the item we added");
		}

		[Test]
		public void TestRegions()
		{
			string key = "key";
			ICache cache1 = provider.BuildCache("nunit1", props);
			ICache cache2 = provider.BuildCache("nunit2", props);
			string s1 = "test1";
			string s2 = "test2";
			cache1.Put(key, s1);
			cache2.Put(key, s2);
			Thread.Sleep(1000);
			object get1 = cache1.Get(key);
			object get2 = cache2.Get(key);
			Assert.IsFalse(get1 == get2);
		}

		[Test]
		public void TestRemove()
		{
			string key = "key1";
			string value = "value";

			ICache cache = provider.BuildCache("nunit", props);
			Assert.IsNotNull(cache, "no cache returned");

			// add the item
			cache.Put(key, value);
			Thread.Sleep(1000);

			// make sure it's there
			object item = cache.Get(key);
			Assert.IsNotNull(item, "item just added is not there");

			// remove it
			cache.Remove(key);

			// make sure it's not there
			item = cache.Get(key);
			Assert.IsNull(item, "item still exists in cache");
		}
	}
}