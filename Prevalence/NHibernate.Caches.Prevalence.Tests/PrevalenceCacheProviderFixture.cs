#region License

//
//  PrevalenceCache - A cache provider for NHibernate using Bamboo.Prevalence.
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
using System.IO;
using log4net.Config;
using NHibernate.Cache;
using NHibernate.Caches.Common.Tests;
using NUnit.Framework;

namespace NHibernate.Caches.Prevalence.Tests
{
	[TestFixture]
	public class PrevalenceCacheProviderFixture : CacheProviderFixture
	{
		private string _testDir;

		protected override void Configure(Dictionary<string, string> defaultProperties)
		{
			XmlConfigurator.Configure();
			base.Configure(defaultProperties);
			_testDir = Path.Combine(Path.GetDirectoryName(typeof(PrevalenceCacheFixture).Assembly.Location), "CacheStorage");
			defaultProperties.Add("prevalenceBase",
				Path.Combine(Path.GetDirectoryName(typeof(PrevalenceCacheFixture).Assembly.Location), "CacheStorage"));
		}

		protected override Func<ICacheProvider> ProviderBuilder =>
			() => new PrevalenceCacheProvider();

		[SetUp]
		public void SetUp()
		{
			if (Directory.Exists(_testDir))
			{
				Directory.Delete(_testDir, true);
			}
		}

		[Ignore("Must supply prevalenceBase or have write access right in current execution directory")]
		public override void TestBuildCacheRegionNoProperties()
		{
		}
	}
}
