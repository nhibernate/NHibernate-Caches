using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Cache;

namespace NHibernate.Caches.Prevalence
{
	/// <summary>
	/// Summary description for PrevalenceCache.
	/// </summary>
	public class PrevalenceCache : ICache
	{
		private const string CacheKeyPrefix = "NHibernate-Cache:";
		private static readonly IInternalLogger log = LoggerProvider.LoggerFor((typeof(PrevalenceCache)));
		private readonly string region;
		private readonly CacheSystem system;

		/// <summary>
		/// default constructor
		/// </summary>
		public PrevalenceCache() : this("nhibernate", null) {}

		/// <summary>
		/// constructor with no properties
		/// </summary>
		/// <param name="region"></param>
		public PrevalenceCache(string region) : this(region, null) {}

		/// <summary>
		/// full constructor
		/// </summary>
		/// <param name="region"></param>
		/// <param name="system">the Prevalance container class</param>
		public PrevalenceCache(string region, CacheSystem system)
		{
			this.region = region;
			this.system = system ?? new CacheSystem();
		}

		#region ICache Members

		/// <summary></summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public object Get(object key)
		{
			if (key == null)
			{
				return null;
			}
			string cacheKey = GetCacheKey(key);
			if (log.IsDebugEnabled)
			{
				log.Debug(String.Format("Fetching object '{0}' from the cache.", cacheKey));
			}

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

		/// <summary></summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void Put(object key, object value)
		{
			if (key == null)
			{
				if (log.IsErrorEnabled)
				{
					log.Error("null key passed to 'Put'");
				}
				throw new ArgumentNullException("key", "null key not allowed");
			}
			if (value == null)
			{
				if (log.IsErrorEnabled)
				{
					log.Error("null value passed to 'Put'");
				}
				throw new ArgumentNullException("value", "null value not allowed");
			}
			string cacheKey = GetCacheKey(key);
			if (log.IsDebugEnabled)
			{
				log.Debug(String.Format("setting value {1} for key {0}", cacheKey, value));
			}
			system.Add(cacheKey, new DictionaryEntry(key, value));
		}

		/// <summary></summary>
		/// <param name="key"></param>
		public void Remove(object key)
		{
			if (key == null)
			{
				if (log.IsErrorEnabled)
				{
					log.Error("null key passed to 'Remove'");
				}
				throw new ArgumentNullException("key");
			}
			string cacheKey = GetCacheKey(key);
			if (log.IsDebugEnabled)
			{
				log.Debug("removing item with key: " + cacheKey);
			}
			system.Remove(cacheKey);
		}

		/// <summary></summary>
		public void Clear()
		{
			if (log.IsInfoEnabled)
			{
				log.Info("clearing all objects from system");
			}
			system.Clear();
		}

		/// <summary></summary>
		public void Destroy()
		{
			if (log.IsInfoEnabled)
			{
				log.Info("'Destroy' was called");
			}
			Clear();
		}

		/// <summary></summary>
		/// <param name="key"></param>
		public void Lock(object key)
		{
			// Do nothing
		}

		/// <summary></summary>
		/// <param name="key"></param>
		public void Unlock(object key)
		{
			// Do nothing
		}

		/// <summary></summary>
		public long NextTimestamp()
		{
			return Timestamper.Next();
		}

		/// <summary></summary>
		public int Timeout
		{
			get { return Timestamper.OneMs * 60000; } // 60 seconds
		}

		public string RegionName
		{
			get { return region; }
		}

		#endregion

		#region ICache async methods delegated to sync implementation

		public Task<object> GetAsync(object key, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			return Task.FromResult(Get(key));
		}

		public Task PutAsync(object key, object value, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled(cancellationToken);
			}
			Put(key, value);
			return Task.CompletedTask;
		}

		public Task RemoveAsync(object key, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled(cancellationToken);
			}
			Remove(key);
			return Task.CompletedTask;
		}

		public Task ClearAsync(CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled(cancellationToken);
			}
			Clear();
			return Task.CompletedTask;
		}

		public Task LockAsync(object key, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled(cancellationToken);
			}
			Lock(key);
			return Task.CompletedTask;
		}

		public Task UnlockAsync(object key, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled(cancellationToken);
			}
			Unlock(key);
			return Task.CompletedTask;
		}

		#endregion

		private string GetCacheKey(object key)
		{
			return String.Concat(CacheKeyPrefix, region, ":", key.ToString(), "@", key.GetHashCode());
		}
	}
}