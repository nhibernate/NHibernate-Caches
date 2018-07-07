using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Cache;
using StackExchange.Redis;

namespace NHibernate.Caches.StackExRedis
{
	/// <summary>
	/// An abstract region strategy that provides common functionalities to create a region strategy.
	/// </summary>
	public abstract partial class AbstractRegionStrategy
	{
		/// <summary>
		/// The NHibernate logger.
		/// </summary>
		protected readonly INHibernateLogger Log;

		/// <summary>
		/// The Redis connection.
		/// </summary>
		protected readonly ConnectionMultiplexer ConnectionMultiplexer;

		/// <summary>
		/// The Redis database where the keys are stored.
		/// </summary>
		protected readonly IDatabase Database;

		/// <summary>
		/// The <see cref="IRedisSerializer"/> instance.
		/// </summary>
		protected readonly IRedisSerializer Serializer;

		private readonly RedisKeyLocker _keyLocker;

		/// <summary>
		/// With the current NHibernate version (5.1) the <see cref="ICache.Lock"/> method does not have a
		/// return value that would indicate which lock value was used to lock the key, that would then be
		/// passed to the <see cref="ICache.Unlock"/> method in order to prevent unlocking keys that were locked
		/// by others. Luckily, the <see cref="ICache.Lock"/> and <see cref="ICache.Unlock"/> methods are currently
		/// always called inside a lock statement which we abuse by storing the locked key when the
		/// <see cref="ICache.Lock"/> is called and use it when <see cref="ICache.Unlock"/> is called. This
		/// means that our <see cref="Lock"/> and <see cref="Unlock"/> are not thread safe and have to be called
		/// sequentially in order prevent overriding the lock key for a given key.
		/// Currently, the <see cref="ICache.Unlock"/> is always called after the <see cref="ICache.Lock"/> method
		/// even if an exception occurs which we are also abusing in order to avoid having a special mechanism to 
		/// clear the dictionary in order to prevent memory leaks.
		/// </summary>
		private readonly ConcurrentDictionary<string, string> _acquiredKeyLocks = new ConcurrentDictionary<string, string>();


		/// <summary>
		/// The constructor for creating the region strategy.
		/// </summary>
		/// <param name="connectionMultiplexer">The Redis connection.</param>
		/// <param name="configuration">The region configuration.</param>
		/// <param name="properties">The NHibernate configuration properties.</param>
		protected AbstractRegionStrategy(ConnectionMultiplexer connectionMultiplexer,
			RedisCacheRegionConfiguration configuration, IDictionary<string, string> properties)
		{
			Log = NHibernateLogger.For(GetType());
			RegionName = configuration.RegionName;
			Expiration = configuration.Expiration;
			UseSlidingExpiration = configuration.UseSlidingExpiration;
			AppendHashcode = configuration.AppendHashcode;
			RegionKey = configuration.RegionKey;
			ConnectionMultiplexer = connectionMultiplexer;
			Database = connectionMultiplexer.GetDatabase(configuration.Database);
			Serializer = configuration.Serializer;
			LockTimeout = configuration.LockConfiguration.KeyTimeout;
			_keyLocker = new RedisKeyLocker(RegionName, Database, configuration.LockConfiguration);
		}

		/// <summary>
		/// The lua script for getting a key from the cache.
		/// </summary>
		protected virtual string GetScript => null;

		/// <summary>
		/// The lua script for getting many keys from the cache at once.
		/// </summary>
		protected virtual string GetManyScript => null;

		/// <summary>
		/// The lua script for putting a key into the cache.
		/// </summary>
		protected virtual string PutScript => null;

		/// <summary>
		/// The lua script for putting many keys into the cache at once.
		/// </summary>
		protected abstract string PutManyScript { get; }

		/// <summary>
		/// The lua script for removing a key from the cache.
		/// </summary>
		protected virtual string RemoveScript => null;

		/// <summary>
		/// The lua script for removing many keys from the cache at once.
		/// </summary>
		protected virtual string RemoveManyScript => null;

		/// <summary>
		/// The lua script for locking a key.
		/// </summary>
		protected virtual string LockScript => null;

		/// <summary>
		/// The lua script for locking many keys at once.
		/// </summary>
		protected abstract string LockManyScript { get; }

		/// <summary>
		/// The lua script for unlocking a key.
		/// </summary>
		protected virtual string UnlockScript => null;

		/// <summary>
		/// The lua script for unlocking many keys at once.
		/// </summary>
		protected abstract string UnlockManyScript { get; }

		/// <summary>
		/// The expiration delay applied to cached items.
		/// </summary>
		public TimeSpan Expiration { get; }

