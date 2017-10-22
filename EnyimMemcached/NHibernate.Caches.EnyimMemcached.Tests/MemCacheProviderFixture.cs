using System;
using System.Collections.Generic;
using System.Reflection;
using log4net.Config;
using NHibernate.Cache;
using NHibernate.Caches.Common.Tests;
using NUnit.Framework;

namespace NHibernate.Caches.EnyimMemcached.Tests
{
	[TestFixture]
	public class MemCacheProviderFixture : CacheProviderFixture
	{
		protected override void Configure(Dictionary<string, string> defaultProperties)
		{
			XmlConfigurator.Configure();
			base.Configure(defaultProperties);
		}

		protected override Func<ICacheProvider> ProviderBuilder =>
			() => new MemCacheProvider();

		[Test]
		public void TestBuildCacheFromConfig()
		{
			var cache = DefaultProvider.BuildCache("foo", null);
			Assert.That(cache, Is.Not.Null, "pre-configured cache not found");
		}

		protected override void OnOneTimeTearDown()
		{
			var clientField = typeof(MemCacheProvider).GetField("clientInstance", BindingFlags.NonPublic | BindingFlags.Static);
			Assert.That(clientField, Is.Not.Null, "Unable to locate client field");
			Assert.That(clientField.GetValue(null), Is.Null, "Provider was not stopped");
		}
	}
}
