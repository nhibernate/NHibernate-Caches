using System;
using System.Collections.Generic;
using NHibernate.Cache;
using NHibernate.Caches.Common.Tests;
using NHibernate.Caches.StackExchangeRedis.Tests.Providers;
using NUnit.Framework;

namespace NHibernate.Caches.StackExchangeRedis.Tests
{
	[TestFixture]
	public class DistributedRedisCacheFixture : CacheFixture
	{
		protected override bool SupportsSlidingExpiration => true;
		protected override bool SupportsLocking => true;
		protected override bool SupportsDistinguishingKeysWithSameStringRepresentationAndHashcode => false;

		protected override Func<ICacheProvider> ProviderBuilder =>
			() => new DistributedRedisCacheProvider();

		protected override void Configure(Dictionary<string, string> defaultProperties)
		{
			// Simulate Redis instances by using databases as instances
			defaultProperties.Add(RedisEnvironment.Configuration, "127.0.0.1,defaultDatabase=0;127.0.0.1,defaultDatabase=1");
			base.Configure(defaultProperties);
		}
	}
}