		/// <summary>
		/// The name of the region.
		/// </summary>
		public string RegionName { get; }

		/// <summary>
		/// The key representing the region that is composed of <see cref="RedisCacheRegionConfiguration.CacheKeyPrefix"/>,
		/// <see cref="RedisCacheRegionConfiguration.EnvironmentName"/>, <see cref="RedisCacheRegionConfiguration.RegionPrefix"/>
		/// and <see cref="RedisCacheRegionConfiguration.RegionName"/>.
		/// </summary>
		public string RegionKey { get; }

		/// <summary>
		/// Should the expiration delay be sliding?
		/// </summary>
		/// <value><see langword="true" /> for resetting a cached item expiration each time it is accessed.</value>
		public bool UseSlidingExpiration { get; }

		/// <summary>
		/// Whether the hash code of the key should be added to the cache key.
		/// </summary>
		public bool AppendHashcode { get; }

		/// <summary>
		/// Is the expiration enabled?
		/// </summary>
		public bool ExpirationEnabled => Expiration != TimeSpan.Zero;

		/// <summary>
		/// The timeout of an acquired lock.
		/// </summary>
		public TimeSpan LockTimeout { get; }

		/// <summary>
		/// Gets the object that is stored in Redis by its key.
		/// </summary>
		/// <param name="key">The key of the object to retrieve.</param>
		/// <returns>The object behind the key or <see langword="null" /> if the key was not found.</returns>
		public virtual object Get(object key)
		{
			if (key == null)
			{
				return null;
			}
			var cacheKey = GetCacheKey(key);
			RedisValue result;
			if (string.IsNullOrEmpty(GetScript))
			{
				result = Database.StringGet(cacheKey);
			}
			else
			{
				var keys = AppendAdditionalKeys(new RedisKey[] { cacheKey });
				var values = AppendAdditionalValues(new RedisValue[]
				{
					UseSlidingExpiration && ExpirationEnabled,
					(long) Expiration.TotalMilliseconds
				});
				var results = (RedisValue[]) Database.ScriptEvaluate(GetScript, keys, values);
				result = results[0];
			}
			return result.IsNullOrEmpty ? null : Serializer.Deserialize(result);
		}

		/// <summary>
		/// Gets the objects that are stored in Redis by their key.
		/// </summary>
		/// <param name="keys">The keys of the objects to retrieve.</param>
		/// <returns>An array of objects behind the keys or <see langword="null" /> if the key was not found.</returns>
		public virtual object[] GetMany(object[] keys)
		{
			if (keys == null)
			{
				return null;
			}
			var cacheKeys = new RedisKey[keys.Length];
			for (var i = 0; i < keys.Length; i++)
			{
				cacheKeys[i] = GetCacheKey(keys[i]);
			}
			RedisValue[] results;
			if (string.IsNullOrEmpty(GetManyScript))
			{
				results = Database.StringGet(cacheKeys);
			}
			else
			{
				cacheKeys = AppendAdditionalKeys(cacheKeys);
				var values = AppendAdditionalValues(new RedisValue[]
				{
					UseSlidingExpiration && ExpirationEnabled,
					(long) Expiration.TotalMilliseconds
				});
				results = (RedisValue[]) Database.ScriptEvaluate(GetManyScript, cacheKeys, values);
			}

			var objects = new object[keys.Length];
			for (var i = 0; i < results.Length; i++)
			{
				var result = results[i];
				if (!result.IsNullOrEmpty)
				{
					objects[i] = Serializer.Deserialize(result);
				}
			}
			return objects;
		}

		/// <summary>
		/// Stores the object into Redis by the given key.
		/// </summary>
		/// <param name="key">The key to store the object.</param>
		/// <param name="value">The object to store.</param>
		public virtual void Put(object key, object value)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}
			var cacheKey = GetCacheKey(key);
			RedisValue serializedValue = Serializer.Serialize(value);

			if (string.IsNullOrEmpty(PutScript))
			{
				Database.StringSet(cacheKey, serializedValue, ExpirationEnabled ? Expiration : (TimeSpan?) null);
				return;
			}

