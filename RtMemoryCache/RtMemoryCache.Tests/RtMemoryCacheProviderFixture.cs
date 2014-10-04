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

using System.Collections.Generic;
using NHibernate.Cache;
using NUnit.Framework;

namespace NHibernate.Caches.RtMemoryCache.Tests
{
	[TestFixture]
	public class RtMemoryCacheProviderFixture
	{
		#region Setup/Teardown

		[SetUp]
		public void Setup()
		{
			provider = new RtMemoryCacheProvider();
		}

		#endregion

		private RtMemoryCacheProvider provider;
		private Dictionary<string, string> props;

		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			props = new Dictionary<string, string> {{"expiration", 120.ToString()}, {"priority", 2.ToString()}};
		}

		[Test]
		public void TestBuildCacheFromConfig()
		{
			ICache cache = provider.BuildCache("foo", null);
			Assert.IsNotNull(cache, "pre-configured cache not found");
		}

		[Test]
		public void TestBuildCacheNullNull()
		{
			ICache cache = provider.BuildCache(null, null);
			Assert.IsNotNull(cache, "no cache returned");
		}

		[Test]
		public void TestBuildCacheStringICollection()
		{
			ICache cache = provider.BuildCache("another_region", props);
			Assert.IsNotNull(cache, "no cache returned");
		}

		[Test]
		public void TestBuildCacheStringNull()
		{
			ICache cache = provider.BuildCache("a_region", null);
			Assert.IsNotNull(cache, "no cache returned");
		}

		[Test]
		public void TestNextTimestamp()
		{
			long ts = provider.NextTimestamp();
			Assert.IsNotNull(ts, "no timestamp returned");
		}
	}
}