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
using Microsoft.Extensions.Caching.Distributed;
using NHibernate.Cache;
using NHibernate.Caches.Common.Tests;
using NUnit.Framework;
using NSubstitute;

namespace NHibernate.Caches.CoreDistributedCache.Tests
{
	[TestFixture]
	public partial class CoreDistributedCacheFixture : CacheFixture
	{
		protected override bool SupportsSlidingExpiration => true;
		protected override bool SupportsClear => false;

		protected override Func<ICacheProvider> ProviderBuilder =>
			() => new CoreDistributedCacheProvider();

		[Test]
		public void MaxKeySize()
		{
			var distribCache = Substitute.For<IDistributedCache>();
			const int maxLength = 20;
			var cache = new CoreDistributedCache(distribCache, new CacheConstraints { MaxKeySize = maxLength }, "foo",
				new Dictionary<string, string>());
			cache.Put(new string('k', maxLength * 2), "test");
			distribCache.Received().Set(Arg.Is<string>(k => k.Length <= maxLength), Arg.Any<byte[]>(),
				Arg.Any<DistributedCacheEntryOptions>());
		}

		[Test]
		public void KeySanitizer()
		{
			var distribCache = Substitute.For<IDistributedCache>();
			Func<string, string> keySanitizer = s => s.Replace('a', 'b');
			var cache = new CoreDistributedCache(distribCache, new CacheConstraints { KeySanitizer = keySanitizer }, "foo",
				new Dictionary<string, string>());
			cache.Put("-abc-", "test");
			distribCache.Received().Set(Arg.Is<string>(k => k.Contains(keySanitizer("-abc-"))), Arg.Any<byte[]>(),
				Arg.Any<DistributedCacheEntryOptions>());
		}
	}
}
