#region License

//
//  SharedCache - A cache provider for NHibernate using indeXus.Net Shared Cache
//  (http://www.sharedcache.com/).
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

namespace NHibernate.Caches.SharedCache.Tests
{
	[TestFixture]
	public class SharedCacheFixture : CacheFixture
	{
		protected override bool SupportsDefaultExpiration => false;

		protected override void Configure(Dictionary<string, string> defaultProperties)
		{
			XmlConfigurator.Configure();
			base.Configure(defaultProperties);
		}

		protected override Func<ICacheProvider> ProviderBuilder =>
			() => new SharedCacheProvider();

		[Test]
		public void TestDefaultConstructor()
		{
			Assert.That(() => new SharedCacheClient(), Throws.Nothing);
		}

		[Test]
		public void TestNoPropertiesConstructor()
		{
			Assert.That(() => new SharedCacheClient("TestNoPropertiesConstructor"), Throws.Nothing);
		}
	}
}
