using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Cache;

namespace NHibernate.Caches.Prevalence
{
	// 6.0 TODO: replace that class by its base
	/// <summary>
	/// Pluggable cache implementation using Bamboo Prevalence.
	/// </summary>
	public class PrevalenceCache : PrevalenceCacheBase,
#pragma warning disable 618
		ICache
#pragma warning restore 618
	{
		/// <summary>
		/// Default constructor.
		/// </summary>
		public PrevalenceCache()
		{
		}

		/// <summary>
		/// Constructor with no properties.
		/// </summary>
		/// <param name="region">The region of the cache.</param>
		public PrevalenceCache(string region)
			: base(region)
		{
		}

		/// <summary>
		/// Full constructor.
		/// </summary>
		/// <param name="region">The cache region name.</param>
		/// <param name="system">The Prevalance container class.</param>
		public PrevalenceCache(string region, CacheSystem system)
			: base(region, system)
		{
		}

		/// <inheritdoc />
		public new Task<object> GetAsync(object key, CancellationToken cancellationToken)
			=> base.GetAsync(key, cancellationToken);

		/// <inheritdoc />
		public new Task PutAsync(object key, object value, CancellationToken cancellationToken)
			=> base.PutAsync(key, value, cancellationToken);

		/// <inheritdoc />
		public new Task RemoveAsync(object key, CancellationToken cancellationToken)
			=> base.RemoveAsync(key, cancellationToken);

		/// <inheritdoc />
		public new Task ClearAsync(CancellationToken cancellationToken)
			=> base.ClearAsync(cancellationToken);

		/// <inheritdoc />
		public new Task LockAsync(object key, CancellationToken cancellationToken)
			=> base.LockAsync(key, cancellationToken);

		/// <inheritdoc />
		public Task UnlockAsync(object key, CancellationToken cancellationToken)
			=> base.UnlockAsync(key, null, cancellationToken);

		/// <inheritdoc />
		public new string RegionName => base.RegionName;

		/// <inheritdoc />
		public new object Get(object key)
			=> base.Get(key);

		/// <inheritdoc />
		public new void Put(object key, object value)
			=> base.Put(key, value);

		/// <inheritdoc />
		public new void Remove(object key)
			=> base.Remove(key);

		/// <inheritdoc />
		public new void Clear()
			=> base.Clear();

		/// <inheritdoc />
		public new void Destroy()
			=> base.Destroy();

		/// <inheritdoc />
		public new void Lock(object key)
			=> base.Lock(key);

		/// <inheritdoc />
		public void Unlock(object key)
			=> base.Unlock(key, null);

		/// <inheritdoc />
		public new long NextTimestamp()
			=> base.NextTimestamp();

		/// <inheritdoc />
		public new int Timeout => base.Timeout;
	}

	/// <summary>
	/// Pluggable cache implementation using Bamboo Prevalence.
	/// </summary>
	public abstract class PrevalenceCacheBase : CacheBase
	{
		private const string CacheKeyPrefix = "NHibernate-Cache:";
		private static readonly INHibernateLogger log = NHibernateLogger.For(typeof(PrevalenceCache));
		private readonly string region;
		private readonly CacheSystem system;

		/// <summary>
		/// Default constructor.
		/// </summary>
		public PrevalenceCacheBase() : this("nhibernate", null) {}

		/// <summary>
		/// Constructor with no properties.
		/// </summary>
		/// <param name="region">The cache region name.</param>
		public PrevalenceCacheBase(string region) : this(region, null) {}

		/// <summary>
		/// Full constructor.
		/// </summary>
		/// <param name="region">The cache region name.</param>
		/// <param name="system">The Prevalance container class.</param>
		public PrevalenceCacheBase(string region, CacheSystem system)
		{
			this.region = region;
			this.system = system ?? new CacheSystem();
		}

		#region CacheBase Members

		/// <inheritdoc />
		public override object Get(object key)
		{
			if (key == null)
			{
				return null;
			}
			string cacheKey = GetCacheKey(key);
			log.Debug("Fetching object '{0}' from the cache.", cacheKey);

			object obj = system.Get(cacheKey);
			if (obj == null)
			{
				return null;
			}

			var de = (DictionaryEntry) obj;

			if (key.Equals(de.Key))
			{
				return de.Value;
			}
			else
			{
				return null;
			}
		}

		/// <inheritdoc />
		public override void Put(object key, object value)
		{
			if (key == null)
			{
				log.Error("null key passed to 'Put'");
				throw new ArgumentNullException("key", "null key not allowed");
			}
			if (value == null)
			{
				log.Error("null value passed to 'Put'");
				throw new ArgumentNullException("value", "null value not allowed");
			}
			string cacheKey = GetCacheKey(key);
			log.Debug("setting value {1} for key {0}", cacheKey, value);
			system.Add(cacheKey, new DictionaryEntry(key, value));
		}

		/// <inheritdoc />
		public override void Remove(object key)
		{
			if (key == null)
			{
				log.Error("null key passed to 'Remove'");
				throw new ArgumentNullException("key");
			}
			string cacheKey = GetCacheKey(key);
			log.Debug("removing item with key: {0}", cacheKey);
			system.Remove(cacheKey);
		}

		/// <inheritdoc />
		public override void Clear()
		{
			log.Info("clearing all objects from system");
			system.Clear();
		}

		/// <inheritdoc />
		public override void Destroy()
		{
			log.Info("'Destroy' was called");
			Clear();
		}

		/// <inheritdoc />
		public override object Lock(object key)
		{
			// Do nothing
			return null;
		}

		/// <inheritdoc />
		public override void Unlock(object key, object lockValue)
		{
			// Do nothing
		}

		/// <inheritdoc />
		public override long NextTimestamp()
		{
			return Timestamper.Next();
		}

		/// <inheritdoc />
		public override int Timeout
		{
			get { return Timestamper.OneMs * 60000; } // 60 seconds
		}

		/// <inheritdoc />
		public override string RegionName
		{
			get { return region; }
		}

		#endregion

		private string GetCacheKey(object key)
		{
			return String.Concat(CacheKeyPrefix, region, ":", key.ToString(), "@", key.GetHashCode());
		}
	}
}
