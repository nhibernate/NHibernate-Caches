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
using System.Web.Caching;
using NUnit.Framework;

namespace NHibernate.Caches.SysCache2.Tests
{
	[TestFixture]
	public class SysCacheSectionFixture
	{
		[Test]
		public void TestGetConfig()
		{
			var section = SysCacheSection.GetSection();
			Assert.That(section.CacheRegions, Has.Count.EqualTo(4), "Unexpected region count");
			var region = section.CacheRegions[0];
			Assert.That(region.Name, Is.EqualTo("foo"), "Unexpected region name");
			Assert.That(region.Priority, Is.EqualTo(CacheItemPriority.AboveNormal), "Unexpected region priority");
			Assert.That(region.RelativeExpiration, Is.EqualTo(TimeSpan.FromSeconds(500)), "Unexpected region relative expiration");
			Assert.That(region.UseSlidingExpiration, Is.EqualTo(true), "Unexpected region use sliding expiration");
		}
	}
}
