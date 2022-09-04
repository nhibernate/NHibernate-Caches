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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using NHibernate.Caches.Common;
using NHibernate.Caches.Util;
using NHibernate.Util;

namespace NHibernate.Caches.CoreDistributedCache
{
	// 6.0 TODO: replace that class by its base
	/// <summary>
	/// Pluggable cache implementation using <see cref="IDistributedCache"/> implementations.
	/// </summary>
	public class CoreDistributedCache : CoreDistributedCacheBase,
#pragma warning disable 618
		ICache
#pragma warning restore 618
	{
		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="cache">The <see cref="IDistributedCache"/> instance to use.</param>
		/// <param name="constraints">Optional constraints of <paramref name="cache"/>.</param>
		/// <param name="region">The region of the cache.</param>
		/// <param name="properties">Cache configuration properties.</param>
		/// <remarks>
		/// There are three (3) configurable parameters taken in <paramref name="properties"/>:
		/// <ul>
		///		<li>expiration (or cache.default_expiration) = number of seconds to wait before expiring each item.</li>
		///		<li>cache.use_sliding_expiration = a boolean, true for resetting a cached item expiration each time it is accessed.</li>
		/// 	<li>regionPrefix = a string for prefixing the region name.</li>
		/// </ul>
		/// All parameters are optional. The defaults are an expiration of 300 seconds, no sliding expiration and no prefix.
		/// </remarks>
		/// <exception cref="ArgumentException">The "expiration" property could not be parsed.</exception>
		[CLSCompliant(false)]
		public CoreDistributedCache(
			IDistributedCache cache, CacheConstraints constraints, string region,
			IDictionary<string, string> properties)
			: base(cache, constraints, region, properties)
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
	/// Pluggable cache implementation using <see cref="IDistributedCache"/> implementations.
	/// </summary>
	public abstract partial class CoreDistributedCacheBase : CacheBase
	{
		private static readonly INHibernateLogger Log = NHibernateLogger.For(typeof(CoreDistributedCache));

		private readonly IDistributedCache _cache;
		private readonly int? _maxKeySize;
		private readonly Func<string, string> _keySanitizer;
		private CacheSerializerBase _serializer;

		private static readonly TimeSpan DefaultExpiration = TimeSpan.FromSeconds(300);
		private const bool _defaultUseSlidingExpiration = false;
		private const bool _defaultAppendHashcodeToKey = false;
		private static readonly string DefaultRegionPrefix = string.Empty;
		private const string _cacheKeyPrefix = "NHibernate-Cache:";

		private string _fullRegion;
		private bool _hasWarnedOnHashLength;

		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="cache">The <see cref="IDistributedCache"/> instance to use.</param>
		/// <param name="constraints">Optional constraints of <paramref name="cache"/>.</param>
		/// <param name="region">The region of the cache.</param>
		/// <param name="properties">Cache configuration properties.</param>
		/// <remarks>
		/// There are three (3) configurable parameters taken in <paramref name="properties"/>:
		/// <ul>
		///		<li>expiration (or cache.default_expiration) = number of seconds to wait before expiring each item.</li>
		///		<li>cache.use_sliding_expiration = a boolean, true for resetting a cached item expiration each time it is accessed.</li>
		/// 	<li>regionPrefix = a string for prefixing the region name.</li>
		/// </ul>
		/// All parameters are optional. The defaults are an expiration of 300 seconds, no sliding expiration and no prefix.
		/// </remarks>
		/// <exception cref="ArgumentException">The "expiration" property could not be parsed.</exception>
		[CLSCompliant(false)]
		public CoreDistributedCacheBase(
			IDistributedCache cache, CacheConstraints constraints, string region, IDictionary<string, string> properties)
		{
			if (constraints?.MaxKeySize <= 0)
				throw new ArgumentException($"{nameof(CacheConstraints.MaxKeySize)} must be null or superior to 1.",
					nameof(constraints));
			_cache = cache;
			_maxKeySize = constraints?.MaxKeySize;
			_keySanitizer = constraints?.KeySanitizer;
			RegionName = region;
			Configure(properties);
		}

		/// <inheritdoc />
		public override string RegionName { get; }

		/// <summary>
		/// The expiration delay applied to cached items.
		/// </summary>
		public TimeSpan Expiration { get; private set; }

		/// <summary>
		/// Should the expiration delay be sliding?
		/// </summary>
		/// <value><see langword="true" /> for resetting a cached item expiration each time it is accessed.</value>
		public bool UseSlidingExpiration { get; private set; }

		/// <summary>Should the keys be appended with their hashcode?</summary>
		/// <remarks>
		/// <para>
		/// This option is a workaround for distinguishing composite-id missing an
		/// <see cref="object.ToString"/> override. It may causes trouble if the cache is shared
		/// between processes running another runtime than .Net Framework, or with future versions
		/// of .Net Framework: the hashcode is not guaranteed to be stable.
		/// </para>
		/// <para>
		/// The value of this property can be set with the attribute <c>append-hashcode</c> of the
		/// region configuration node, or globally through
		/// <see cref="CoreDistributedCacheProvider.AppendHashcodeToKey"/>.
		/// </para>
		/// </remarks>
		public bool AppendHashcodeToKey { get; private set; }

		private void Configure(IDictionary<string, string> props)
		{
			var regionPrefix = DefaultRegionPrefix;
			if (props == null)
			{
				Log.Warn("Configuring cache with default values");
				Expiration = DefaultExpiration;
				UseSlidingExpiration = _defaultUseSlidingExpiration;
				AppendHashcodeToKey = _defaultAppendHashcodeToKey;
			}
			else
			{
				Expiration = GetExpiration(props);
				UseSlidingExpiration = GetUseSlidingExpiration(props);
				regionPrefix = GetRegionPrefix(props);
				AppendHashcodeToKey = GetAppendHashcodeToKey(props);
			}

			_fullRegion = regionPrefix + RegionName;
			_serializer = CoreDistributedCacheProvider.GetSerializer(props) ??
				CoreDistributedCacheProvider.DefaultSerializer ??
				throw new InvalidOperationException("The serializer must be not null");
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

		private static bool GetAppendHashcodeToKey(IDictionary<string, string> props)
		{
			var append = PropertiesHelper.GetBoolean("cache.append_hashcode_to_key", props, _defaultAppendHashcodeToKey);
			Log.Debug("Use append hashcode to key value: {0}", append);
			return append;
		}

		private (string fullKey, string sanitzedKey) GetCacheKey(object key)
		{
			var baseKey = _cacheKeyPrefix + _fullRegion + ":" + key;
			baseKey = AppendHashcodeToKey
				? baseKey + "@" + key.GetHashCode()
				: baseKey;

			var keyAsString = baseKey;

			if (_maxKeySize < keyAsString.Length)
			{
				// Hash it for respecting max key size. Collisions will be avoided by storing the actual key along
				// the object and comparing it on retrieval.
				var hash = Hasher.HashToString(keyAsString);
				if (hash.Length > _maxKeySize)
				{
					if (!_hasWarnedOnHashLength)
					{
						// No lock for this field, some redundant logs will be less harm than locking.
						_hasWarnedOnHashLength = true;
						Log.Warn(
							"Hash computed for too long keys are themselves too long. They will be truncated, further " +
							"increasing the risk of collision resulting into additional cache misses. Consider using a " +
							"cache supporting longer keys. Hash length: {0}; max key size: {1}",
							hash.Length, _maxKeySize);
					}

					hash = hash.Substring(0, _maxKeySize.Value);
				}

				var toLongKey = keyAsString;
				keyAsString = keyAsString.Substring(0, _maxKeySize.Value - hash.Length) + hash;
				Log.Info(
					"Computed the hashed key '{0}' for too long cache key '{1}' (object key '{2}'). This may cause" +
					"collisions resulting into additional cache misses.",
					keyAsString, toLongKey, key);
			}

			if (_keySanitizer != null)
			{
				Log.Debug("Sanitizing cache key '{0}'.", keyAsString);
				keyAsString = _keySanitizer(keyAsString);
			}

			Log.Debug("Using cache key '{0}' for object key '{1}'.", keyAsString, key);

			return (baseKey, keyAsString);
		}

		/// <inheritdoc />
		public override object Get(object key)
		{
			if (key == null)
			{
				return null;
			}

			var (fullKey, cacheKey) = GetCacheKey(key);
			Log.Debug("Fetching object '{0}' from the cache.", cacheKey);

			var cachedData = _cache.Get(cacheKey);
			if (cachedData == null)
				return null;

			var entry = _serializer.Deserialize(cachedData) as Tuple<string, object>;
			return Equals(entry?.Item1, fullKey) ? entry.Item2 : null;
		}

		/// <inheritdoc />
		public override void Put(object key, object value)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key), "null key not allowed");
			}

