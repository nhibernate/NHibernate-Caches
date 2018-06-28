#region License

//
//  SysCache - A cache provider for NHibernate using System.Web.Caching.Cache.
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
using System.Web;
using System.Web.Caching;
using NHibernate.Cache;
using System.Collections.Generic;
using NHibernate.Util;

namespace NHibernate.Caches.SysCache
{
	/// <summary>
	/// Pluggable cache implementation using the System.Web.Caching classes.
	/// </summary>
	public partial class SysCache : ICache
	{
		private static readonly INHibernateLogger Log = NHibernateLogger.For(typeof(SysCache));
		private string _regionPrefix;
		private readonly System.Web.Caching.Cache _cache;

		// The name of the cache key used to clear the cache. All cached items depend on this key.
		private readonly string _rootCacheKey;

		private bool _rootCacheKeyStored;
		private static readonly TimeSpan DefaultExpiration = TimeSpan.FromSeconds(300);
		private const bool _defaultUseSlidingExpiration = false;
		private static readonly string DefaultRegionPrefix = string.Empty;
		private const string _cacheKeyPrefix = "NHibernate-Cache:";

		/// <summary>
		/// Default constructor.
		/// </summary>
		public SysCache()
			: this("nhibernate", null)
		{
		}

		/// <summary>
		/// Constructor with no properties.
		/// </summary>
		/// <param name="region">The cache region name.</param>
		public SysCache(string region)
			: this(region, null)
		{
		}

		/// <summary>
		/// Full constructor.
		/// </summary>
		/// <param name="region">The cache region name.</param>
		/// <param name="properties">The cache configuration properties.</param>
		/// <remarks>
		/// There are four (4) configurable parameters:
		/// <ul>
		///   <li>expiration (or cache.default_expiration) = number of seconds to wait before expiring each item</li>
		///   <li>cache.use_sliding_expiration = a boolean, true for resetting a cached item expiration each time it is accessed.</li>
		///   <li>regionPrefix = a string for prefixing the region name.</li>
		///   <li>priority = a numeric cost of expiring each item, where 1 is a low cost, 6 is the highest, and 3 is
		///         normal. Only values 1 through 6 are valid. 6 should be avoided, this value is the
		///         <see cref="CacheItemPriority.NotRemovable" /> priority.</li>
		/// </ul>
		/// All parameters are optional. The defaults are an expiration of 300 seconds, no sliding expiration, no region
		/// prefix and the default priority of 3.
		/// </remarks>
		/// <exception cref="IndexOutOfRangeException">The "priority" property is not between 1 and 6</exception>
		/// <exception cref="ArgumentException">The "expiration" property could not be parsed.</exception>
		public SysCache(string region, IDictionary<string, string> properties)
		{
			Region = region;
			_cache = HttpRuntime.Cache;
			Configure(properties);

			_rootCacheKey = GenerateRootCacheKey();
			StoreRootCacheKey();
		}

		/// <summary>
		/// The cache region.
		/// </summary>
		public string Region { get; }

		/// <summary>
		/// The cached items expiration.
		/// </summary>
		public TimeSpan Expiration { get; private set; }

		/// <summary>
		/// The cached items <see cref="CacheItemPriority"/>.
		/// </summary>
		public CacheItemPriority Priority { get; private set; }

		/// <summary>
		/// Whether the cached items expiration is sliding (reset at each hit) or not.
		/// </summary>
		public bool UseSlidingExpiration { get; private set; }

		private void Configure(IDictionary<string, string> props)
		{
			if (props == null)
			{
				Log.Warn("configuring cache with default values");
				Expiration = DefaultExpiration;
				UseSlidingExpiration = _defaultUseSlidingExpiration;
				Priority = CacheItemPriority.Default;
				_regionPrefix = DefaultRegionPrefix;
			}
			else
			{
				Priority = GetPriority(props);
				Expiration = GetExpiration(props);
				UseSlidingExpiration = GetUseSlidingExpiration(props);
				_regionPrefix = GetRegionPrefix(props);
			}
		}

		private static string GetRegionPrefix(IDictionary<string, string> props)
		{
			if (props.TryGetValue("regionPrefix", out var result))
			{
				Log.Debug("new regionPrefix: {0}", result);
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
					Log.Debug("new expiration value: {0}", seconds);
				}
				catch (Exception ex)
				{
					Log.Error("error parsing expiration value '{0}'", expirationString);
					throw new ArgumentException($"could not parse expiration '{expirationString}' as a number of seconds", ex);
				}
			}
			else
			{
				Log.Debug("no expiration value given, using defaults");
			}
			return result;
		}

