using System;

namespace NHibernate.Caches.StackExchangeRedis.Tests
{
	public abstract class DistributedLocalCacheFixture<TRegionStrategy> : RedisCacheFixture<TRegionStrategy>
	{
		protected override bool SupportsDistributedCache => true;
		protected override TimeSpan DistributedSynhronizationTime => TimeSpan.FromMilliseconds(100);
		protected override int WriteStressTestTaskCount => base.WriteStressTestTaskCount / 4;
	}
}
