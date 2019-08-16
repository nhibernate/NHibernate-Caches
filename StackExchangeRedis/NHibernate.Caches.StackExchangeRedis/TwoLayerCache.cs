using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Caches.Common;
using NHibernate.Caches.StackExchangeRedis.Messages;
using StackExchange.Redis;

namespace NHibernate.Caches.StackExchangeRedis
{
	internal partial class TwoLayerCache
	{
		private static readonly string GetManyTimeToLiveLuaScript;

		static TwoLayerCache()
		{
			GetManyTimeToLiveLuaScript = LuaScriptProvider.GetScript("GetManyTimeToLive");
		}

		private readonly RegionMemoryCacheBase _memoryCache;
		private readonly int _clientId;
		private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
		private readonly RedisValue _invalidationChannel;
		private readonly INHibernateLogger _log;
		private readonly CacheSerializerBase _serializer;
		private readonly IDatabase _database;
		private readonly bool _expirationEnabled;
		private readonly bool _useSlidingExpiration;
		private readonly string _putScript;
		private readonly string _removeScript;
		private readonly TimeSpan _expiration;
		private readonly Func<RedisKey[], RedisKey[]> _appendAdditionalKeys;
		private readonly Func<RedisValue[], RedisValue[]> _appendAdditionalValues;
		private readonly Func<string, object> _redisGet;
		private readonly Func<string, CancellationToken, Task<object>> _redisGetAsync;
		private readonly Func<RedisKey[], object[]> _redisGetMany;
		private readonly Func<RedisKey[], CancellationToken, Task<object[]>> _redisGetManyAsync;
		private readonly Action<string> _logErrorMessage;
		private readonly TimeSpan _maxSynchronizationTime;
		private readonly bool _usePipelining;
		private readonly string _regionKey;
		private long _version;
		private string _versionKey;

		public TwoLayerCache(TwoLayerCacheConfiguration configuration)
		{
			_log = configuration.Log;
			_memoryCache = configuration.MemoryCache;
			_clientId = configuration.ClientId;
			_serializer = configuration.Serializer;
			_database = configuration.Database;
			_expirationEnabled = configuration.ExpirationEnabled;
			_useSlidingExpiration = configuration.UseSlidingExpiration;
			_expiration = configuration.Expiration;
			_appendAdditionalKeys = configuration.AppendAdditionalKeys;
			_appendAdditionalValues = configuration.AppendAdditionalValues;
			_redisGet = configuration.RedisGet;
			_redisGetAsync = configuration.RedisGetAsync;
			_redisGetMany = configuration.RedisGetMany;
			_redisGetManyAsync = configuration.RedisGetManyAsync;
			_putScript = configuration.PutScript;
			_removeScript = configuration.RemoveScript;
			_logErrorMessage = configuration.LogErrorMessage;
			_regionKey = configuration.RegionKey;
			_usePipelining = configuration.UsePipelining;
			_maxSynchronizationTime = configuration.MaxSynchronizationTime;

			var connectionMultiplexer = configuration.ConnectionMultiplexer;
			var invalidationChannel = string.Concat("{", configuration.RegionKey, "}@", "Invalidation");
			_invalidationChannel = invalidationChannel;
			connectionMultiplexer.GetSubscriber().Subscribe(invalidationChannel).OnMessage((Action<ChannelMessage>) OnInvalidationMessage);
			connectionMultiplexer.ConnectionFailed += OnConnectionFailed;
			connectionMultiplexer.ErrorMessage += OnErrorMessage;
			connectionMultiplexer.ConnectionRestored += OnConnectionRestored;
		}

		public void Put(RedisKey cacheKey, object value)
		{
			PutLocal(cacheKey, value, true);
		}

		public object Get(string cacheKey)
		{
			var value = GetLocal(cacheKey);
			if (value != null)
			{
				return value;
			}

			var version = _version;
			_log.Debug("Object was not found in local cache, fetching it from Redis.");
			value = ExecuteGet(cacheKey);
			if (value == null)
			{
				return null;
			}

