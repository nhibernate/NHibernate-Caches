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
using log4net.Config;
using NHibernate.Cache;
using NHibernate.Caches.Common.Tests;
using NUnit.Framework;

namespace NHibernate.Caches.RtMemoryCache.Tests
{
	[TestFixture]
	public class RtMemoryCacheProviderFixture : CacheProviderFixture
	{
		protected override Func<ICacheProvider> ProviderBuilder =>
			() => new RtMemoryCacheProvider();

		[Test]
		public void TestBuildCacheFromConfig()
		{
			ICache cache = DefaultProvider.BuildCache("foo", null);
			Assert.That(cache, Is.Not.Null, "pre-configured cache not found");
		}

		[Test]
		public void TestExpiration()
		{
			var cache = DefaultProvider.BuildCache("foo", null) as RtMemoryCache;
			Assert.That(cache, Is.Not.Null, "pre-configured foo cache not found");
			Assert.That(cache.Expiration, Is.EqualTo(TimeSpan.FromSeconds(500)), "Unexpected expiration value for foo region");

			cache = DefaultProvider.BuildCache("noExplicitExpiration", null) as RtMemoryCache;
			Assert.That(cache.Expiration, Is.EqualTo(TimeSpan.FromSeconds(300)),
				"Unexpected expiration value for noExplicitExpiration region");
			Assert.That(cache.UseSlidingExpiration, Is.True, "Unexpected sliding value for noExplicitExpiration region");

			cache = DefaultProvider
				.BuildCache("noExplicitExpiration", new Dictionary<string, string> { { "expiration", "100" } }) as RtMemoryCache;
			Assert.That(cache.Expiration, Is.EqualTo(TimeSpan.FromSeconds(100)),
				"Unexpected expiration value for noExplicitExpiration region with default expiration");

			cache = DefaultProvider
				.BuildCache("noExplicitExpiration", new Dictionary<string, string> { { Cfg.Environment.CacheDefaultExpiration, "50" } }) as RtMemoryCache;
			Assert.That(cache.Expiration, Is.EqualTo(TimeSpan.FromSeconds(50)),
				"Unexpected expiration value for noExplicitExpiration region with cache.default_expiration");
		}
	}
}
