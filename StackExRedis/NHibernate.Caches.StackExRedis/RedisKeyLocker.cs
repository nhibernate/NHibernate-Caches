using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NHibernate.Cache;
using StackExchange.Redis;

namespace NHibernate.Caches.StackExRedis
{
	/// <summary>
	/// Provides a mechanism for locking and unlocking one or many keys.
	/// </summary>
	internal partial class RedisKeyLocker
	{
		private readonly string _lockKeySuffix;
		private readonly TimeSpan _lockTimeout;
		private readonly ICacheLockValueProvider _lockValueProvider;
		private readonly IDatabase _database;
		private readonly RetryPolicy<string, Func<string>> _retryPolicy;

		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="regionName">The region name.</param>
		/// <param name="database">The Redis database.</param>
		/// <param name="configuration">The lock configuration.</param>
		public RedisKeyLocker(
			string regionName,
			IDatabase database,
			RedisCacheLockConfiguration configuration)
		{
			_database = database;
			_lockKeySuffix = configuration.KeySuffix;
			_lockTimeout = configuration.KeyTimeout;
			_lockValueProvider = configuration.ValueProvider;

			var acquireTimeout = configuration.AcquireTimeout;
			var retryTimes = configuration.RetryTimes;
			var maxRetryDelay = configuration.MaxRetryDelay;
			var minRetryDelay = configuration.MinRetryDelay;
			var lockRetryDelayProvider = configuration.RetryDelayProvider;

			_retryPolicy = new RetryPolicy<string, Func<string>>(
					retryTimes,
					acquireTimeout,
					() => lockRetryDelayProvider.GetValue(minRetryDelay, maxRetryDelay)
				)
				.ShouldRetry(s => s == null)
				.OnFaliure((totalAttempts, elapsedMs, getKeysFn) =>
					throw new CacheException("Unable to acquire cache lock: " +
					                         $"region='{regionName}', " +
					                         $"keys='{getKeysFn()}', " +
					                         $"total attempts='{totalAttempts}', " +
					                         $"total acquiring time= '{elapsedMs}ms'"));
		}

		/// <summary>
		/// Tries to lock the given key.
		/// </summary>
		/// <param name="key">The key to lock.</param>
		/// <param name="luaScript">The lua script to lock the key.</param>
		/// <param name="extraKeys">The extra keys that will be provided to the <paramref name="luaScript"/></param>
		/// <param name="extraValues">The extra values that will be provided to the <paramref name="luaScript"/></param>
		/// <returns>The lock value used to lock the key.</returns>
		/// <exception cref="CacheException">Thrown if the lock was not acquired.</exception>
		public string Lock(string key, string luaScript, RedisKey[] extraKeys, RedisValue[] extraValues)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}
			var lockKey = $"{key}{_lockKeySuffix}";
			string Context() => lockKey;

