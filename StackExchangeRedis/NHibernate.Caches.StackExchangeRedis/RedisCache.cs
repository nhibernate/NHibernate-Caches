using NHibernate.Cache;

namespace NHibernate.Caches.StackExchangeRedis
{
	/// <summary>
	/// A cache used to store objects into a Redis cache.
	/// </summary>
	public partial class RedisCache : CacheBase
	{
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
		public override int Timeout { get; }

		/// <inheritdoc />
		public override string RegionName { get; }

		/// <summary>
		/// The region strategy used by the cache.
		/// </summary>
		public AbstractRegionStrategy RegionStrategy { get; }

		/// <inheritdoc />
		public override object Get(object key)
		{
			return RegionStrategy.Get(key);
		}

		/// <inheritdoc />
		public override object[] GetMany(object[] keys)
		{
			return RegionStrategy.GetMany(keys);
		}

		/// <inheritdoc />
		public override void Put(object key, object value)
		{
			RegionStrategy.Put(key, value);
		}

		/// <inheritdoc />
		public override void PutMany(object[] keys, object[] values)
		{
			RegionStrategy.PutMany(keys, values);
		}

		/// <inheritdoc />
		public override void Remove(object key)
		{
			RegionStrategy.Remove(key);
		}

		/// <inheritdoc />
		public override void Clear()
		{
			RegionStrategy.Clear();
		}

		/// <inheritdoc />
		public override void Destroy()
		{
			// No resources to clean-up. 
		}

		/// <inheritdoc />
		public override object Lock(object key)
		{
			return RegionStrategy.Lock(key);
		}

		/// <inheritdoc />
		public override object LockMany(object[] keys)
		{
			return RegionStrategy.LockMany(keys);
		}

		/// <inheritdoc />
		public override void Unlock(object key, object lockValue)
		{
			RegionStrategy.Unlock(key, (string)lockValue);
		}

		/// <inheritdoc />
		public override void UnlockMany(object[] keys, object lockValue)
		{
			RegionStrategy.UnlockMany(keys, (string) lockValue);
		}

		/// <inheritdoc />
		public override long NextTimestamp()
		{
			return Timestamper.Next();
		}
	}
}
