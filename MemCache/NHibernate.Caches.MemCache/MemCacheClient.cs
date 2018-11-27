#region License

//
//  MemCache - A cache provider for NHibernate using the .NET client
//  (http://sourceforge.net/projects/memcacheddotnet) for memcached,
//  which is located at http://www.danga.com/memcached/.
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
// CLOVER:OFF
//

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using Memcached.ClientLibrary;
using NHibernate.Cache;
using NHibernate.Caches.Util;

namespace NHibernate.Caches.MemCache
{
	/// <summary>
	/// Pluggable cache implementation using Memcached.
	/// </summary>
	public class MemCacheClient : CacheBase
	{
		internal const string PoolName = "nhibernate";
		private static readonly INHibernateLogger log;

		private readonly MemcachedClient client;
		private readonly int expiry;

		private readonly string region;
		private readonly string regionPrefix = "";
		private readonly bool noLingeringDelete;

		private const int _maxKeySize = 249;

		static MemCacheClient()
		{
			log = NHibernateLogger.For(typeof(MemCacheClient));
		}

		/// <summary>
		/// Default constructor.
		/// </summary>
		public MemCacheClient() : this("nhibernate", null)
		{
		}

		/// <summary>
		/// Contructor with no properties.
		/// </summary>
		/// <param name="regionName">The cache region name.</param>
		public MemCacheClient(string regionName) : this(regionName, null)
		{
		}

		/// <summary>
		/// Full constructor.
		/// </summary>
		/// <param name="regionName">The cache region name.</param>
		/// <param name="properties">The configuration properties.</param>
		public MemCacheClient(string regionName, IDictionary<string, string> properties)
		{
			region = regionName;
			client = new MemcachedClient { PoolName = PoolName };
			expiry = 300;

			if (properties != null)
			{
				if (properties.ContainsKey("compression_enabled"))
				{
					client.EnableCompression = Convert.ToBoolean(properties["compression_enabled"]);
					log.Debug("compression_enabled set to {0}", client.EnableCompression);
				}

				var expirationString = GetExpirationString(properties);
				if (expirationString != null)
				{
					expiry = Convert.ToInt32(expirationString);
					log.Debug("using expiration of {0} seconds", expiry);
				}

				if (properties.ContainsKey("regionPrefix"))
				{
					regionPrefix = properties["regionPrefix"];

					log.Debug("new regionPrefix :{0}", regionPrefix);
				}
				else
				{
					log.Debug("no regionPrefix value given, using defaults");
				}

				if (properties.ContainsKey("lingering_delete_disabled"))
				{
					noLingeringDelete = Convert.ToBoolean(properties["lingering_delete_disabled"]);
					log.Debug("lingering_delete_disabled set to {0}", noLingeringDelete);
				}
			}
		}

		private static string GetExpirationString(IDictionary<string, string> props)
		{
			string result;
			if (!props.TryGetValue("expiration", out result))
			{
				props.TryGetValue(Cfg.Environment.CacheDefaultExpiration, out result);
			}
			return result;
		}

		#region CacheBase Members

		/// <inheritdoc />
		public override object Get(object key)
		{
			if (key == null)
			{
				return null;
			}
			log.Debug("fetching object {0} from the cache", key);
			object maybeObj = client.Get(KeyAsString(key));
			if (maybeObj == null)
			{
				return null;
			}
			//we need to check here that the key that we stored is really the key that we got
			//the reason is that for long keys, we hash the value, and this mean that we may get
			//hash collisions. The chance is very low, but it is better to be safe
			var de = (DictionaryEntry) maybeObj;
			if (key.ToString().Equals(de.Key))
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
				throw new ArgumentNullException("key", "null key not allowed");
			}
			if (value == null)
			{
				throw new ArgumentNullException("value", "null value not allowed");
			}

			log.Debug("setting value for item {0}", key);
			bool returnOk = client.Set(KeyAsString(key), new DictionaryEntry(key.ToString(), value),
				DateTime.Now.AddSeconds(expiry));
			if (!returnOk)
			{
				log.Warn("could not save: {0} => {1}", key, value);
			}
		}

		/// <inheritdoc />
		public override void Remove(object key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			log.Debug("removing item {0}", key);

			if (noLingeringDelete)
				client.Delete(KeyAsString(key)); // Memcached 1.4+ does not support lingering deletes anymore
			else
				client.Delete(KeyAsString(key), DateTime.Now.AddSeconds(expiry));
		}

		/// <inheritdoc />
		public override void Clear()
		{
			client.FlushAll();
		}

		/// <inheritdoc />
		public override void Destroy()
		{
			Clear();
		}

		/// <inheritdoc />
		public override object Lock(object key)
		{
			// do nothing
			return null;
		}

		/// <inheritdoc />
		public override void Unlock(object key, object lockValue)
		{
			// do nothing
		}

		/// <inheritdoc />
		public override long NextTimestamp()
		{
			return Timestamper.Next();
		}

		/// <inheritdoc />
		public override int Timeout
		{
			get { return Timestamper.OneMs * 60000; }
		}

		/// <inheritdoc />
		public override string RegionName
		{
			get { return region; }
		}

		#endregion

		/// <summary>
		/// Turn the key obj into a string, preperably using human readable
		/// string, and if the string is too long (>_maxKeySize) it will be hashed
		/// </summary>
		private string KeyAsString(object key)
		{
			string fullKey = FullKeyAsString(key);
			if (fullKey.Length > _maxKeySize)
			{
				var toLongKey = fullKey;
				fullKey = ComputeHash(fullKey);

				log.Info(
					"Computed the hashed key '{0}' for too long cache key '{1}' (object key '{2}'). This may cause" +
					"collisions resulting into additional cache misses.",
					fullKey, toLongKey, key);
			}
			return fullKey.Replace(' ', '-');
		}

		/// <summary>
		/// Turn the key object into a human readable string.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		private string FullKeyAsString(object key)
		{
			return string.Format("{0}{1}@{2}", regionPrefix, region, (key == null ? string.Empty : key.ToString()));
		}

		/// <summary>
		/// Compute the hash of the full key string using the given hash algorithm
		/// </summary>
		/// <param name="fullKeyString">The full key return by call FullKeyAsString</param>
		/// <returns>The hashed key as a string</returns>
		private static string ComputeHash(string fullKeyString)
		{
			// Hash it for respecting max key size. Collisions will be avoided by storing the actual key along
			// the object and comparing it on retrieval.
			var hash = Hasher.HashToString(fullKeyString);

			return fullKeyString.Substring(0, _maxKeySize - hash.Length) + hash;
		}
	}
}
