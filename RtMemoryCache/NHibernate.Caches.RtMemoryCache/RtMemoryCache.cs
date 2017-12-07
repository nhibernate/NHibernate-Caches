#region License

//
//  RtMemoryCache - A cache provider for NHibernate using System.Runtime.Caching.MemoryCache.
//
//  This library is free software; you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation; either
//  version 2.1 of the License, or (at your option) any later version.
//
//  This library is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//  Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//

#endregion

using System;
using System.Collections;
using System.Runtime.Caching;
using NHibernate.Cache;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Util;

namespace NHibernate.Caches.RtMemoryCache
{
	/// <summary>
	/// Pluggable cache implementation using the System.Runtime.Caching classes
	/// </summary>
	public class RtMemoryCache : ICache
	{
		private static readonly IInternalLogger Log = LoggerProvider.LoggerFor(typeof(RtMemoryCache));
		private string _regionPrefix;
		private readonly ObjectCache _cache;

		// The name of the cache key used to clear the cache. All cached items depend on this key.
		private readonly string _rootCacheKey;
		private bool _rootCacheKeyStored;
		private static readonly TimeSpan DefaultExpiration = TimeSpan.FromSeconds(300);
		private const bool _defaultUseSlidingExpiration = false;
		private static readonly string DefaultRegionPrefix = string.Empty;
		private const string _cacheKeyPrefix = "NHibernate-Cache:";

		/// <summary>
		/// default constructor
		/// </summary>
		public RtMemoryCache()
			: this("nhibernate", null)
		{
		}

		/// <summary>
		/// constructor with no properties
		/// </summary>
		/// <param name="region"></param>
		public RtMemoryCache(string region)
			: this(region, null)
		{
		}

		/// <summary>
		/// full constructor
		/// </summary>
		/// <param name="region"></param>
		/// <param name="properties">cache configuration properties</param>
		/// <remarks>
		/// There are two (2) configurable parameters:
		/// <ul>
		///		<li>expiration = number of seconds to wait before expiring each item</li>
		///		<li>priority = a numeric cost of expiring each item, where 1 is a low cost, 5 is the highest, and 3 is normal. Only values 1 through 5 are valid.</li>
		/// </ul>
		/// All parameters are optional. The defaults are an expiration of 300 seconds and the default priority of 3.
		/// </remarks>
		/// <exception cref="IndexOutOfRangeException">The "priority" property is not between 1 and 5</exception>
		/// <exception cref="ArgumentException">The "expiration" property could not be parsed.</exception>
		public RtMemoryCache(string region, IDictionary<string, string> properties)
		{
			Region = region;
			_cache = MemoryCache.Default;
			Configure(properties);

			_rootCacheKey = GenerateRootCacheKey();
			StoreRootCacheKey();
		}

		public string Region { get; }

		public TimeSpan Expiration { get; private set; }

		public bool UseSlidingExpiration { get; private set; }

		// Since v5.1
		[Obsolete("There are not many levels of priority for RtMemoryCache, only Default and NotRemovable. Now yields always Default.")]
		public CacheItemPriority Priority => CacheItemPriority.Default;

		private void Configure(IDictionary<string, string> props)
		{
			if (props == null)
			{
				if (Log.IsWarnEnabled)
				{
					Log.Warn("configuring cache with default values");
				}
				Expiration = DefaultExpiration;
				UseSlidingExpiration = _defaultUseSlidingExpiration;
				_regionPrefix = DefaultRegionPrefix;
			}
			else
			{
				Expiration= GetExpiration(props);
				UseSlidingExpiration = GetUseSlidingExpiration(props);
				_regionPrefix= GetRegionPrefix(props);
			}
		}

		private static string GetRegionPrefix(IDictionary<string, string> props)
		{
			if (props.TryGetValue("regionPrefix", out var result))
			{
				Log.DebugFormat("new regionPrefix: {0}", result);
			}
			else
			{
				result = DefaultRegionPrefix;
				Log.Debug("no regionPrefix value given, using defaults");
			}
			return result;
		}