			TimeSpan? timeToLive = null;
			if (_expirationEnabled && !_useSlidingExpiration)
			{
				// We have to be careful as the key may be removed between base.Get and Database.KeyTimeToLive calls.
				// In that case, timeToLive will be null
				timeToLive = _database.KeyTimeToLive(cacheKey);
				if (!timeToLive.HasValue)
				{
					return null;
				}
			}

			return AddAndGet(cacheKey, value, version, timeToLive);
		}

		public object[] GetMany(RedisKey[] cacheKeys)
		{
			int i;
			var values = new object[cacheKeys.Length];
			List<KeyValuePair<int, RedisKey>> missingCacheKeys = null;
			for (i = 0; i < cacheKeys.Length; i++)
			{
				values[i] = GetLocal(cacheKeys[i]);
				if (values[i] != null)
				{
					continue;
				}

				if (missingCacheKeys == null)
				{
					missingCacheKeys = new List<KeyValuePair<int, RedisKey>>();
				}

				missingCacheKeys.Add(new KeyValuePair<int, RedisKey>(i, cacheKeys[i]));
			}

			if (missingCacheKeys == null)
			{
				return values;
			}

			RedisKey[] missingKeys;
			if (missingCacheKeys.Count == cacheKeys.Length)
			{
				missingKeys = cacheKeys;
			}
			else
			{
				missingKeys = new RedisKey[missingCacheKeys.Count];
				for (i = 0; i < missingCacheKeys.Count; i++)
				{
					missingKeys[i] = cacheKeys[missingCacheKeys[i].Key];
				}
			}

			var version = _version;
			RedisValue[] timesToLive = null;
			var redisValues = ExecuteGetMany(missingKeys);
			if (_expirationEnabled && !_useSlidingExpiration)
			{
				timesToLive = (RedisValue[]) _database.ScriptEvaluate(GetManyTimeToLiveLuaScript, missingKeys);
			}

			for (i = 0; i < missingKeys.Length; i++)
			{
				if (redisValues[i] == null)
				{
					continue;
				}

				TimeSpan? timeToLive = null;
				if (timesToLive != null)
				{
					if (!timesToLive[i].HasValue)
					{
						// The key was removed in the meantime
						values[missingCacheKeys[i].Key] = null;
						continue;
					}

					timeToLive = TimeSpan.FromMilliseconds((long) timesToLive[i]);
				}

				values[missingCacheKeys[i].Key] = AddAndGet(missingKeys[i], redisValues[i], version, timeToLive) ?? redisValues[i];
			}

			return values;
		}

		public bool Remove(string cacheKey)
		{
			return RemoveLocal(cacheKey, true);
		}

		public void OnVersionUpdate(long oldVersion, long newVersion)
		{
			_writeLock.Wait();
			try
			{
				ClearLocal(_versionKey);
				_version = newVersion;
				_versionKey = GetVersionKey(newVersion);
				_memoryCache.Put(_versionKey, newVersion, TimeSpan.Zero);
			}
			finally
			{
				_writeLock.Release();
			}
		}

		private object ExecuteGet(string cacheKey)
		{
			return _redisGet(cacheKey);
		}

		private Task<object> ExecuteGetAsync(string cacheKey, CancellationToken cancellationToken)
		{
			return _redisGetAsync(cacheKey, cancellationToken);
		}

		private object[] ExecuteGetMany(RedisKey[] cacheKeys)
		{
			return _redisGetMany(cacheKeys);
		}

		private Task<object[]> ExecuteGetManyAsync(RedisKey[] cacheKeys, CancellationToken cancellationToken)
		{
			return _redisGetManyAsync(cacheKeys, cancellationToken);
		}

		private void ExecutePut(RedisKey cacheKey, object value, byte[] message)
		{
			_database.ScriptEvaluate(
				_putScript,
				_appendAdditionalKeys(new[] {cacheKey}),
				_appendAdditionalValues(new RedisValue[]
				{
					_serializer.Serialize(value),
					_expirationEnabled,
					(long) _expiration.TotalMilliseconds,
					_invalidationChannel,
					message
				}), _usePipelining ? CommandFlags.FireAndForget : CommandFlags.None);
		}

