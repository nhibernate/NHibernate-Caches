using NUnit.Framework;

namespace NHibernate.Caches.Common.Tests
{
	[TestFixture]
	public abstract class CacheProviderFixture : Fixture
	{
		[Test]
		public void TestBuildCacheNoRegionNoProperties()
		{
			var cache = DefaultProvider.BuildCache(null, null);
			Assert.That(cache, Is.Not.Null, "no cache returned");
		}

		[Test]
		public void TestBuildCacheNoRegionWithProperties()
		{
			// Using same "region" than another test, need to get another provider for avoiding it
			// to yield a previously built cache if the underlying provider does some instance caching.
			var cache = GetNewProvider().BuildCache(null, GetDefaultProperties());
			Assert.That(cache, Is.Not.Null, "no cache returned");
		}

		[Test]
		public void TestBuildCacheRegionWithProperties()
		{
			var cache = DefaultProvider.BuildCache("TestBuildCacheWithProperties", GetDefaultProperties());
			Assert.That(cache, Is.Not.Null, "no cache returned");
		}

		[Test]
		public virtual void TestBuildCacheRegionNoProperties()
		{
			var cache = DefaultProvider.BuildCache("TestBuildCacheRegionNoProperties", null);
			Assert.That(cache, Is.Not.Null, "no cache returned");
		}

		[Test]
		public void TestNextTimestamp()
		{
			long ts = DefaultProvider.NextTimestamp();
			Assert.That(ts, Is.Not.Zero, "no timestamp returned");
		}
	}
}
