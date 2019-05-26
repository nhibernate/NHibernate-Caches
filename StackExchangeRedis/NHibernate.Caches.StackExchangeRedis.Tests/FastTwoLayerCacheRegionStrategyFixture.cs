using NUnit.Framework;

namespace NHibernate.Caches.StackExchangeRedis.Tests
{
	[TestFixture]
	public class FastTwoLayerCacheRegionStrategyFixture : DistributedLocalCacheFixture<FastTwoLayerCacheRegionStrategy>
	{
		protected override bool SupportsClear => false;
	}
}