			return _retryPolicy.Execute(() =>
			{
				var lockValue = _lockValueProvider.GetValue();
				if (!string.IsNullOrEmpty(luaScript))
				{
					var keys = new RedisKey[] {lockKey};
					if (extraKeys != null)
					{
						keys = keys.Concat(extraKeys).ToArray();
					}
					var values = new RedisValue[] {lockValue, (long) _lockTimeout.TotalMilliseconds};
					if (extraValues != null)
					{
						values = values.Concat(extraValues).ToArray();
					}
					var result = (RedisValue[]) _database.ScriptEvaluate(luaScript, keys, values);
					if ((bool) result[0])
					{
						return lockValue;
					}
				}
				else if (_database.LockTake(lockKey, lockValue, _lockTimeout))
				{
					return lockValue;
				}

				return null; // retry
			}, Context);
		}

		/// <summary>
		/// Tries to lock the given keys.
		/// </summary>
		/// <param name="keys">The keys to lock.</param>
		/// <param name="luaScript">The lua script to lock the keys.</param>
		/// <param name="extraKeys">The extra keys that will be provided to the <paramref name="luaScript"/></param>
		/// <param name="extraValues">The extra values that will be provided to the <paramref name="luaScript"/></param>
		/// <returns>The lock value used to lock the keys.</returns>
		/// <exception cref="CacheException">Thrown if the lock was not acquired.</exception>
		public string LockMany(string[] keys, string luaScript, RedisKey[] extraKeys, RedisValue[] extraValues)
		{
			if (keys == null)
			{
				throw new ArgumentNullException(nameof(keys));
			}
			if (luaScript == null)
			{
				throw new ArgumentNullException(nameof(luaScript));
			}
			string Context() => string.Join(",", keys.Select(o => $"{o}{_lockKeySuffix}"));

			return _retryPolicy.Execute(() =>
			{
				var lockKeys = new RedisKey[keys.Length];
				for (var i = 0; i < keys.Length; i++)
				{
					lockKeys[i] = $"{keys[i]}{_lockKeySuffix}";
				}
				var lockValue = _lockValueProvider.GetValue();
				if (extraKeys != null)
				{
					lockKeys = lockKeys.Concat(extraKeys).ToArray();
				}
				var values = new RedisValue[] {lockValue, (long) _lockTimeout.TotalMilliseconds};
				if (extraValues != null)
				{
					values = values.Concat(extraValues).ToArray();
				}
				var result = (RedisValue[]) _database.ScriptEvaluate(luaScript, lockKeys, values);
				if ((bool) result[0])
				{
					return lockValue;
				}

				return null; // retry
			}, Context);
		}

		/// <summary>
		/// Tries to unlock the given key.
		/// </summary>
		/// <param name="key">The key to unlock.</param>
		/// <param name="lockValue">The value that was used to lock the key.</param>
		/// <param name="luaScript">The lua script to unlock the key.</param>
		/// <param name="extraKeys">The extra keys that will be provided to the <paramref name="luaScript"/></param>
		/// <param name="extraValues">The extra values that will be provided to the <paramref name="luaScript"/></param>
		/// <returns>Whether the key was unlocked.</returns>
		public bool Unlock(string key, string lockValue, string luaScript, RedisKey[] extraKeys, RedisValue[] extraValues)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}
			var lockKey = $"{key}{_lockKeySuffix}";
			if (string.IsNullOrEmpty(luaScript))
			{
				return _database.LockRelease(lockKey, lockValue);
			}
			var keys = new RedisKey[] {lockKey};
			if (extraKeys != null)
			{
				keys = keys.Concat(extraKeys).ToArray();
			}
			var values = new RedisValue[] {lockValue};
			if (extraValues != null)
			{
				values = values.Concat(extraValues).ToArray();
			}

			var result = (RedisValue[]) _database.ScriptEvaluate(luaScript, keys, values);
			return (bool) result[0];
		}

		/// <summary>
		/// Tries to unlock the given keys.
		/// </summary>
		/// <param name="keys">The keys to unlock.</param>
		/// <param name="lockValue">The value that was used to lock the keys.</param>
		/// <param name="luaScript">The lua script to unlock the keys.</param>
		/// <param name="extraKeys">The extra keys that will be provided to the <paramref name="luaScript"/></param>
		/// <param name="extraValues">The extra values that will be provided to the <paramref name="luaScript"/></param>
		/// <returns>How many keys were unlocked.</returns>
		public int UnlockMany(string[] keys, string lockValue, string luaScript, RedisKey[] extraKeys, RedisValue[] extraValues)
		{
			if (keys == null)
			{
				throw new ArgumentNullException(nameof(keys));
			}
			if (luaScript == null)
			{
				throw new ArgumentNullException(nameof(luaScript));
			}

			var lockKeys = new RedisKey[keys.Length];
			for (var i = 0; i < keys.Length; i++)
			{
				lockKeys[i] = $"{keys[i]}{_lockKeySuffix}";
			}
			if (extraKeys != null)
			{
				lockKeys = lockKeys.Concat(extraKeys).ToArray();
			}
			var values = new RedisValue[] {lockValue};
			if (extraValues != null)
			{
				values = values.Concat(extraValues).ToArray();
			}

			var result = (RedisValue[]) _database.ScriptEvaluate(luaScript, lockKeys, values);
			return (int) result[0];
		}
	}
}
