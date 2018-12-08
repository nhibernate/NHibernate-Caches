using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Cache;

namespace NHibernate.Caches.StackExchangeRedis.Tests.Caches
{
	/// <summary>
	/// Operates with multiple independent Redis instances. This cache should not be used in a real environment
	/// as its purpose is just to demonstrate that <see cref="CacheBase"/> can be extended for a distributed environment.
	/// </summary>
	public partial class DistributedRedisCache : CacheBase
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
		public override int Timeout { get; }

		/// <inheritdoc />
		public override string RegionName { get; }

		/// <inheritdoc />
		public override long NextTimestamp() => Timestamper.Next();

		/// <inheritdoc />
		public override object Get(object key)
		{
			// Use a random strategy to get the value.
			// A real distributed cache should use a proper load balancing.
			var strategy = _regionStrategies[_random.Next(0, _regionStrategies.Length - 1)];
			return strategy.Get(key);
		}

		/// <inheritdoc />
		public override void Put(object key, object value)
		{
			foreach (var strategy in _regionStrategies)
			{
				strategy.Put(key, value);
			}
		}

		/// <inheritdoc />
		public override void PutMany(object[] keys, object[] values)
		{
			foreach (var strategy in _regionStrategies)
			{
				strategy.PutMany(keys, values);
			}
		}

		/// <inheritdoc />
		public override void Remove(object key)
		{
			foreach (var strategy in _regionStrategies)
			{
				strategy.Remove(key);
			}
		}

		/// <inheritdoc />
		public override void Clear()
		{
			foreach (var strategy in _regionStrategies)
			{
				strategy.Clear();
			}
		}

		/// <inheritdoc />
		public override void Destroy()
		{
		}

		/// <inheritdoc />
		public override object Lock(object key)
		{
			// A simple locking that requires all instances to obtain the lock
			// A real distributed cache should use something like the Redlock algorithm.
			var lockValues = new string[_regionStrategies.Length];
			try
			{
				for (var i = 0; i < _regionStrategies.Length; i++)
				{
					lockValues[i] = _regionStrategies[i].Lock(key);
				}

				return lockValues;
			}
			catch (CacheException)
			{
				for (var i = 0; i < _regionStrategies.Length; i++)
				{
					if (lockValues[i] == null)
					{
						continue;
					}
					_regionStrategies[i].Unlock(key, lockValues[i]);
				}
				throw;
			}
		}

		/// <inheritdoc />
		public override object LockMany(object[] keys)
		{
			// A simple locking that requires all instances to obtain the lock
			// A real distributed cache should use something like the Redlock algorithm.
			var lockValues = new string[_regionStrategies.Length];
			try
			{
				for (var i = 0; i < _regionStrategies.Length; i++)
				{
					lockValues[i] = _regionStrategies[i].LockMany(keys);
				}

				return lockValues;
			}
			catch (CacheException)
			{
				for (var i = 0; i < _regionStrategies.Length; i++)
				{
					if (lockValues[i] == null)
					{
						continue;
					}
					_regionStrategies[i].UnlockMany(keys, lockValues[i]);
				}
				throw;
			}
		}

		/// <inheritdoc />
		public override void Unlock(object key, object lockValue)
		{
			var lockValues = (string[]) lockValue;
			for (var i = 0; i < _regionStrategies.Length; i++)
			{
				_regionStrategies[i].Unlock(key, lockValues[i]);
			}
		}

		/// <inheritdoc />
		public override void UnlockMany(object[] keys, object lockValue)
		{
			var lockValues = (string[]) lockValue;
			for (var i = 0; i < _regionStrategies.Length; i++)
			{
				_regionStrategies[i].UnlockMany(keys, lockValues[i]);
			}
		}
	}
}
