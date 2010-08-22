using System.Collections.Generic;
using log4net.Config;
using NHibernate.Cache;
using NUnit.Framework;

namespace NHibernate.Caches.EnyimMemcached.Tests
{
	public class MemCacheProviderFixture
	{
		private Dictionary<string, string> props;
		private ICacheProvider provider;

		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			XmlConfigurator.Configure();
			props = new Dictionary<string, string>();
			provider = new MemCacheProvider();
			provider.Start(props);
		}

		[TestFixtureTearDown]
		public void Stop()
		{
			provider.Stop();
		}

		[Test]
		public void TestBuildCacheFromConfig()
		{
			ICache cache = provider.BuildCache("foo", null);
			Assert.IsNotNull(cache, "pre-configured cache not found");
		}

		[Test]
		public void TestBuildCacheNullNull()
		{
			ICache cache = provider.BuildCache(null, null);
			Assert.IsNotNull(cache, "no cache returned");
		}

		[Test]
		public void TestBuildCacheStringICollection()
		{
			ICache cache = provider.BuildCache("another_region", props);
			Assert.IsNotNull(cache, "no cache returned");
		}

		[Test]
		public void TestBuildCacheStringNull()
		{
			ICache cache = provider.BuildCache("a_region", null);
			Assert.IsNotNull(cache, "no cache returned");
		}

		[Test]
		public void TestNextTimestamp()
		{
			long ts = provider.NextTimestamp();
			Assert.IsNotNull(ts, "no timestamp returned");
		}
	}
}