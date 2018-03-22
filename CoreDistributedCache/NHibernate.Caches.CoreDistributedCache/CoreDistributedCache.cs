#region License

//
//  CoreDistributedCache - A cache provider for NHibernate using Microsoft.Extensions.Caching.Distributed.IDistributedCache.
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
using NHibernate.Cache;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using NHibernate.Util;

namespace NHibernate.Caches.CoreDistributedCache
{
	/// <summary>
	/// Pluggable cache implementation using the Microsoft.Extensions.Caching.Memory classes.
	/// </summary>
	/// <remarks>
	/// Priority is not configurable because it is un-usable: the compaction on memory pressure feature has been
	/// removed from MemoryCache, only explicit compaction or size limit compaction may use priorities. But
	/// <see cref="ICache" /> API does not have a suitable method for triggering compaction, and size of each
	/// cached entry has to be user provided, which <see cref="ICache" /> API does not support.
	/// </remarks>
	public partial class CoreDistributedCache : ICache
	{
		private static readonly INHibernateLogger Log = NHibernateLogger.For(typeof(CoreDistributedCache));

		private readonly IDistributedCache _cache;
		private readonly int? _maxKeySize;

		private static readonly TimeSpan DefaultExpiration = TimeSpan.FromSeconds(300);
		private const bool _defaultUseSlidingExpiration = false;
		private static readonly string DefaultRegionPrefix = string.Empty;
		private const string _cacheKeyPrefix = "NHibernate-Cache:";

		private string _fullRegion;
		private bool _hasWarnedOnHashLength;

		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="cache">The <see cref="IDistributedCache"/> instance to use.</param>
		/// <param name="maxKeySize">If key size is limited, the maximal key size.</param>
		/// <param name="region">The region of the cache.</param>
		/// <param name="properties">Cache configuration properties.</param>
		/// <remarks>
		/// There are three (3) configurable parameters:
		/// <ul>
		///		<li>expiration (or cache.default_expiration) = number of seconds to wait before expiring each item.</li>
		///		<li>cache.use_sliding_expiration = a boolean, true for resetting a cached item expiration each time it is accessed.</li>
		/// 	<li>regionPrefix = a string for prefixing the region name.</li>
		/// </ul>
		/// All parameters are optional. The defaults are an expiration of 300 seconds, no sliding expiration and no prefix.
		/// </remarks>
		/// <exception cref="ArgumentException">The "expiration" property could not be parsed.</exception>
		public CoreDistributedCache(
			IDistributedCache cache, int? maxKeySize, string region, IDictionary<string, string> properties)
		{
			if (maxKeySize.HasValue && maxKeySize <= 0)
				throw new ArgumentException($"{nameof(maxKeySize)} must be null or superior to 1.", nameof(maxKeySize));
			_cache = cache;
			_maxKeySize = maxKeySize;
			RegionName = region;
			Configure(properties);
		}

		/// <inheritdoc />
		public string RegionName { get; }

		/// <summary>
		/// The expiration delay applied to cached items.
		/// </summary>
		public TimeSpan Expiration { get; private set; }

		/// <summary>
		/// Should the expiration delay be sliding?
		/// </summary>
		/// <value><see langword="true" /> for resetting a cached item expiration each time it is accessed.</value>
		public bool UseSlidingExpiration { get; private set; }

		private void Configure(IDictionary<string, string> props)
		{
			var regionPrefix = DefaultRegionPrefix;
			if (props == null)
			{
				Log.Warn("Configuring cache with default values");
				Expiration = DefaultExpiration;
				UseSlidingExpiration = _defaultUseSlidingExpiration;
			}
			else
			{
				Expiration = GetExpiration(props);
				UseSlidingExpiration = GetUseSlidingExpiration(props);
				regionPrefix = GetRegionPrefix(props);
			}

			_fullRegion = regionPrefix + RegionName;
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
				if (int.TryParse(expirationString, out var seconds))
				{
					result = TimeSpan.FromSeconds(seconds);
					Log.Debug("new expiration value: {0}", seconds);
				}
				else
				{
					Log.Error("error parsing expiration value '{0}'", expirationString);
					throw new ArgumentException($"could not parse expiration '{expirationString}' as a number of seconds");
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

		private string GetCacheKey(object key)
		{
			var keyAsString = string.Concat(_cacheKeyPrefix, _fullRegion, ":", key.ToString(), "@", key.GetHashCode());
			if (!_maxKeySize.HasValue || _maxKeySize >= keyAsString.Length)
				return keyAsString;

			Log.Info(
				"Computing a hashed key for too long key '{0}'. This may cause collisions resulting into additional cache misses.",
				key);
			// Hash it for respecting max key size. Collisions will be avoided by storing the actual key along
			// the object and comparing it on retrieval.
			using (var hasher = new SHA256Managed())
			{
				var bytes = Encoding.UTF8.GetBytes(keyAsString);
				var computedHash = Convert.ToBase64String(hasher.ComputeHash(bytes));
				if (computedHash.Length <= _maxKeySize)
					return computedHash;

				if (!_hasWarnedOnHashLength)
				{
					// No lock for this field, some redundant logs will be less harm than locking.
					_hasWarnedOnHashLength = true;
					Log.Warn(
						"Hash computed for too long keys are themselves too long. They will be truncated, further " +
						"increasing the risk of collision resulting into additional cache misses. Consider using a " +
						"cache supporting longer keys. Hash length: {0}; max key size: {1}",
						computedHash.Length, _maxKeySize);
				}

				return computedHash.Substring(0, _maxKeySize.Value);
			}
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

			var cachedData = _cache.Get(cacheKey);
			if (cachedData == null)
				return null;

			var serializer = new BinaryFormatter();
			using (var stream = new MemoryStream(cachedData))
			{
				var entry = serializer.Deserialize(stream) as Tuple<object, object>;
				return Equals(entry?.Item1, key) ? entry.Item2 : null;
			}
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

			byte[] cachedData;
			var serializer = new BinaryFormatter();
			using (var stream = new MemoryStream())
			{
				var entry = new Tuple<object, object>(key, value);
				serializer.Serialize(stream, entry);
				cachedData = stream.ToArray();
			}

			var cacheKey = GetCacheKey(key);
			var options = new DistributedCacheEntryOptions();
			if (UseSlidingExpiration)
				options.SlidingExpiration = Expiration;
			else
				options.AbsoluteExpirationRelativeToNow = Expiration;

			Log.Debug("putting item with key: {0}", cacheKey);
			_cache.Set(cacheKey, cachedData, options);
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
			// Like IMemoryCache, it does not support Clear. Unlike it, it does neither provides a dependency
			// mechanism which would allow to implement it.
			Log.Warn($"Clear is not supported by {nameof(IDistributedCache)}, ignoring the call.");
		}

		/// <inheritdoc />
		public void Destroy()
		{
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
	}
}