		private Task ExecutePutAsync(RedisKey cacheKey, object value, byte[] message)
		{
			if (_usePipelining)
			{
				ExecutePut(cacheKey, value, message);
				return Task.CompletedTask;
			}

			return _database.ScriptEvaluateAsync(
				_putScript,
				_appendAdditionalKeys(new[] {cacheKey}),
				_appendAdditionalValues(new RedisValue[]
				{
					_serializer.Serialize(value),
					_expirationEnabled,
					(long) _expiration.TotalMilliseconds,
					_invalidationChannel,
					message
				}));
		}

		private void ExecuteRemove(string cacheKey, byte[] message)
		{
			_database.ScriptEvaluate(
				_removeScript,
				_appendAdditionalKeys(new RedisKey[] {cacheKey}),
				_appendAdditionalValues(new RedisValue[]
				{
					_invalidationChannel,
					message
				}),
				_usePipelining ? CommandFlags.FireAndForget : CommandFlags.None);
		}

		private Task ExecuteRemoveAsync(string cacheKey, byte[] message)
		{
			if (_usePipelining)
			{
				ExecuteRemove(cacheKey, message);
				return Task.CompletedTask;
			}

			return _database.ScriptEvaluateAsync(
				_removeScript,
				_appendAdditionalKeys(new RedisKey[] {cacheKey}),
				_appendAdditionalValues(new RedisValue[]
				{
					_invalidationChannel,
					message
				}));
		}

		private object GetLocal(string key)
		{
			var cacheValue = (CacheValue) _memoryCache.Get(key);
			if (cacheValue == null || _version != cacheValue.Version)
			{
				return null;
			}

			return cacheValue.Value;
		}

		private bool RemoveLocal(string cacheKey, bool publish)
		{
			return ExecuteOperation(cacheKey, null, publish, true);
		}

		private void PutLocal(string cacheKey, object value, bool publish)
		{
			ExecuteOperation(cacheKey, value, publish, false);
		}

		private bool ExecuteOperation(string cacheKey, object value, bool publish, bool remove)
		{
			var invalidationMessage = publish
				? _serializer.Serialize(new CacheKeyInvalidationMessage
				{
					ClientId = _clientId,
					Key = cacheKey
				})
				: null;

			var cacheValue = (CacheValue) _memoryCache.Get(cacheKey);
			if (cacheValue == null && TryPutLocal())
			{
				return !remove;
			}

			var lockValue = cacheValue.Lock;
			lockValue.Wait();
			try
			{
				cacheValue = (CacheValue) _memoryCache.Get(cacheKey);

				// When a different thread calls TryPutLocal at the begining of the method
				// and gets the lock before the current thread we have to retry to put as the lock
				// value is not valid anymore
				if (cacheValue == null && TryPutLocal()) // The key expired in the meantime
				{
					return !remove;
				}

				if (lockValue != cacheValue.Lock)
				{
					return false;
				}

				if (cacheValue is RemovedCacheValue)
				{
					if (remove)
					{
						cacheValue.Version = _version;
					}
					else
					{
						cacheValue = new CacheValue(value, _version, cacheValue.Lock);
					}
				}
				else
				{
					if (remove)
					{
						cacheValue = new RemovedCacheValue(_version, cacheValue.Lock);
					}
					else
					{
						cacheValue.Value = value;
						cacheValue.Version = _version;
					}
				}

				return PutAndPublish();
			}
			finally
			{
				lockValue.Release();
			}

			bool TryPutLocal()
			{
				_writeLock.Wait();
				try
				{
					cacheValue = (CacheValue) _memoryCache.Get(cacheKey);
					if (cacheValue != null)
					{
						return false;
					}

					return PutAndPublish();
				}
				finally
				{
					_writeLock.Release();
				}
			}

			bool PutAndPublish()
			{
				if (remove)
				{
					RemoveLocal(cacheKey, new RemovedCacheValue(_version));
				}
				else
				{
					PutLocal(cacheKey, new CacheValue(value, _version));
				}

				if (publish)
				{
					ExecuteOperation(cacheKey, value, invalidationMessage, remove);
				}

				return true;
			}
		}

