using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Cache;
using StackExchange.Redis;

namespace NHibernate.Caches.StackExRedis
{
	/// <summary>
	/// A cache used to store objects into a Redis cache.
	/// </summary>
	public partial class RedisCache : ICache
	{
		private static readonly INHibernateLogger Log = NHibernateLogger.For(typeof(RedisCache));

		/// <summary>
		/// Default constructor.
		/// </summary>
		public RedisCache(string regionName, AbstractRegionStrategy regionStrategy)
		{
			RegionName = regionName;
			RegionStrategy = regionStrategy;
			Timeout = Timestamper.OneMs * (int) RegionStrategy.LockTimeout.TotalMilliseconds;
		}

		/// <inheritdoc />
		public int Timeout { get; }

		/// <inheritdoc />
		public string RegionName { get; }

		/// <summary>
		/// The region strategy used by the cache.
		/// </summary>
		public AbstractRegionStrategy RegionStrategy { get; }

		/// <inheritdoc />
		public object Get(object key)
		{
			return RegionStrategy.Get(key);
		}

		/// <inheritdoc />
		public object[] GetMany(object[] keys)
		{
			return RegionStrategy.GetMany(keys);
		}

		/// <inheritdoc />
		public void Put(object key, object value)
		{
			RegionStrategy.Put(key, value);
		}

		/// <inheritdoc />
		public void PutMany(object[] keys, object[] values)
		{
			RegionStrategy.PutMany(keys, values);
		}

		/// <inheritdoc />
		public void Remove(object key)
		{
			RegionStrategy.Remove(key);
		}

		/// <inheritdoc />
		public void RemoveMany(object[] keys)
		{
			RegionStrategy.RemoveMany(keys);
		}

		/// <inheritdoc />
		public void Clear()
		{
			RegionStrategy.Clear();
		}

		/// <inheritdoc />
		public void Destroy()
		{
			// We cannot destroy the region cache as there may be other clients using it.
		}

		/// <inheritdoc />
		public void Lock(object key)
		{
			RegionStrategy.Lock(key);
		}

		/// <inheritdoc />
		public object LockMany(object[] keys)
		{
			return RegionStrategy.LockMany(keys);
		}

		/// <inheritdoc />
		public void Unlock(object key)
		{
			RegionStrategy.Unlock(key);
		}

		/// <inheritdoc />
		public void UnlockMany(object[] keys, object lockValue)
		{
			RegionStrategy.UnlockMany(keys, (string) lockValue);
		}

		/// <inheritdoc />
		public long NextTimestamp()
		{
			return Timestamper.Next();
		}
	}
}
