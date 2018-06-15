using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
		private readonly List<ConnectionMultiplexer> _connectionMultiplexers = new List<ConnectionMultiplexer>();

		/// <inheritdoc />
		protected override void Start(string configurationString, IDictionary<string, string> properties, TextWriter textWriter)
		{
			foreach (var instanceConfiguration in configurationString.Split(';'))
			{
				var configuration = ConfigurationOptions.Parse(instanceConfiguration);
				var connectionMultiplexer = ConnectionMultiplexer.Connect(configuration, textWriter);
				connectionMultiplexer.PreserveAsyncOrder = false; // Recommended setting
				_connectionMultiplexers.Add(connectionMultiplexer);
			}
		}

		/// <inheritdoc />
		protected override ICache BuildCache(RedisCacheConfiguration defaultConfiguration, RedisCacheRegionConfiguration regionConfiguration,
			IDictionary<string, string> properties)
		{
			var strategies = new List<AbstractRegionStrategy>();
			foreach (var connectionMultiplexer in _connectionMultiplexers)
			{
				var regionStrategy =
					defaultConfiguration.RegionStrategyFactory.Create(connectionMultiplexer, regionConfiguration, properties);
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
