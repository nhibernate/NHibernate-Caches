using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Cache;
using NHibernate.Caches.StackExchangeRedis.Messages;
using StackExchange.Redis;
using static NHibernate.Caches.StackExchangeRedis.ConfigurationHelper;

namespace NHibernate.Caches.StackExchangeRedis
{
	/// <summary>
	/// A strategy that uses only a memory cache to store the values and uses Redis pubsub mechanism to synchronize
	/// data between other local caches. The synchronization between caches is done by comparing the UTC <see cref="DateTime.Ticks"/>,
	/// which represent when the operation was performed. When two operations have the same <see cref="DateTime.Ticks"/>, then the client
	/// with the highest id wins. This strategy should be used only for regions that have few write operations and a high expiration time.
	/// It is recommended to use <see cref="TwoLayerCacheRegionStrategy"/>, when the instances where the strategy would run are ofter restarted/recycled.
	/// </summary>
	public partial class DistributedLocalCacheRegionStrategy : AbstractRegionStrategy
	{
		private readonly int _clientId;
		private readonly ISubscriber _subscriber;
		private readonly RegionMemoryCacheBase _memoryCache;
		private readonly ICacheLockValueProvider _lockValueProvider;
		private readonly string _lockKeySuffix;
		private readonly TimeSpan _lockAcquireTimeout;
		private readonly TimeSpan _lockKeyTimeout;
		private readonly RetryPolicy<string, Func<object>> _retryPolicy;
		private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
		private readonly object _lockLock = new object();
		private readonly RedisChannel _synchronizationChannel;
		private readonly TimeSpan _maxSynchronizationTime;
		private readonly bool _usePipelining;
		private long _lastClearTimestamp;
		private int _lastClearClientId;
		private long _version;

		/// <inheritdoc />
		public DistributedLocalCacheRegionStrategy(
			IConnectionMultiplexer connectionMultiplexer,
			RedisCacheRegionConfiguration configuration,
			RegionMemoryCacheBase memoryCache,
			IDictionary<string, string> properties)
			: base(connectionMultiplexer, configuration, properties)
		{
			var lockConfiguration = configuration.LockConfiguration;
			var acquireTimeout = lockConfiguration.AcquireTimeout;
			var retryTimes = lockConfiguration.RetryTimes;
			var maxRetryDelay = lockConfiguration.MaxRetryDelay;
			var minRetryDelay = lockConfiguration.MinRetryDelay;
			var lockRetryDelayProvider = lockConfiguration.RetryDelayProvider;

			_usePipelining = GetBoolean("cache.region_strategy.distributed_local_cache.use_pipelining", properties, false);
			Log.Debug("Use pipelining for region {0}: {1}", RegionName, _usePipelining);

			_clientId = GetInteger("cache.region_strategy.distributed_local_cache.client_id", properties, Guid.NewGuid().GetHashCode());
			Log.Debug("Client id for region {0}: {1}", RegionName, _clientId);

			_maxSynchronizationTime = GetTimeSpanFromSeconds(
				"cache.region_strategy.distributed_local_cache.max_synchronization_time", properties, TimeSpan.FromSeconds(10));
			Log.Debug("Max synchronization time for region {0}: {1} seconds", RegionName, _maxSynchronizationTime.TotalSeconds);

			_memoryCache = memoryCache;
			_synchronizationChannel = string.Concat("{", configuration.RegionKey, "}@", "Synchronization");
			_lockValueProvider = lockConfiguration.ValueProvider;
			_lockKeySuffix = lockConfiguration.KeySuffix;
			_lockAcquireTimeout = lockConfiguration.AcquireTimeout;
			_lockKeyTimeout = lockConfiguration.KeyTimeout;
			_retryPolicy = new RetryPolicy<string, Func<object>>(
					retryTimes,
					acquireTimeout,
					() => lockRetryDelayProvider.GetValue(minRetryDelay, maxRetryDelay)
				)
				.ShouldRetry(s => s == null)
				.OnFailure(OnFailedLock);
			_subscriber = ConnectionMultiplexer.GetSubscriber();

			ConnectionMultiplexer.ConnectionFailed += OnConnectionFailed;
			ConnectionMultiplexer.ConnectionRestored += OnConnectionRestored;
			ConnectionMultiplexer.ErrorMessage += OnErrorMessage;
			_subscriber.Subscribe(_synchronizationChannel).OnMessage((Action<ChannelMessage>) OnSynchronizationMessage);
			_subscriber.Subscribe(GetClientChannel(_clientId)).OnMessage((Action<ChannelMessage>) OnPrivateMessage);
		}

