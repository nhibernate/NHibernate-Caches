using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Cache;

namespace NHibernate.Caches.StackExRedis.Tests.Caches
{
	/// <summary>
	/// Operates with multiple independent Redis instances.
	/// </summary>
	public partial class DistributedRedisCache : ICache
	{
		private readonly AbstractRegionStrategy[] _regionStrategies;
		private readonly Random _random = new Random();

		public DistributedRedisCache(RedisCacheRegionConfiguration configuration, IEnumerable<AbstractRegionStrategy> regionStrategies)
		{
			_regionStrategies = regionStrategies.ToArray();
			RegionName = configuration.RegionName;
			Timeout = Timestamper.OneMs * (int) configuration.LockConfiguration.KeyTimeout.TotalMilliseconds;
		}

		/// <summary>
		/// The region strategies used by the cache.
		/// </summary>
		public IEnumerable<AbstractRegionStrategy> RegionStrategies => _regionStrategies;

		/// <inheritdoc />
		public int Timeout { get; }

		/// <inheritdoc />
		public string RegionName { get; }

		/// <inheritdoc />
		public long NextTimestamp() => Timestamper.Next();

		/// <inheritdoc />
		public object Get(object key)
		{
			// Use a random strategy to get the value.
			// A real distributed cache should use a proper load balancing.
			var strategy = _regionStrategies[_random.Next(0, _regionStrategies.Length - 1)];
			return strategy.Get(key);
		}

		/// <inheritdoc />
		public void Put(object key, object value)
		{
			foreach (var strategy in _regionStrategies)
			{
				strategy.Put(key, value);
			}
		}

		/// <inheritdoc />
		public void Remove(object key)
		{
			foreach (var strategy in _regionStrategies)
			{
				strategy.Remove(key);
			}
		}

		/// <inheritdoc />
		public void Clear()
		{
			foreach (var strategy in _regionStrategies)
			{
				strategy.Clear();
			}
		}

		/// <inheritdoc />
		public void Destroy()
		{
		}

		/// <inheritdoc />
		public void Lock(object key)
		{
			// A simple locking that requires all instances to obtain the lock
			// A real distributed cache should use something like the Redlock algorithm.
			try
			{
				foreach (var strategy in _regionStrategies)
				{
					strategy.Lock(key);
				}

			}
			catch (CacheException)
			{
				foreach (var strategy in _regionStrategies)
				{
					strategy.Unlock(key);
				}
				throw;
			}
		}

		/// <inheritdoc />
		public void Unlock(object key)
		{
			foreach (var strategy in _regionStrategies)
			{
				strategy.Unlock(key);
			}
		}

	}
}
