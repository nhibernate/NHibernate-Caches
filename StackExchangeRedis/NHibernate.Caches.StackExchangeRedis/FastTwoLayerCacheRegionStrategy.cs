using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;
using static NHibernate.Caches.StackExchangeRedis.ConfigurationHelper;

namespace NHibernate.Caches.StackExchangeRedis
{
	/// <summary>
	/// A strategy that uses an additional local memory cache for faster readings. The local caches are invalidated by
	/// using Redis pubsub mechanism. This strategy should be used only for regions that have few write operations
	/// and a high expiration time. This strategy does not support <see cref="AbstractRegionStrategy.Clear"/> operation.
	/// </summary>
	public sealed partial class FastTwoLayerCacheRegionStrategy : FastRegionStrategy
	{
		private static readonly string PutLuaScript;
		private static readonly string RemoveLuaScript;

		static FastTwoLayerCacheRegionStrategy()
		{
			PutLuaScript = LuaScriptProvider.GetScript<TwoLayerCacheRegionStrategy>(nameof(Put));
			RemoveLuaScript = LuaScriptProvider.GetScript<TwoLayerCacheRegionStrategy>(nameof(Remove));
		}


		private readonly TwoLayerCache _twoLayerCache;

		/// <inheritdoc />
		public FastTwoLayerCacheRegionStrategy(
			IConnectionMultiplexer connectionMultiplexer,
			RedisCacheRegionConfiguration configuration,
			RegionMemoryCacheBase memoryCache, 
			IDictionary<string, string> properties)
			: base(connectionMultiplexer, configuration, properties)
		{
			var usePipelining = GetBoolean("cache.region_strategy.fast_two_layer_cache.use_pipelining", properties, false);
			Log.Debug("Use pipelining for region {0}: {1}", RegionName, usePipelining);

			var clientId = GetInteger("cache.region_strategy.fast_two_layer_cache.client_id", properties, Guid.NewGuid().GetHashCode());
			Log.Debug("Client id for region {0}: {1}", RegionName, clientId);

			var maxSynchronizationTime = GetTimeSpanFromSeconds(
				"cache.region_strategy.fast_two_layer_cache.max_synchronization_time", properties, TimeSpan.FromSeconds(10));
			Log.Debug("Max synchronization time for region {0}: {1} seconds", RegionName, maxSynchronizationTime.TotalSeconds);

			_twoLayerCache = new TwoLayerCache(new TwoLayerCacheConfiguration
			{
				ConnectionMultiplexer = connectionMultiplexer,
				RegionKey = RegionKey,
				MemoryCache = memoryCache,
				Expiration = Expiration,
				Serializer = Serializer,
				UseSlidingExpiration = UseSlidingExpiration,
				Log = Log,
				Database = Database,
				PutScript = PutScript,
				AppendAdditionalValues = AppendAdditionalValues,
				ExpirationEnabled = ExpirationEnabled,
				AppendAdditionalKeys = AppendAdditionalKeys,
				RemoveScript = RemoveScript,
				RedisGet = base.ExecuteGet,
				RedisGetAsync = base.ExecuteGetAsync,
				RedisGetMany = base.ExecuteGetMany,
				RedisGetManyAsync = base.ExecuteGetManyAsync,
				UsePipelining = usePipelining,
				ClientId = clientId,
				MaxSynchronizationTime = maxSynchronizationTime
			});
		}

		/// <inheritdoc />
		protected override string PutScript => PutLuaScript;

		/// <inheritdoc />
		protected override string RemoveScript => RemoveLuaScript;

		/// <inheritdoc />
		protected override void ExecutePut(string cacheKey, object value)
		{
			_twoLayerCache.Put(cacheKey, value);
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
				ExecutePutMany(keys, values);
				return Task.CompletedTask;
			}
			catch (Exception ex)
			{
				return Task.FromException<bool>(ex);
			}
		}

		/// <inheritdoc />
		protected override object ExecuteGet(string cacheKey)
		{
			return _twoLayerCache.Get(cacheKey);
		}

		/// <inheritdoc />
		protected override object[] ExecuteGetMany(RedisKey[] cacheKeys)
		{
			return _twoLayerCache.GetMany(cacheKeys);
		}

		/// <inheritdoc />
		protected override bool ExecuteRemove(string cacheKey)
		{
			return _twoLayerCache.Remove(cacheKey);
		}
	}
}
