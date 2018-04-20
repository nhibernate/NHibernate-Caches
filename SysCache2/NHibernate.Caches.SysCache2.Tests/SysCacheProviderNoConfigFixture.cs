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
using NHibernate.Cache;
using NHibernate.Caches.Common.Tests;
using NUnit.Framework;

namespace NHibernate.Caches.SysCache2.Tests
{
	[TestFixture]
	public class SysCacheProviderNoConfigurationFixture
	{
		private const string DefaultExpirationSetting = "expiration";

		private ICacheProvider _defaultProvider;

		private readonly Dictionary<string, string> _defaultProperties = new Dictionary<string, string>();

		private readonly List<ICacheProvider> _providers = new List<ICacheProvider>();

		[OneTimeSetUp]
		public void FixtureSetup()
		{
			Configure(_defaultProperties);
			_defaultProvider = GetNewProvider();
		}

		private void Configure(Dictionary<string, string> defaultProperties)
		{
			defaultProperties.Add(DefaultExpirationSetting, 120.ToString());
		}

		private ICacheProvider GetNewProvider()
		{
			var provider = new SysCacheProvider();
			_providers.Add(provider);
			provider.Start(new Dictionary<string, string>(_defaultProperties));
			return provider;
		}

		[Test]
		public void TestNullReferenceException() =>
			Assert.DoesNotThrow(
				() => _defaultProvider.BuildCache("SomeRegion", null),
				"Not defining 'cacheRegion's should not result in NullReferenceException being thrown.");
	}
}
