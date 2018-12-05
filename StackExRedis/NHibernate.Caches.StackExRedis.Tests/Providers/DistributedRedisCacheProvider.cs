using System.Collections.Generic;
using NHibernate.Cache;
using NHibernate.Caches.StackExRedis.Tests.Caches;
using StackExchange.Redis;

namespace NHibernate.Caches.StackExRedis.Tests.Providers
{
	/// <summary>
	/// Provider for building a cache capable of operating with multiple independent Redis instances.
	/// </summary>
	public class DistributedRedisCacheProvider : RedisCacheProvider
	{
		private readonly List<IConnectionMultiplexer> _connectionMultiplexers = new List<IConnectionMultiplexer>();

		/// <inheritdoc />
		protected override void Start(string configurationString, IDictionary<string, string> properties)
		{
			foreach (var instanceConfiguration in configurationString.Split(';'))
			{
				var connectionMultiplexer = CacheConfiguration.ConnectionMultiplexerProvider.Get(instanceConfiguration);
				_connectionMultiplexers.Add(connectionMultiplexer);
			}
		}

		/// <inheritdoc />
		protected override CacheBase BuildCache(RedisCacheRegionConfiguration regionConfiguration, IDictionary<string, string> properties)
		{
			var strategies = new List<AbstractRegionStrategy>();
			foreach (var connectionMultiplexer in _connectionMultiplexers)
			{
				var regionStrategy =
					CacheConfiguration.RegionStrategyFactory.Create(connectionMultiplexer, regionConfiguration, properties);
				regionStrategy.Validate();
				strategies.Add(regionStrategy);
			}
			return new DistributedRedisCache(regionConfiguration, strategies);
		}

		/// <inheritdoc />
		public override void Stop()
		{
			foreach (var connectionMultiplexer in _connectionMultiplexers)
			{
				connectionMultiplexer.Dispose();
			}
			_connectionMultiplexers.Clear();
		}
	}
}