		/// <inheritdoc />
		protected override string PutManyScript => null;

		/// <inheritdoc />
		protected override string LockManyScript => null;

		/// <inheritdoc />
		protected override string UnlockManyScript => null;

		/// <inheritdoc />
		protected override void ExecutePut(string cacheKey, object value)
		{
			TryPutLocal(cacheKey, value, DateTime.UtcNow.Ticks, _clientId, true);
		}

		/// <inheritdoc />
		protected override void ExecutePutMany(object[] keys, object[] values)
		{
			for (var i = 0; i < keys.Length; i++)
			{
				Put(keys[i], values[i]);
			}
		}

		/// <inheritdoc />
		protected override Task ExecutePutManyAsync(object[] keys, object[] values, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<bool>(cancellationToken);
			}
			try
			{
				PutMany(keys, values);
				return Task.CompletedTask;
			}
			catch (Exception ex)
			{
				return Task.FromException<bool>(ex);
			}
		}

		/// <inheritdoc />
		public override object Get(object key)
		{
			if (key == null)
			{
				return null;
			}

			var cacheKey = GetCacheKey(key);
			return GetLocal(cacheKey);
		}

		/// <inheritdoc />
		public override object[] GetMany(object[] keys)
		{
			if (keys == null)
			{
				throw new ArgumentNullException(nameof(keys));
			}

			var values = new object[keys.Length];
			for (var i = 0; i < keys.Length; i++)
			{
				values[i] = Get(keys[i]);
			}

			return values;
		}

