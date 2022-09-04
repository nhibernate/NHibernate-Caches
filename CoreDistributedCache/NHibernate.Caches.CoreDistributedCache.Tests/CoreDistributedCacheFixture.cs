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
using NHibernate.Caches.Util.JsonSerializer;
using NHibernate.Engine;

namespace NHibernate.Caches.CoreDistributedCache.Tests
{
	[TestFixture]
	public partial class CoreDistributedCacheFixture : CacheFixture
	{
		protected override bool SupportsSlidingExpiration => true;
		protected override bool SupportsClear => false;
		protected override bool SupportsDistinguishingKeysWithSameStringRepresentationAndHashcode => false;

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

		[Test]
		public void CanUseCacheKeyWithJsonSerializer()
		{
			var key = new CacheKey("keyTestJsonPut", NHibernateUtil.String, "someEntityName", Substitute.For<ISessionFactoryImplementor>());
			const string value = "valuePut";

			var props = GetDefaultProperties();
			props["cache.serializer"] = typeof(DistributedCacheJsonSerializer).AssemblyQualifiedName;
			var cache = (CacheBase) DefaultProvider.BuildCache(DefaultRegion, props);
			// Due to async version, it may already be there.
			cache.Remove(key);

			Assert.That(cache.Get(key), Is.Null, "cache returned an item we didn't add !?!");

			cache.Put(key, value);
			var item = cache.Get(key);
			Assert.That(item, Is.Not.Null, "Unable to retrieve cached item");
			Assert.That(item, Is.EqualTo(value), "didn't return the item we added");
		}

		private class DistributedCacheJsonSerializer : JsonCacheSerializer
		{
			public DistributedCacheJsonSerializer()
			{
				RegisterType(typeof(Tuple<string, object>), "tso");
			}
		}
	}
}