		private void ExecuteOperation(string cacheKey, object value, byte[] message, bool remove)
		{
			if (remove)
			{
				ExecuteRemove(cacheKey, message);
			}
			else
			{
				ExecutePut(cacheKey, value, message);
			}
		}

		private void PutLocal(string cacheKey, CacheValue cacheValue)
		{
			if (_versionKey != null)
			{
				_memoryCache.Put(cacheKey, _versionKey, cacheValue);
			}
			else
			{
				_memoryCache.Put(cacheKey, cacheValue);
			}
		}

		private void PutLocal(string cacheKey, CacheValue cacheValue, TimeSpan expiration)
		{
			if (_versionKey != null)
			{
				_memoryCache.Put(cacheKey, _versionKey, cacheValue, expiration);
			}
			else
			{
				_memoryCache.Put(cacheKey, cacheValue, expiration);
			}
		}

		private void RemoveLocal(string cacheKey, CacheValue cacheValue)
		{
			PutLocal(cacheKey, cacheValue, _maxSynchronizationTime);
		}

		private void ClearLocal()
		{
			_writeLock.Wait();
			try
			{
				ClearLocal(_versionKey);
			}
			finally
			{
				_writeLock.Release();
			}
		}

		private void ClearLocal(string dependencyKey)
		{
			if (dependencyKey != null)
			{
				_memoryCache.Remove(dependencyKey);
			}
		}

		private void OnConnectionFailed(object sender, ConnectionFailedEventArgs e)
		{
			_log.Error(e.Exception, "The connection to Redis was lost.");
		}

		private void OnConnectionRestored(object sender, ConnectionFailedEventArgs e)
		{
			_log.Error(e.Exception, "The connection to Redis is restored, clearing the local cache.");
			// Clear the local cache as we don't know the state of the Redis database
			ClearLocal();
		}

		private void OnErrorMessage(object sender, RedisErrorEventArgs e)
		{
			if (_logErrorMessage != null)
			{
				_logErrorMessage(e.Message);
			}
			else
			{
				_log.Error("An error occurred while executing a Redis command: {0}", e.Message);
			}

			// As we don't know for which key the error occurred, we have to clear the local cache
			ClearLocal();
		}

		private void OnInvalidationMessage(ChannelMessage channelMessage)
		{
			var message = (CacheKeyInvalidationMessage) _serializer.Deserialize(channelMessage.Message);
			if (message.ClientId == _clientId)
			{
				return;
			}

			// Prevent AddAndGet from adding a value after the key was invalidated
			RemoveLocal(message.Key, false);
		}

		private object AddAndGet(string cacheKey, object value, long version, TimeSpan? timeToLive)
		{
			_writeLock.Wait();
			try
			{
				if (_version != version)
				{
					return null;
				}

				var cacheValue = (CacheValue) _memoryCache.Get(cacheKey);
				if (cacheValue != null)
				{
					// The key was added in the meantime
					return cacheValue.Value ?? value;
				}

				cacheValue = new CacheValue(value, version, new SemaphoreSlim(1, 1));
				if (timeToLive.HasValue)
				{
					PutLocal(cacheKey, cacheValue, timeToLive.Value);
				}
				else
				{
					PutLocal(cacheKey, cacheValue);
				}
			}
			finally
			{
				_writeLock.Release();
			}

			return value;
		}

		private string GetVersionKey(long version)
		{
			return string.Concat(_regionKey, "-", version);
		}

		private class RemovedCacheValue : CacheValue
		{
			public RemovedCacheValue(long version) : base(null, version)
			{
			}

			public RemovedCacheValue(long version, SemaphoreSlim lockValue) : base(null, version, lockValue)
			{
			}
		}

		private class CacheValue
		{
			public CacheValue(object value, long version) : this(value, version, new SemaphoreSlim(1, 1))
			{
			}

			public CacheValue(object value, long version, SemaphoreSlim lockValue)
			{
				Value = value;
				Version = version;
				Lock = lockValue;
			}

			public long Version { get; set; }

			public object Value { get; set; }

			public SemaphoreSlim Lock { get; }
		}
	}
}
