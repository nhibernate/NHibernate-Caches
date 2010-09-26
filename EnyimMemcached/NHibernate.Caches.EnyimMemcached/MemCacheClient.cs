using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Enyim.Caching;
using Enyim.Caching.Memcached;
using NHibernate.Cache;
using Environment = NHibernate.Cfg.Environment;

namespace NHibernate.Caches.EnyimMemcached
{
	public class MemCacheClient : ICache
	{
		private static readonly IInternalLogger log;
		[ThreadStatic] private static HashAlgorithm hasher;

		[ThreadStatic] private static MD5 md5;
		private readonly MemcachedClient client;
		private readonly int expiry;

		private readonly string region;
		private readonly string regionPrefix = "";

		static MemCacheClient()
		{
			log = LoggerProvider.LoggerFor(typeof (MemCacheClient));
		}

		public MemCacheClient()
			: this("nhibernate", null)
		{
		}

		public MemCacheClient(string regionName)
			: this(regionName, null)
		{
		}

		public MemCacheClient(string regionName, IDictionary<string, string> properties)
			: this(regionName, properties, new MemcachedClient())
		{
		}

		public MemCacheClient(string regionName, IDictionary<string, string> properties, MemcachedClient memcachedClient)
		{
			region = regionName;

			client = memcachedClient;

			expiry = 300;

			if (properties != null)
			{
				string expirationString = GetExpirationString(properties);
				if (expirationString != null)
				{
					expiry = Convert.ToInt32(expirationString);
					if (log.IsDebugEnabled)
					{
						log.DebugFormat("using expiration of {0} seconds", expiry);
					}
				}

				if (properties.ContainsKey("regionPrefix"))
				{
					regionPrefix = properties["regionPrefix"];
					if (log.IsDebugEnabled)
					{
						log.DebugFormat("new regionPrefix :{0}", regionPrefix);
					}
				}
				else
				{
					if (log.IsDebugEnabled)
					{
						log.Debug("no regionPrefix value given, using defaults");
					}
				}
			}
		}

		private static HashAlgorithm Hasher
		{
			get
			{
				if (hasher == null)
				{
					hasher = HashAlgorithm.Create();
				}
				return hasher;
			}
		}

		private static MD5 Md5
		{
			get
			{
				if (md5 == null)
				{
					md5 = MD5.Create();
				}
				return md5;
			}
		}

		#region ICache Members

		public object Get(object key)
		{
			if (key == null)
			{
				return null;
			}
			if (log.IsDebugEnabled)
			{
				log.DebugFormat("fetching object {0} from the cache", key);
			}
			object maybeObj = client.Get(KeyAsString(key));
			if (maybeObj == null)
			{
				return null;
			}
			//we need to check here that the key that we stored is really the key that we got
			//the reason is that for long keys, we hash the value, and this mean that we may get
			//hash collisions. The chance is very low, but it is better to be safe
			var de = (DictionaryEntry) maybeObj;
			string checkKeyHash = GetAlternateKeyHash(key);
			if (checkKeyHash.Equals(de.Key))
			{
				return de.Value;
			}
			return null;
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

			if (log.IsDebugEnabled)
			{
				log.DebugFormat("setting value for item {0}", key);
			}
			bool returnOk = client.Store(
				StoreMode.Set, KeyAsString(key),
				new DictionaryEntry(GetAlternateKeyHash(key), value),
				TimeSpan.FromSeconds(expiry));
			if (!returnOk)
			{
				if (log.IsWarnEnabled)
				{
					log.WarnFormat("could not save: {0} => {1}", key, value);
				}
			}
		}

		public void Remove(object key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			if (log.IsDebugEnabled)
			{
				log.DebugFormat("removing item {0}", key);
			}
			client.Remove(KeyAsString(key));
		}

		public void Clear()
		{
			client.FlushAll();
		}

		public void Destroy()
		{
			Clear();
		}

		public void Lock(object key)
		{
			// do nothing
		}

		public void Unlock(object key)
		{
			// do nothing
		}

		public long NextTimestamp()
		{
			return Timestamper.Next();
		}

		public int Timeout
		{
			get { return Timestamper.OneMs*60000; }
		}

		public string RegionName
		{
			get { return region; }
		}

		#endregion

		private static string GetExpirationString(IDictionary<string, string> props)
		{
			string result;
			if (!props.TryGetValue("expiration", out result))
			{
				props.TryGetValue(Environment.CacheDefaultExpiration, out result);
			}
			return result;
		}

		/// <summary>
		/// Turn the key obj into a string, preperably using human readable
		/// string, and if the string is too long (>=250) it will be hashed
		/// </summary>
		private string KeyAsString(object key)
		{
			string fullKey = FullKeyAsString(key);
			if (fullKey.Length >= 250) //max key size for memcache
			{
				return ComputeHash(fullKey, Hasher);
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
		/// <param name="hashAlgorithm">The hash algorithm used to hash the key</param>
		/// <returns>The hashed key as a string</returns>
		private static string ComputeHash(string fullKeyString, HashAlgorithm hashAlgorithm)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(fullKeyString);
			byte[] computedHash = hashAlgorithm.ComputeHash(bytes);
			return Convert.ToBase64String(computedHash);
		}

		/// <summary>
		/// Compute an alternate key hash; used as a check that the looked-up value is 
		/// in fact what has been put there in the first place.
		/// </summary>
		/// <param name="key"></param>
		/// <returns>The alternate key hash (using the MD5 algorithm)</returns>
		private string GetAlternateKeyHash(object key)
		{
			string fullKey = FullKeyAsString(key);
			if (fullKey.Length >= 250)
			{
				return ComputeHash(fullKey, Md5);
			}
			return fullKey.Replace(' ', '-');
		}
	}
}