		private static TimeSpan GetExpiration(IDictionary<string, string> props)
		{
			var result = DefaultExpiration;
			if (!props.TryGetValue("expiration", out var expirationString))
			{
				props.TryGetValue(Cfg.Environment.CacheDefaultExpiration, out expirationString);
			}

			if (expirationString != null)
			{
				try
				{
					var seconds = Convert.ToInt32(expirationString);
					result = TimeSpan.FromSeconds(seconds);
					Log.DebugFormat("new expiration value: {0}", seconds);
				}
				catch (Exception ex)
				{
					Log.ErrorFormat("error parsing expiration value '{0}'", expirationString);
					throw new ArgumentException($"could not parse expiration '{expirationString}' as a number of seconds", ex);
				}
			}
			else
			{
				if (Log.IsDebugEnabled)
				{
					Log.Debug("no expiration value given, using defaults");
				}
			}
			return result;
		}

		private static bool GetUseSlidingExpiration(IDictionary<string, string> props)
		{
			var sliding = PropertiesHelper.GetBoolean("cache.use_sliding_expiration", props, _defaultUseSlidingExpiration);
			Log.DebugFormat("Use sliding expiration value: {0}", sliding);
			return sliding;
		}

		private string GetCacheKey(object key)
		{
			return string.Concat(_cacheKeyPrefix, _regionPrefix, Region, ":", key.ToString(), "@", key.GetHashCode());
		}

		public object Get(object key)
		{
			if (key == null)
			{
				return null;
			}
			var cacheKey = GetCacheKey(key);
			Log.DebugFormat("Fetching object '{0}' from the cache.", cacheKey);

			var obj = _cache.Get(cacheKey);
			if (obj == null)
			{
				return null;
			}

			var de = (DictionaryEntry) obj;
			return key.Equals(de.Key) ? de.Value : null;
		}

		public void Put(object key, object value)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key), "null key not allowed");
			}
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value), "null value not allowed");
			}
			var cacheKey = GetCacheKey(key);
			if (_cache[cacheKey] != null)
			{
				Log.DebugFormat("updating value of key '{0}' to '{1}'.", cacheKey, value);

				// Remove the key to re-add it again below
				_cache.Remove(cacheKey);
			}
			else
			{
				Log.DebugFormat("adding new data: key={0}&value={1}", cacheKey, value);
			}

			if (!_rootCacheKeyStored)
			{
				StoreRootCacheKey();
			}

			_cache.Add(cacheKey, new DictionaryEntry(key, value),
			          new CacheItemPolicy
			          {
			              AbsoluteExpiration = UseSlidingExpiration ? ObjectCache.InfiniteAbsoluteExpiration : DateTimeOffset.UtcNow.Add(Expiration),
			              SlidingExpiration = UseSlidingExpiration ? Expiration : ObjectCache.NoSlidingExpiration,
			              ChangeMonitors = {_cache.CreateCacheEntryChangeMonitor(new[] {_rootCacheKey})}
			          });
		}

		public void Remove(object key)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}
			var cacheKey = GetCacheKey(key);
			Log.DebugFormat("removing item with key: {0}", cacheKey);
			_cache.Remove(cacheKey);
		}

		public void Clear()
		{
			RemoveRootCacheKey();
			StoreRootCacheKey();
		}

		/// <summary>
		/// Generate a unique root key for all cache items to be dependant upon
		/// </summary>
		private string GenerateRootCacheKey()
		{
			return GetCacheKey(Guid.NewGuid());
		}

		private void RootCacheItemRemoved(CacheEntryRemovedArguments arguments)
		{
			_rootCacheKeyStored = false;
		}

		private void StoreRootCacheKey()
		{
			_rootCacheKeyStored = true;
			_cache.Add(
				_rootCacheKey,
				_rootCacheKey,
				new CacheItemPolicy
				{
					AbsoluteExpiration = ObjectCache.InfiniteAbsoluteExpiration,
					SlidingExpiration = ObjectCache.NoSlidingExpiration,
					Priority = CacheItemPriority.Default,
					RemovedCallback = RootCacheItemRemoved
				});
		}

		private void RemoveRootCacheKey()
		{
			_cache.Remove(_rootCacheKey);
		}

		public void Destroy()
		{
			Clear();
		}

		public void Lock(object key)
		{
			// Do nothing
		}

		public void Unlock(object key)
		{
			// Do nothing
		}

		public long NextTimestamp()
		{
			return Timestamper.Next();
		}

		public int Timeout => Timestamper.OneMs * 60000;

		public string RegionName => Region;

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
	}
}