		/// <inheritdoc />
		public override Task<object[]> GetManyAsync(object[] keys, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object[]>(cancellationToken);
			}
			try
			{
				return Task.FromResult(GetMany(keys));
			}
			catch (Exception ex)
			{
				return Task.FromException<object[]>(ex);
			}
		}

		/// <inheritdoc />
		protected override bool ExecuteRemove(string cacheKey)
		{
			return TryRemoveLocal(cacheKey, DateTime.UtcNow.Ticks, _clientId, true);
		}

		/// <inheritdoc />
		public override void Clear()
		{
			Log.Debug("Clearing region: '{0}'.", RegionKey);
			TryClearLocal(DateTime.UtcNow.Ticks, _clientId, true);
		}

		/// <inheritdoc />
		public override string Lock(object key)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			var lockKey = GetLockKey(key);
			Log.Debug("Locking key: '{0}'.", lockKey);
			var lockValue = _lockValueProvider.GetValue();
			using (var cacheLockValue = new CacheLockValue(lockValue))
			{
				object Context() => new KeyValuePair<string, CacheLockValue>(lockKey, cacheLockValue);
				return _retryPolicy
					.Execute(() =>
					{
						if (!LockLocal(lockKey, cacheLockValue))
						{
							Log.Debug("Failed to acquire lock for key '{0}' in the local cache, retrying...", lockKey);
							return null; // Retry
						}

						cacheLockValue.Setup();
						var subscriberCount = _subscriber.Publish(_synchronizationChannel, Serializer.Serialize(new CacheSynchronizationMessage
						{
							OperationType = OperationType.Lock,
							Timestamp = DateTime.UtcNow.Ticks,
							ClientId = _clientId,
							Data = new LockData
							{
								LockKey = lockKey,
								LockValue = lockValue
							}
						})) - 1;

						if (subscriberCount == 0)
						{
							Log.Debug("Acquired lock for key '{0}', no other caches were involved.", lockKey);
							// We are the only one subscribed
							return cacheLockValue.Value;
						}

						Log.Debug("Waiting lock result from '{0}' other local caches.", subscriberCount);
						IncreaseLock(cacheLockValue, subscriberCount);
						if (!cacheLockValue.Semaphore.Wait(_lockAcquireTimeout) || cacheLockValue.Failed)
						{
							Log.Debug("Failed to acquire lock for key '{0}' from '{1}' other local caches, retrying...", lockKey, subscriberCount);
							return null;
						}

						Log.Debug("Acquired lock for key '{0}', '{1}' other caches were involved.", lockKey, subscriberCount);
						return cacheLockValue.Value;
					}, Context);
			}
		}

		/// <inheritdoc />
		public override string LockMany(object[] keys)
		{
			if (keys == null)
			{
				throw new ArgumentNullException(nameof(keys));
			}

			var lockKeys = new string[keys.Length];
			var lockValue = _lockValueProvider.GetValue();
			for (var i = 0; i < keys.Length; i++)
			{
				lockKeys[i] = GetLockKey(keys[i]);
				Log.Debug("Locking key: '{0}'.", lockKeys[i]);
			}

			using (var cacheLockValue = new CacheLockValue(lockValue))
			{
				object Context() => new KeyValuePair<string[], CacheLockValue>(lockKeys, cacheLockValue);
				return _retryPolicy
					.Execute(() =>
					{
						if (!LockManyLocal(lockKeys, cacheLockValue))
						{
							Log.Debug("Failed to acquire lock for '{0}' keys in the local cache, retrying...", lockKeys.Length);
							return null; // Retry
						}

						cacheLockValue.Setup();
						var subscriberCount = _subscriber.Publish(_synchronizationChannel, Serializer.Serialize(new CacheSynchronizationMessage
						{
							OperationType = OperationType.LockMany,
							Timestamp = DateTime.UtcNow.Ticks,
							ClientId = _clientId,
							Data = new LockManyData
							{
								LockKeys = lockKeys,
								LockValue = lockValue
							}
						})) - 1;

						if (subscriberCount == 0)
						{
							Log.Debug("Acquired lock for '{0}' keys, no other caches were involved.", lockKeys.Length);
							// We are the only one subscribed
							return cacheLockValue.Value;
						}

						Log.Debug("Waiting lock result from '{0}' other local caches.", subscriberCount);
						IncreaseLock(cacheLockValue, subscriberCount);
						if (!cacheLockValue.Semaphore.Wait(_lockAcquireTimeout) || cacheLockValue.Failed)
						{
							Log.Debug("Failed to acquire lock for '{0}' keys from '{1}' other local caches, retrying...", lockKeys.Length, subscriberCount);
							return null;
						}

						Log.Debug("Acquired lock for '{0}' keys, '{1}' other caches were involved.", lockKeys.Length, subscriberCount);
						return cacheLockValue.Value;
					}, Context);
			}
		}

		/// <inheritdoc />
		public override bool Unlock(object key, string lockValue)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			var lockKey = GetLockKey(key);
			Log.Debug("Unlocking key: '{0}'.", lockKey);

			return UnlockKey(lockKey, lockValue);
		}

		/// <inheritdoc />
		public override int UnlockMany(object[] keys, string lockValue)
		{
			if (keys == null)
			{
				throw new ArgumentNullException(nameof(keys));
			}

			var lockKeys = new string[keys.Length];
			for (var i = 0; i < keys.Length; i++)
			{
				lockKeys[i] = GetLockKey(keys[i]);
				Log.Debug("Unlocking key: '{0}'.", lockKeys[i]);
			}

			return UnlockManyKeys(lockKeys, lockValue);
		}

		/// <inheritdoc />
		public override void Validate()
		{
		}

		private RedisChannel GetClientChannel(int clientId)
		{
			return $"{RegionKey}@{clientId}";
		}

		private void OnPrivateMessage(ChannelMessage obj)
		{
			var message = (LockResultMessage) Serializer.Deserialize(obj.Message);
			ReleaseLock(message.LockKey, message.Success);
		}

		private void OnSynchronizationMessage(ChannelMessage channelMessage)
		{
			var message = (CacheSynchronizationMessage) Serializer.Deserialize(channelMessage.Message);
			var timestamp = message.Timestamp;
			var clientId = message.ClientId;
			if (clientId == _clientId)
			{
				return;
			}

			switch (message.OperationType)
			{
				case OperationType.Put:
					var putMessage = (PutData) message.Data;
					TryPutLocal(putMessage.Key, putMessage.Value, timestamp, clientId, false);
					break;
				case OperationType.Remove:
					TryRemoveLocal((string) message.Data, timestamp, clientId, false);
					break;
				case OperationType.Clear:
					TryClearLocal(timestamp, clientId, false);
					break;
				case OperationType.Lock:
					var lockData = (LockData) message.Data;
					// Send the result to the client that triggered the lock
					_subscriber.Publish(GetClientChannel(clientId), Serializer.Serialize(new LockResultMessage
					{
						LockKey = lockData.LockKey,
						Success = LockLocal(lockData.LockKey, new CacheLockValue(lockData.LockValue))
					}));
					break;
				case OperationType.LockMany:
					var lockManyData = (LockManyData) message.Data;
					// Send the result to the client that triggered the lock
					_subscriber.Publish(GetClientChannel(clientId), Serializer.Serialize(new LockResultMessage
					{
						LockKey = lockManyData.LockKeys[0], // Is enough to send one key as the value is shared across all of them
						Success = LockManyLocal(lockManyData.LockKeys, new CacheLockValue(lockManyData.LockValue))
					}));
					break;
				case OperationType.Unlock:
					var unlockData = (LockData) message.Data;
					UnlockLocal(unlockData.LockKey, unlockData.LockValue);
					break;
				case OperationType.UnlockMany:
					var unlockManyData = (LockManyData) message.Data;
					UnlockManyLocal(unlockManyData.LockKeys, unlockManyData.LockValue);
					break;
			}
		}

		private bool UnlockKey(string lockKey, string lockValue)
		{
			Publish(Serializer.Serialize(new CacheSynchronizationMessage
			{
				OperationType = OperationType.Unlock,
				ClientId = _clientId,
				Timestamp = DateTime.UtcNow.Ticks,
				Data = new LockData
				{
					LockKey = lockKey,
					LockValue = lockValue
				}
			}));

			return UnlockLocal(lockKey, lockValue);
		}

		private int UnlockManyKeys(string[] lockKeys, string lockValue)
		{
			Publish(Serializer.Serialize(new CacheSynchronizationMessage
			{
				OperationType = OperationType.UnlockMany,
				ClientId = _clientId,
				Timestamp = DateTime.UtcNow.Ticks,
				Data = new LockManyData
				{
					LockKeys = lockKeys,
					LockValue = lockValue
				}
			}));

			return UnlockManyLocal(lockKeys, lockValue);
		}

		private int UnlockManyLocal(string[] lockKeys, string lockValue)
		{
			var result = 0;
			foreach (var lockKey in lockKeys)
			{
				if (UnlockLocal(lockKey, lockValue))
				{
					result++;
				}
			}

			return result;
		}

		private bool UnlockLocal(string lockKey, string lockValue)
		{
			var value = (CacheLockValue) _memoryCache.Get(lockKey);
			if (value != null && value.Value == lockValue)
			{
				return _memoryCache.Remove(lockKey);
			}

			return false;
		}

		private bool LockManyLocal(string[] lockKeys, CacheLockValue lockValue)
		{
			foreach (var lockKey in lockKeys)
			{
				if (!LockLocal(lockKey, lockValue))
				{
					return false;
				}
			}

			return true;
		}

		private bool LockLocal(string lockKey, CacheLockValue lockValue)
		{
			lock (_lockLock)
			{
				var value = (CacheLockValue) _memoryCache.Get(lockKey);
				if (value != null && !value.Equals(lockValue))
				{
					return false;
				}

				_memoryCache.Put(lockKey, lockValue, _lockKeyTimeout);
				return true;
			}
		}

		private void ReleaseLock(string lockKey, bool success)
		{
			var cacheLockValue = (CacheLockValue) _memoryCache.Get(lockKey);
			if (cacheLockValue == null)
			{
				return;
			}

			lock (cacheLockValue)
			{
				if (cacheLockValue.Failed)
				{
					return;
				}

				if (!success)
				{
					cacheLockValue.Failed = true;
					cacheLockValue.Semaphore.Release();
				}

				cacheLockValue.RemainingLocks--;
				Log.Debug("Remaining locks to acquire: '{0}'", cacheLockValue.RemainingLocks);
				if (cacheLockValue.RemainingLocks == 0)
				{
					cacheLockValue.Semaphore.Release();
				}
			}
		}

		private void IncreaseLock(CacheLockValue cacheLockValue, long value)
		{
			lock (cacheLockValue)
			{
				if (cacheLockValue.Failed)
				{
					return;
				}

				cacheLockValue.RemainingLocks += value;
				Log.Debug("Remaining locks to acquire: '{0}'", cacheLockValue.RemainingLocks);
				if (cacheLockValue.RemainingLocks == 0)
				{
					cacheLockValue.Semaphore.Release();
				}
			}
		}

		private void OnFailedLock(int totalAttempts, long elapsedMs, Func<object> getDataFn)
		{
			var data = getDataFn();
			var key = string.Empty;
			switch (data)
			{
				case KeyValuePair<string, CacheLockValue> pair:
					key = pair.Key;
					UnlockKey(pair.Key, pair.Value.Value);
					break;
				case KeyValuePair<string[], CacheLockValue> pairMany:
					key = string.Join(",", pairMany.Key);
					UnlockManyKeys(pairMany.Key, pairMany.Value.Value);
					break;
			}

			throw new CacheException("Unable to acquire cache lock: " +
			                         $"region='{RegionName}', " +
			                         $"keys='{key}', " +
			                         $"total attempts='{totalAttempts}', " +
			                         $"total acquiring time= '{elapsedMs}ms'");
		}

		private string GetLockKey(object key)
		{
			return $"{GetCacheKey(key)}{_lockKeySuffix}";
		}

		private object GetLocal(string key)
		{
			var cacheValue = (CacheValue) _memoryCache.Get(key);
			if (cacheValue == null || _lastClearTimestamp > cacheValue.Timestamp)
			{
				return null;
			}

			// When the cache value was added with the same timestamp as the clear operation,
			// we have to rely on the version in order to find out which operation was the last
			// executed.
			if (_lastClearTimestamp == cacheValue.Timestamp && cacheValue.Version != _version)
			{
				return null;
			}

			return cacheValue.Value;
		}

		private void OnConnectionRestored(object sender, ConnectionFailedEventArgs e)
		{
			Log.Error(e.Exception, "The connection to Redis is restored, clearing the local cache.");
			// As we don't know if there was any message during the connection drop, we have to clear the local cache
			TryClearLocal(DateTime.UtcNow.Ticks, _clientId, false);
		}

		private void OnConnectionFailed(object sender, ConnectionFailedEventArgs e)
		{
			Log.Error(e.Exception, "The connection to Redis was lost.");
		}

		private void OnErrorMessage(object sender, RedisErrorEventArgs e)
		{
			Log.Error("An error occurred while executing a Redis command: {0}", e.Message);
			// As we don't know for which key the error occurred, we have to clear the local cache
			TryClearLocal(DateTime.UtcNow.Ticks, _clientId, false);
		}

		private bool TryRemoveLocal(string cacheKey, long timestamp, int clientId, bool publish)
		{
			return ExecuteOperation(cacheKey, null, timestamp, clientId, publish, true);
		}

		private bool TryPutLocal(string cacheKey, object value, long timestamp, int clientId, bool publish)
		{
			return ExecuteOperation(cacheKey, value, timestamp, clientId, publish, false);
		}

		private bool ExecuteOperation(string cacheKey, object value, long timestamp, int clientId, bool publish, bool remove)
		{
			var message = publish
				? Serializer.Serialize(new CacheSynchronizationMessage
				{
					OperationType = remove ? OperationType.Remove : OperationType.Put,
					ClientId = _clientId,
					Timestamp = timestamp,
					Data = remove
						? (object) cacheKey
						: new PutData
						{
							Key = cacheKey,
							Value = value
						}
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
				if (!IsActionValid(timestamp, clientId))
				{
					return false;
				}

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
					return ExecuteOperation(cacheKey, value, timestamp, clientId, publish, remove);
				}

				if (cacheValue.Timestamp == timestamp)
				{
					if (Log.IsDebugEnabled())
					{
						Log.Debug(
							"The timestamp for the key '{0}' is equal to the current one... " +
							"comparing the client id in order to determine who has a higher priority. ", cacheKey);
					}

					if (cacheValue.ClientId > clientId)
					{
						return false;
					}
				}
				else if (cacheValue.Timestamp > timestamp)
				{
					return false;
				}

				if (cacheValue is RemovedCacheValue)
				{
					if (remove)
					{
						cacheValue.Timestamp = timestamp;
						cacheValue.ClientId = clientId;
						cacheValue.Version = _version;
					}
					else
					{
						cacheValue = new CacheValue(value, timestamp, clientId, _version, cacheValue.Lock);
					}
				}
				else
				{
					if (remove)
					{
						cacheValue = new RemovedCacheValue(timestamp, clientId, _version, cacheValue.Lock);
					}
					else
					{
						cacheValue.Value = value;
						cacheValue.Timestamp = timestamp;
						cacheValue.ClientId = clientId;
						cacheValue.Version = _version;
					}
				}

				if (remove)
				{
					_memoryCache.Put(cacheKey, cacheValue, _maxSynchronizationTime);
				}
				else
				{
					_memoryCache.Put(cacheKey, cacheValue);
				}
				
				if (publish)
				{
					Publish(message);
				}

				return true;
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

					if (remove)
					{
						_memoryCache.Put(cacheKey, new RemovedCacheValue(timestamp, clientId, _version), _maxSynchronizationTime);
					}
					else
					{
						_memoryCache.Put(cacheKey, new CacheValue(value, timestamp, clientId, _version));
					}

					if (publish)
					{
						Publish(message);
					}

					return true;
				}
				finally
				{
					_writeLock.Release();
				}
			}
		}

		private void Publish(RedisValue message)
		{
			_subscriber.Publish(_synchronizationChannel, message, _usePipelining ? CommandFlags.FireAndForget : CommandFlags.None);
		}

		private Task PublishAsync(RedisValue message)
		{
			if (!_usePipelining)
			{
				return _subscriber.PublishAsync(_synchronizationChannel, message);
			}

			Publish(message);
			return Task.CompletedTask;
		}

		private bool TryClearLocal(long timestamp, int clientId, bool publish)
		{
			var message = publish
				? Serializer.Serialize(new CacheSynchronizationMessage
				{
					OperationType = OperationType.Clear,
					ClientId = _clientId,
					Timestamp = timestamp
				})
				: null;

			_writeLock.Wait();
			try
			{
				if (!IsActionValid(timestamp, clientId))
				{
					return false;
				}

				_lastClearTimestamp = timestamp;
				_lastClearClientId = clientId;
				_version++;
				// Unfortunately we cannot clear the local cache as it can lead to an inconsistent
				// state across local caches due to delays of synchronization messages.

				if (publish)
				{
					Publish(message);
				}

				return true;
			}
			finally
			{
				_writeLock.Release();
			}
		}

		private bool IsActionValid(long timestamp, long clientId)
		{
			if (_lastClearTimestamp == timestamp)
			{
				if (Log.IsDebugEnabled())
				{
					Log.Debug(
						"The timestamp for the operation is equal to the last clear timestamp... " +
						"comparing the client id in order to determine who has a higher priority. ");
				}

				if (_lastClearClientId > clientId)
				{
					return false;
				}
			}
			else if (_lastClearTimestamp > timestamp)
			{
				return false;
			}

			return true;
		}

		private class CacheValue
		{
			public CacheValue(object value, long timestamp, int clientId, long version, SemaphoreSlim lockValue)
			{
				Value = value;
				Timestamp = timestamp;
				ClientId = clientId;
				Version = version;
				Lock = lockValue;
			}

			public CacheValue(object value, long timestamp, int clientId, long version)
				: this(value, timestamp, clientId, version, new SemaphoreSlim(1, 1))
			{
			}

			public object Value { get; set; }

			public long Timestamp { get; set; }

			public int ClientId { get; set; }

			public long Version { get; set; }

			public SemaphoreSlim Lock { get; }
		}

		private class RemovedCacheValue : CacheValue
		{
			public RemovedCacheValue(long timestamp, int clientId, long version, SemaphoreSlim lockValue)
				: base(null, timestamp, clientId, version, lockValue)
			{
			}

			public RemovedCacheValue(long timestamp, int clientId, long version)
				: base(null, timestamp, clientId, version)
			{
			}
		}

		private class CacheLockValue : IEquatable<CacheLockValue>, IDisposable
		{
			public CacheLockValue(string value)
			{
				Value = value;
			}

			public void Setup()
			{
				Failed = false;
				if (Semaphore == null)
				{
					Semaphore = new SemaphoreSlim(0, 1);
				}
			}

			public string Value { get; }

			public long RemainingLocks { get; set; }

			public bool Failed { get; set; }

			public SemaphoreSlim Semaphore { get; private set; }

			public override bool Equals(object obj)
			{
				return obj?.GetType() == typeof(CacheLockValue) && Equals((CacheLockValue) obj);
			}

			public override int GetHashCode()
			{
				return Value.GetHashCode();
			}

			public bool Equals(CacheLockValue other)
			{
				return string.Equals(Value, other?.Value);
			}

			public void Dispose()
			{
				Semaphore?.Dispose();
				Semaphore = null;
			}
		}
	}
}