			if (value == null)
			{
				throw new ArgumentNullException(nameof(value), "null value not allowed");
			}

			var (fullKey, cacheKey) = GetCacheKey(key);
			var entry = new Tuple<string, object>(fullKey, value);
			var cachedData = _serializer.Serialize(entry);

			var options = new DistributedCacheEntryOptions();
			if (UseSlidingExpiration)
				options.SlidingExpiration = Expiration;
			else
				options.AbsoluteExpirationRelativeToNow = Expiration;

			Log.Debug("putting item with key: {0}", cacheKey);
			_cache.Set(cacheKey, cachedData, options);
		}

		/// <inheritdoc />
		public override void Remove(object key)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			var (_, cacheKey) = GetCacheKey(key);
			Log.Debug("removing item with key: {0}", cacheKey);
			_cache.Remove(cacheKey);
		}

		/// <inheritdoc />
		public override void Clear()
		{
			// Like IMemoryCache, it does not support Clear. Unlike it, it does neither provides a dependency
			// mechanism which would allow to implement it.
			Log.Warn($"Clear is not supported by {nameof(IDistributedCache)}, ignoring the call.");
		}

		/// <inheritdoc />
		public override void Destroy()
		{
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
		public override int Timeout => Timestamper.OneMs * 60000;
	}
}