		private static bool GetUseSlidingExpiration(IDictionary<string, string> props)
		{
			var sliding = PropertiesHelper.GetBoolean("cache.use_sliding_expiration", props, _defaultUseSlidingExpiration);
			Log.Debug("Use sliding expiration value: {0}", sliding);
			return sliding;
		}

		private static CacheItemPriority GetPriority(IDictionary<string, string> props)
		{
			var result = CacheItemPriority.Default;
			if (props.TryGetValue("priority", out var priorityString))
			{
				result = ConvertCacheItemPriorityFromXmlString(priorityString);
				Log.Debug("new priority: {0}", result);
			}
			return result;
		}

		private static CacheItemPriority ConvertCacheItemPriorityFromXmlString(string priorityString)
		{
			if (string.IsNullOrEmpty(priorityString))
			{
				return CacheItemPriority.Default;
			}
			var ps = priorityString.Trim().ToLowerInvariant();
			if (ps.Length == 1 && char.IsDigit(priorityString, 0))
			{
				// the priority is specified as a number
				var priorityAsInt = int.Parse(ps);
				if (priorityAsInt >= 1 && priorityAsInt <= 6)
				{
					return (CacheItemPriority) priorityAsInt;
				}
			}
			else
			{
				switch (ps)
				{
					case "abovenormal":
						return CacheItemPriority.AboveNormal;
					case "belownormal":
						return CacheItemPriority.BelowNormal;
					case "default":
						return CacheItemPriority.Default;
					case "high":
						return CacheItemPriority.High;
					case "low":
						return CacheItemPriority.Low;
					case "normal":
						return CacheItemPriority.Normal;
					case "notremovable":
						return CacheItemPriority.NotRemovable;
				}
			}
			Log.Error("priority value out of range: {0}", priorityString);
			throw new IndexOutOfRangeException("Priority must be a valid System.Web.Caching.CacheItemPriority; was: " +
				priorityString);
		}

		private string GetCacheKey(object key)
		{
			return string.Concat(_cacheKeyPrefix, _regionPrefix, Region, ":", key.ToString(), "@", key.GetHashCode());
		}

		/// <inheritdoc />
		public object Get(object key)
		{
			if (key == null)
			{
				return null;
			}
			var cacheKey = GetCacheKey(key);
			Log.Debug("Fetching object '{0}' from the cache.", cacheKey);

			var obj = _cache.Get(cacheKey);
			if (obj == null)
			{
				return null;
			}

			var de = (DictionaryEntry) obj;
			return key.Equals(de.Key) ? de.Value : null;
		}

		/// <inheritdoc />
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
			if (Log.IsDebugEnabled())
			{
				Log.Debug(
					_cache[cacheKey] != null
						? "updating value of key '{0}' to '{1}'."
						: "adding new data: key={0}&value={1}", cacheKey, value);
			}

			if (!_rootCacheKeyStored)
			{
				StoreRootCacheKey();
			}

			_cache.Insert(
				cacheKey,
				new DictionaryEntry(key, value),
				new CacheDependency(null, new[] { _rootCacheKey }),
				UseSlidingExpiration ? System.Web.Caching.Cache.NoAbsoluteExpiration : DateTime.UtcNow.Add(Expiration),
				UseSlidingExpiration ? Expiration : System.Web.Caching.Cache.NoSlidingExpiration,
				Priority,
				null);
		}

		/// <inheritdoc />
		public void Remove(object key)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}
			var cacheKey = GetCacheKey(key);
			Log.Debug("removing item with key: {0}", cacheKey);
			_cache.Remove(cacheKey);
		}

		/// <inheritdoc />
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

		private void RootCacheItemRemoved(string key, object value, CacheItemRemovedReason reason)
		{
			_rootCacheKeyStored = false;
		}

		private void StoreRootCacheKey()
		{
			_rootCacheKeyStored = true;
			_cache.Add(
				_rootCacheKey,
				_rootCacheKey,
				null,
				System.Web.Caching.Cache.NoAbsoluteExpiration,
				System.Web.Caching.Cache.NoSlidingExpiration,
				CacheItemPriority.Default,
				RootCacheItemRemoved);
		}

		private void RemoveRootCacheKey()
		{
			_cache.Remove(_rootCacheKey);
		}

		/// <inheritdoc />
		public void Destroy()
		{
			Clear();
		}

		/// <inheritdoc />
		public void Lock(object key)
		{
			// Do nothing
		}

		/// <inheritdoc />
		public void Unlock(object key)
		{
			// Do nothing
		}

		/// <inheritdoc />
		public long NextTimestamp()
		{
			return Timestamper.Next();
		}

		/// <inheritdoc />
		public int Timeout => Timestamper.OneMs * 60000;

		/// <inheritdoc />
		public string RegionName => Region;
	}
}
