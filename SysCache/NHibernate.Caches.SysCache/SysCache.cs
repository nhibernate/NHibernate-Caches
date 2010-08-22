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

namespace NHibernate.Caches.SysCache
{
	/// <summary>
	/// Pluggable cache implementation using the System.Web.Caching classes
	/// </summary>
	public class SysCache : ICache
	{
		private static readonly ILogger log = LoggerProvider.LoggerFor(typeof(SysCache));
		private readonly string region;
		private string regionPrefix;
		private readonly System.Web.Caching.Cache cache;
		private TimeSpan expiration;
		private CacheItemPriority priority;
		// The name of the cache key used to clear the cache. All cached items depend on this key.
		private readonly string rootCacheKey;
		private bool rootCacheKeyStored;
		private static readonly TimeSpan DefaultExpiration = TimeSpan.FromSeconds(300);
		private static readonly string DefauktRegionPrefix = string.Empty;
		private const string CacheKeyPrefix = "NHibernate-Cache:";

		/// <summary>
		/// default constructor
		/// </summary>
		public SysCache()
			: this("nhibernate", null)
		{
		}

		/// <summary>
		/// constructor with no properties
		/// </summary>
		/// <param name="region"></param>
		public SysCache(string region)
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
		public SysCache(string region, IDictionary<string,string> properties)
		{
			this.region = region;
			cache = HttpRuntime.Cache;
			Configure(properties);

			rootCacheKey = GenerateRootCacheKey();
			StoreRootCacheKey();
		}

		public string Region
		{
			get { return region; }
		}

		public TimeSpan Expiration
		{
			get { return expiration; }
		}

		public CacheItemPriority Priority
		{
			get { return priority; }
		}

		private void Configure(IDictionary<string, string> props)
		{
			if (props == null)
			{
				if (log.IsWarnEnabled)
				{
					log.Warn("configuring cache with default values");
				}
				expiration = DefaultExpiration;
				priority = CacheItemPriority.Default;
				regionPrefix = DefauktRegionPrefix;
			}
			else
			{
				priority = GetPriority(props);
				expiration= GetExpiration(props);
				regionPrefix= GetRegionPrefix(props);
			}
		}

		private static string GetRegionPrefix(IDictionary<string, string> props)
		{
			string result;
			if (props.TryGetValue("regionPrefix", out result))
			{
				log.DebugFormat("new regionPrefix :{0}", result);
			}
			else
			{
				result = DefauktRegionPrefix;
				log.Debug("no regionPrefix value given, using defaults");
			}
			return result;
		}

		private static TimeSpan GetExpiration(IDictionary<string, string> props)
		{
			TimeSpan result = DefaultExpiration;
			string expirationString;
			if (!props.TryGetValue("expiration", out expirationString))
			{
				props.TryGetValue(Cfg.Environment.CacheDefaultExpiration, out expirationString);
			}

			if (expirationString != null)
			{
				try
				{
					int seconds = Convert.ToInt32(expirationString);
					result = TimeSpan.FromSeconds(seconds);
					log.Debug("new expiration value: " + seconds);
				}
				catch (Exception ex)
				{
					log.Error("error parsing expiration value");
					throw new ArgumentException("could not parse 'expiration' as a number of seconds", ex);
				}
			}
			else
			{
				if (log.IsDebugEnabled)
				{
					log.Debug("no expiration value given, using defaults");
				}
			}
			return result;
		}

		private static CacheItemPriority GetPriority(IDictionary<string, string> props)
		{
			CacheItemPriority result = CacheItemPriority.Default;
			string priorityString;
			if (props.TryGetValue("priority", out priorityString))
			{
				result = ConvertCacheItemPriorityFromXmlString(priorityString);
				if (log.IsDebugEnabled)
				{
					log.Debug("new priority: " + result);
				}
			}
			return result;
		}


		private static CacheItemPriority ConvertCacheItemPriorityFromXmlString(string priorityString)
		{
			if(string.IsNullOrEmpty(priorityString))
			{
				return CacheItemPriority.Default;
			}
			var ps = priorityString.Trim().ToLowerInvariant();
			if(ps.Length == 1 && char.IsDigit(priorityString,0))
			{
				// the priority is specified as a number
				int priorityAsInt = int.Parse(ps);
				if(priorityAsInt >= 1 && priorityAsInt <=6)
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
			log.Error("priority value out of range: " + priorityString);
			throw new IndexOutOfRangeException("Priority must be a valid System.Web.Caching.CacheItemPriority; was: " + priorityString);
		}

		private string GetCacheKey(object key)
		{
			return String.Concat(CacheKeyPrefix, regionPrefix, region, ":", key.ToString(), "@", key.GetHashCode());
		}

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

			object obj = cache.Get(cacheKey);
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

		public void Put(object key, object value)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key", "null key not allowed");
			}
			if (value == null)
			{
				throw new ArgumentNullException("value", "null value not allowed");
			}
			string cacheKey = GetCacheKey(key);
			if (cache[cacheKey] != null)
			{
				if (log.IsDebugEnabled)
				{
					log.Debug(String.Format("updating value of key '{0}' to '{1}'.", cacheKey, value));
				}

				// Remove the key to re-add it again below
				cache.Remove(cacheKey);
			}
			else
			{
				if (log.IsDebugEnabled)
				{
					log.Debug(String.Format("adding new data: key={0}&value={1}", cacheKey, value));
				}
			}

			if (!rootCacheKeyStored)
			{
				StoreRootCacheKey();
			}

			cache.Add(
				cacheKey,
				new DictionaryEntry(key, value),
				new CacheDependency(null, new[] {rootCacheKey}),
				DateTime.Now.Add(expiration),
				System.Web.Caching.Cache.NoSlidingExpiration,
				priority,
				null);
		}

		public void Remove(object key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			string cacheKey = GetCacheKey(key);
			if (log.IsDebugEnabled)
			{
				log.Debug("removing item with key: " + cacheKey);
			}
			cache.Remove(cacheKey);
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

		private void RootCacheItemRemoved(string key, object value, CacheItemRemovedReason reason)
		{
			rootCacheKeyStored = false;
		}

		private void StoreRootCacheKey()
		{
			rootCacheKeyStored = true;
			cache.Add(
				rootCacheKey,
				rootCacheKey,
				null,
				System.Web.Caching.Cache.NoAbsoluteExpiration,
				System.Web.Caching.Cache.NoSlidingExpiration,
				CacheItemPriority.Default,
				RootCacheItemRemoved);
		}

		private void RemoveRootCacheKey()
		{
			cache.Remove(rootCacheKey);
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

		public int Timeout
		{
			get { return Timestamper.OneMs * 60000; } // 60 seconds
		}

		public string RegionName
		{
			get { return region; }
		}
	}
}