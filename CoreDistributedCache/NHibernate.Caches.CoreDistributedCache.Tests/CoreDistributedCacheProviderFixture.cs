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
using NHibernate.Cache;
using NHibernate.Caches.Common.Tests;
using NUnit.Framework;

namespace NHibernate.Caches.CoreDistributedCache.Tests
{
	[TestFixture]
	public class CoreDistributedCacheProviderFixture : CacheProviderFixture
	{
		protected override Func<ICacheProvider> ProviderBuilder =>
			() => new CoreDistributedCacheProvider();

		[Test]
		public void TestBuildCacheFromConfig()
		{
			var cache = DefaultProvider.BuildCache("foo", null);
			Assert.That(cache, Is.Not.Null, "pre-configured cache not found");
		}

		[Test]
		public void TestExpiration()
		{
			var cache = DefaultProvider.BuildCache("foo", null) as CoreDistributedCache;
			Assert.That(cache, Is.Not.Null, "pre-configured foo cache not found");
			Assert.That(cache.Expiration, Is.EqualTo(TimeSpan.FromSeconds(500)), "Unexpected expiration value for foo region");

			cache = (CoreDistributedCache) DefaultProvider.BuildCache("noExplicitExpiration", null);
			Assert.That(cache.Expiration, Is.EqualTo(TimeSpan.FromSeconds(300)),
				"Unexpected expiration value for noExplicitExpiration region");
			Assert.That(cache.UseSlidingExpiration, Is.True, "Unexpected sliding value for noExplicitExpiration region");

			cache = (CoreDistributedCache) DefaultProvider
				.BuildCache("noExplicitExpiration", new Dictionary<string, string> { { "expiration", "100" } });
			Assert.That(cache.Expiration, Is.EqualTo(TimeSpan.FromSeconds(100)),
				"Unexpected expiration value for noExplicitExpiration region with default expiration");

			cache = (CoreDistributedCache) DefaultProvider
				.BuildCache("noExplicitExpiration",
					new Dictionary<string, string> { { Cfg.Environment.CacheDefaultExpiration, "50" } });
			Assert.That(cache.Expiration, Is.EqualTo(TimeSpan.FromSeconds(50)),
				"Unexpected expiration value for noExplicitExpiration region with cache.default_expiration");
		}
	}
}