			var keys = AppendAdditionalKeys(new RedisKey[] {cacheKey});
			var values = AppendAdditionalValues(new[]
			{
				serializedValue,
				ExpirationEnabled,
				(long)Expiration.TotalMilliseconds
			});
			Database.ScriptEvaluate(PutScript, keys, values);
		}

		/// <summary>
		/// Stores the objects into Redis by the given keys.
		/// </summary>
		/// <param name="keys">The keys to store the objects.</param>
		/// <param name="values">The objects to store.</param>
		public virtual void PutMany(object[] keys, object[] values)
		{
			if (keys == null)
			{
				throw new ArgumentNullException(nameof(keys));
			}
			if (values == null)
			{
				throw new ArgumentNullException(nameof(values));
			}
			if (keys.Length != values.Length)
			{
				throw new ArgumentException($"Length of {nameof(keys)} array does not match with {nameof(values)} array.");
			}
			if (string.IsNullOrEmpty(PutManyScript))
			{
				if (ExpirationEnabled)
				{
					throw new NotSupportedException($"{nameof(PutMany)} operation is not supported.");
				}
				var pairs = new KeyValuePair<RedisKey,RedisValue>[keys.Length];
				for (var i = 0; i < keys.Length; i++)
				{
					pairs[i] = new KeyValuePair<RedisKey, RedisValue>(GetCacheKey(keys[i]), Serializer.Serialize(values[i]));
				}
				Database.StringSet(pairs);
				return;
			}
			

			var cacheKeys = new RedisKey[keys.Length];
			var cacheValues = new RedisValue[keys.Length + 2];
			for (var i = 0; i < keys.Length; i++)
			{
				cacheKeys[i] = GetCacheKey(keys[i]);
				cacheValues[i] = Serializer.Serialize(values[i]);
			}
			cacheKeys = AppendAdditionalKeys(cacheKeys);
			cacheValues[keys.Length] = ExpirationEnabled;
			cacheValues[keys.Length + 1] = (long) Expiration.TotalMilliseconds;
			cacheValues = AppendAdditionalValues(cacheValues);
			Database.ScriptEvaluate(PutManyScript, cacheKeys, cacheValues);
		}

		/// <summary>
		/// Removes the key from Redis.
		/// </summary>
		/// <param name="key">The key to remove.</param>
		public virtual bool Remove(object key)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			var cacheKey = GetCacheKey(key);
			if (string.IsNullOrEmpty(RemoveScript))
			{
				return Database.KeyDelete(cacheKey);
			}
			var keys = AppendAdditionalKeys(new RedisKey[] { cacheKey });
			var values = GetAdditionalValues();
			var results = (RedisValue[]) Database.ScriptEvaluate(RemoveScript, keys, values);
			return (bool) results[0];
		}

		/// <summary>
		/// Removes many keys from Redis at once.
		/// </summary>
		/// <param name="keys">The keys to remove.</param>
		public virtual long RemoveMany(object[] keys)
		{
			if (keys == null)
			{
				throw new ArgumentNullException(nameof(keys));
			}
			var cacheKeys = new RedisKey[keys.Length];
			for (var i = 0; i < keys.Length; i++)
			{
				cacheKeys[i] = GetCacheKey(keys[i]);
			}
			if (string.IsNullOrEmpty(RemoveManyScript))
			{
				return Database.KeyDelete(cacheKeys);
			}
			cacheKeys = AppendAdditionalKeys(cacheKeys);
			var results = (RedisValue[]) Database.ScriptEvaluate(RemoveManyScript, cacheKeys, GetAdditionalValues());
			return (long) results[0];
		}

		/// <summary>
		/// Locks the key.
		/// </summary>
		/// <param name="key">The key to lock.</param>
		/// <returns>The value used to lock the key.</returns>
		public virtual string Lock(object key)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}
			var cacheKey = GetCacheKey(key);
			var lockValue = _keyLocker.Lock(cacheKey, LockScript, GetAdditionalKeys(), GetAdditionalValues());

			_acquiredKeyLocks.AddOrUpdate(cacheKey, _ => lockValue, (_, currValue) =>
			{
				Log.Warn(
					$"Calling {nameof(Lock)} method for key:'{cacheKey}' that was already locked. " +
					$"{nameof(Unlock)} method must be called after each {nameof(Lock)} call for " +
					$"the same key prior calling {nameof(Lock)} again with the same key.");
				return lockValue;
			});
			return lockValue;
		}

		/// <summary>
		/// Locks many keys at once.
		/// </summary>
		/// <param name="keys">The keys to lock.</param>
		/// <returns>The value used to lock the keys.</returns>
		public virtual string LockMany(object[] keys)
		{
			if (keys == null)
			{
				throw new ArgumentNullException(nameof(keys));
			}
			if (string.IsNullOrEmpty(LockManyScript))
			{
				throw new NotSupportedException($"{nameof(LockMany)} operation is not supported.");
			}
			var cacheKeys = new string[keys.Length];
			for (var i = 0; i < keys.Length; i++)
			{
				cacheKeys[i] = GetCacheKey(keys[i]);
			}
			return _keyLocker.LockMany(cacheKeys, LockManyScript, GetAdditionalKeys(), GetAdditionalValues());
		}

		/// <summary>
		/// Unlocks the key.
		/// </summary>
		/// <param name="key">The key to unlock.</param>
		/// <returns>Whether the key was unlocked</returns>
		public virtual bool Unlock(object key)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}
			var cacheKey = GetCacheKey(key);

			if (!_acquiredKeyLocks.TryRemove(cacheKey, out var lockValue))
			{
				Log.Warn(
					$"Calling {nameof(Unlock)} method for key:'{cacheKey}' that was not locked with {nameof(Lock)} method before.");
				return false;
			}
			return _keyLocker.Unlock(cacheKey, lockValue, UnlockScript, GetAdditionalKeys(), GetAdditionalValues());
		}

		/// <summary>
		/// Unlocks many keys at once.
		/// </summary>
		/// <param name="keys">The keys to unlock.</param>
		/// <param name="lockValue">The value used to lock the keys</param>
		/// <returns>The number of unlocked keys.</returns>
		public virtual int UnlockMany(object[] keys, string lockValue)
		{
			if (keys == null)
			{
				throw new ArgumentNullException(nameof(keys));
			}
			if (string.IsNullOrEmpty(UnlockManyScript))
			{
				throw new NotSupportedException($"{nameof(UnlockMany)} operation is not supported.");
			}
			var cacheKeys = new string[keys.Length];
			for (var i = 0; i < keys.Length; i++)
			{
				var cacheKey = GetCacheKey(keys[i]);
				cacheKeys[i] = cacheKey;
			}
			return _keyLocker.UnlockMany(cacheKeys, lockValue, UnlockManyScript, GetAdditionalKeys(), GetAdditionalValues());
		}

		/// <summary>
		/// Clears all the keys from the region.
		/// </summary>
		public abstract void Clear();

		/// <summary>
		/// Validates if the region strategy was correctly configured.
		/// </summary>
		/// <exception cref="CacheException">Thrown when the region strategy is not configured correctly.</exception>
		public abstract void Validate();

		/// <summary>
		/// Gets additional values required by the concrete strategy that can be used in the lua scripts.
		/// </summary>
		/// <returns>The values to be used in the lua scripts.</returns>
		protected virtual RedisValue[] GetAdditionalValues()
		{
			return null;
		}

		/// <summary>
		/// Gets additional keys required by the concrete strategy that can be used in the lua scripts.
		/// </summary>
		/// <returns>The keys to be used in the lua scripts.</returns>
		protected virtual RedisKey[] GetAdditionalKeys()
		{
			return null;
		}

		/// <summary>
		/// Calculates the cache key for the given object.
		/// </summary>
		/// <param name="value">The object for which the key will be calculated.</param>
		/// <returns>The key for the given object.</returns>
		protected virtual string GetCacheKey(object value)
		{
			// Hash tag (wrap with curly brackets) the region key in order to ensure that all region keys
			// will be located on the same server, when a Redis cluster is used.
			return AppendHashcode
				? string.Concat("{", RegionKey, "}:", value.ToString(), "@", value.GetHashCode())
				: string.Concat("{", RegionKey, "}:", value.ToString());
		}

		/// <summary>
		/// Combine the given values with the values returned from <see cref="GetAdditionalValues"/>.
		/// </summary>
		/// <param name="values">The values to combine with the additional values.</param>
		/// <returns>An array of combined values.</returns>
		protected RedisValue[] AppendAdditionalValues(RedisValue[] values)
		{
			if (values == null)
			{
				return GetAdditionalValues();
			}
			var additionalValues = GetAdditionalValues();
			if (additionalValues == null)
			{
				return values;
			}
			var combinedValues = new RedisValue[values.Length + additionalValues.Length];
			values.CopyTo(combinedValues, 0);
			additionalValues.CopyTo(combinedValues, values.Length);
			return combinedValues;
		}

		/// <summary>
		/// Combine the given keys with the keys returned from <see cref="GetAdditionalKeys"/>.
		/// </summary>
		/// <param name="keys">The keys to combine with the additional keys.</param>
		/// <returns>An array of combined keys.</returns>
		protected RedisKey[] AppendAdditionalKeys(RedisKey[] keys)
		{
			if (keys == null)
			{
				return GetAdditionalKeys();
			}
			var additionalKeys = GetAdditionalKeys();
			if (additionalKeys == null)
			{
				return keys;
			}
			var combinedKeys = new RedisKey[keys.Length + additionalKeys.Length];
			keys.CopyTo(combinedKeys, 0);
			additionalKeys.CopyTo(combinedKeys, keys.Length);
			return combinedKeys;
		}
	}
}
