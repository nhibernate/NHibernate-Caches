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
using System.Collections.Generic;
using System.Reflection;
using log4net.Config;
using NHibernate.Cache;
using NHibernate.Caches.Common.Tests;
using NUnit.Framework;

namespace NHibernate.Caches.SysCache2.Tests
{
	[TestFixture]
	public class SysCacheProviderFixture : CacheProviderFixture
	{
		protected override void Configure(Dictionary<string, string> defaultProperties)
		{
			XmlConfigurator.Configure();
			base.Configure(defaultProperties);
			defaultProperties.Add("priority", 2.ToString());
		}

		protected override Func<ICacheProvider> ProviderBuilder =>
			() => new SysCacheProvider();

		[Test]
		public void TestBuildCacheFromConfig()
		{
			var cache = DefaultProvider.BuildCache("foo", null);
			Assert.That(cache, Is.Not.Null, "pre-configured cache not found");
		}

		private static readonly FieldInfo RelativeExpirationField =
			typeof(SysCacheRegionBase).GetField("_relativeExpiration", BindingFlags.NonPublic | BindingFlags.Instance);

		private static readonly FieldInfo UseSlidingExpirationField =
			typeof(SysCacheRegionBase).GetField("_useSlidingExpiration", BindingFlags.NonPublic | BindingFlags.Instance);

		[Test]
		public void TestExpiration()
		{
			var cache = DefaultProvider.BuildCache("foo", null) as SysCacheRegion;
			Assert.That(cache, Is.Not.Null, "pre-configured foo cache not found");
			Assert.That(RelativeExpirationField.GetValue(cache), Is.EqualTo(TimeSpan.FromSeconds(500)), "Unexpected expiration value for foo region");

			// In the case of SysCache2, due to the SQL notification callbacks which may be set up for each configured region,
			// the build provider handles a global region cache and guarantees uniqueness of yielded cache region per region name
			// for the whole application.
			cache = DefaultProvider.BuildCache("noExplicitExpiration1", null) as SysCacheRegion;
			Assert.That(RelativeExpirationField.GetValue(cache), Is.EqualTo(TimeSpan.FromSeconds(300)),
				"Unexpected expiration value for noExplicitExpiration1 region");
			Assert.That(UseSlidingExpirationField.GetValue(cache), Is.True, "Unexpected sliding value for noExplicitExpiration1 region");

			cache = DefaultProvider
				.BuildCache("noExplicitExpiration2", new Dictionary<string, string> { { "expiration", "100" } }) as SysCacheRegion;
			Assert.That(RelativeExpirationField.GetValue(cache), Is.EqualTo(TimeSpan.FromSeconds(100)),
				"Unexpected expiration value for noExplicitExpiration2 region with default expiration");

			cache = DefaultProvider
				.BuildCache("noExplicitExpiration3", new Dictionary<string, string> { { Cfg.Environment.CacheDefaultExpiration, "50" } }) as SysCacheRegion;
			Assert.That(RelativeExpirationField.GetValue(cache), Is.EqualTo(TimeSpan.FromSeconds(50)),
				"Unexpected expiration value for noExplicitExpiration3 region with cache.default_expiration");
		}
	}
}
