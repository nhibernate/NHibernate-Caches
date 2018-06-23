using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NHibernate.Cache;
using NHibernate.Caches.Common.Tests;
using NUnit.Framework;

namespace NHibernate.Caches.StackExRedis.Tests
{
	[TestFixture, Explicit]
	public partial class RedisCachePerformanceFixture : Fixture
	{
		private const int RepeatTimes = 5;
		private const int BatchSize = 20;
		private const int CacheItems = 1000;

		protected override Func<ICacheProvider> ProviderBuilder =>
			() => new RedisCacheProvider();

		[Test]
		public void TestGetOperation()
		{
			TestOperation("Get", true, (cache, key, _) => cache.Get(key));
		}

		[Test]
		public void TestGetManyOperation()
		{
			TestBatchOperation("GetMany", true, (cache, keys, _) => cache.GetMany(keys));
		}

		[Test]
		public void TestGetOperationWithSlidingExpiration()
		{
			var props = new Dictionary<string, string> {{"sliding", "true"}};
			TestOperation("Get", true, (cache, key, _) => cache.Get(key),
				caches: new List<RedisCache> {GetDefaultRedisCache(props), GetFastRedisCache(props)});
		}

		[Test]
		public void TestGetManyOperationWithSlidingExpiration()
		{
			var props = new Dictionary<string, string> {{"sliding", "true"}};
			TestBatchOperation("GetMany", true, (cache, keys, _) => cache.GetMany(keys),
				caches: new List<RedisCache> {GetDefaultRedisCache(props), GetFastRedisCache(props)});
		}

		[Test]
		public void TestPutOperation()
		{
			var props = new Dictionary<string, string> {{"expiration", "0"}};
			TestOperation("Put", false, (cache, key, value) => cache.Put(key, value),
				caches: new List<RedisCache> {GetFastRedisCache(props)});
		}

		[Test]
		public void TestPutManyOperation()
		{
			var props = new Dictionary<string, string> {{"expiration", "0"}};
			TestBatchOperation("PutMany", false, (cache, keys, values) => cache.PutMany(keys, values),
				caches: new List<RedisCache> {GetFastRedisCache(props)});
		}

		[Test]
		public void TestPutOperationWithExpiration()
		{
			TestOperation("Put", false, (cache, key, value) => cache.Put(key, value));
		}

		[Test]
		public void TestPutManyOperationWithExpiration()
		{
			TestBatchOperation("PutMany", false, (cache, keys, values) => cache.PutMany(keys, values));
		}

		[Test]
		public void TestLockUnlockOperation()
		{
			TestOperation("Lock/Unlock", true, (cache, key, _) =>
			{
				cache.Lock(key);
				cache.Unlock(key);
			});
		}

		[Test]
		public void TestLockUnlockManyOperation()
		{
			TestBatchOperation("LockMany/UnlockMany", true, (cache, keys, _) =>
			{
				var value = cache.LockMany(keys);
				cache.UnlockMany(keys, value);
			});
		}

		private void TestBatchOperation(string operation, bool fillData,
			Action<RedisCache, object[], object[]> keyValueAction, int? batchSize = null,
			int? cacheItems = null, int? repeat = null, List<RedisCache> caches = null)
		{
			TestOperation(operation, fillData, null, keyValueAction, batchSize, cacheItems, repeat, caches);
		}

		private Task TestBatchOperationAsync(string operation, bool fillData,
			Func<RedisCache, object[], object[], Task> keyValueAction, int? batchSize = null,
			int? cacheItems = null, int? repeat = null, List<RedisCache> caches = null)
		{
			return TestOperationAsync(operation, fillData, null, keyValueAction, batchSize, cacheItems, repeat, caches);
		}

		private void TestOperation(string operation, bool fillData,
			Action<RedisCache, CacheKey, List<object>> keyValueAction,
			int? cacheItems = null, int? repeat = null, List<RedisCache> caches = null)
		{
			TestOperation(operation, fillData, keyValueAction, null, null, cacheItems, repeat, caches);
		}

		private Task TestOperationAsync(string operation, bool fillData,
			Func<RedisCache, CacheKey, List<object>, Task> keyValueAction,
			int? cacheItems = null, int? repeat = null, List<RedisCache> caches = null)
		{
			return TestOperationAsync(operation, fillData, keyValueAction, null, null, cacheItems, repeat, caches);
		}

		private void TestOperation(string operation, bool fillData,
			Action<RedisCache, CacheKey, List<object>> keyValueAction,
			Action<RedisCache, object[], object[]> batchKeyValueAction,
			int? batchSize, int? cacheItems = null, int? repeat = null, List<RedisCache> caches = null
			)
		{
			caches = caches ?? new List<RedisCache> {GetDefaultRedisCache(), GetFastRedisCache()};
			var cacheData = GetCacheData(cacheItems ?? CacheItems);

			if (fillData)
			{
				foreach (var cache in caches)
				{
					PutCacheData(cache, cacheData);
				}
			}

			foreach (var cache in caches)
			{
				var repeatPolicy = new CacheOperationRepeatPolicy(operation, cache, repeat ?? RepeatTimes, cacheData);
				if (keyValueAction != null)
				{
					repeatPolicy.Execute(keyValueAction);
				}
				else
				{
					repeatPolicy.BatchExecute(batchKeyValueAction, batchSize ?? BatchSize);
				}
			}

			foreach (var cache in caches)
			{
				RemoveCacheData(cache, cacheData);
			}
		}

		private async Task TestOperationAsync(string operation, bool fillData,
			Func<RedisCache, CacheKey, List<object>, Task> keyValueAction,
			Func<RedisCache, object[], object[], Task> batchKeyValueAction,
			int? batchSize, int? cacheItems = null, int? repeat = null, List<RedisCache> caches = null
		)
		{
			caches = caches ?? new List<RedisCache> { GetDefaultRedisCache(), GetFastRedisCache() };
			var cacheData = GetCacheData(cacheItems ?? CacheItems);

			if (fillData)
			{
				foreach (var cache in caches)
				{
					PutCacheData(cache, cacheData);
				}
			}

			foreach (var cache in caches)
			{
				var repeatPolicy = new CacheOperationRepeatPolicy(operation, cache, repeat ?? RepeatTimes, cacheData);
				if (keyValueAction != null)
				{
					await repeatPolicy.ExecuteAsync(keyValueAction);
				}
				else
				{
					await repeatPolicy.BatchExecuteAsync(batchKeyValueAction, batchSize ?? BatchSize);
				}
			}

			foreach (var cache in caches)
			{
				RemoveCacheData(cache, cacheData);
			}
		}

		private void PutCacheData(ICache cache, Dictionary<CacheKey, List<object>> cacheData)
		{
			foreach (var pair in cacheData)
			{
				cache.Put(pair.Key, pair.Value);
			}
		}

		private void RemoveCacheData(ICache cache, Dictionary<CacheKey, List<object>> cacheData)
		{
			foreach (var pair in cacheData)
			{
				cache.Remove(pair.Key);
			}
		}

		private Dictionary<CacheKey, List<object>> GetCacheData(int numberOfKeys)
		{
			var keyValues = new Dictionary<CacheKey, List<object>>();
			for (var i = 0; i < numberOfKeys; i++)
			{
				keyValues.Add(
					new CacheKey((long) i, NHibernateUtil.Int64, nameof(GetCacheData), null),
					new List<object>
					{
						i,
						string.Join("", Enumerable.Repeat(i, 30)),
						Enumerable.Repeat((byte) i, 30).ToArray(),
						null,
						DateTime.Now,
						i / 4.5,
						Guid.NewGuid()
					}
				);
			}

			return keyValues;
		}

		private RedisCache GetFastRedisCache(Dictionary<string, string> properties = null)
		{
			var props = GetDefaultProperties();
			foreach (var property in properties ?? new Dictionary<string, string>())
			{
				props[property.Key] = property.Value;
			}
			props["strategy"] = typeof(FastRegionStrategy).AssemblyQualifiedName;
			return (RedisCache)DefaultProvider.BuildCache(DefaultRegion, props);
		}

		private RedisCache GetDefaultRedisCache(Dictionary<string, string> properties = null)
		{
			var props = GetDefaultProperties();
			foreach (var property in properties ?? new Dictionary<string, string>())
			{
				props[property.Key] = property.Value;
			}
			return (RedisCache) DefaultProvider.BuildCache(DefaultRegion, props);
		}
	}

	public class CacheOperationRepeatPolicy
	{
		private static readonly INHibernateLogger Log = NHibernateLogger.For(typeof(CacheOperationRepeatPolicy));
		private readonly string _operation;
		private readonly RedisCache _cache;
		private readonly int _repeatTimes;
		private readonly Dictionary<CacheKey, List<object>> _cacheData;

		public CacheOperationRepeatPolicy(string operation, RedisCache cache, int repeatTimes, Dictionary<CacheKey, List<object>> cacheData)
		{
			_operation = operation;
			_cache = cache;
			_repeatTimes = repeatTimes;
			_cacheData = cacheData;
		}

		public void BatchExecute(Action<RedisCache, object[], object[]> keyValueAction, int batchSize)
		{
			var batchKeys = new List<object>();
			var batchValues = new List<object>();
			// Cold start
			Iterate();

			var timer = new Stopwatch();
			var result = new long[_repeatTimes];
			for (var i = 0; i < _repeatTimes; i++)
			{
				timer.Restart();
				Iterate();
				timer.Stop();
				result[i] = timer.ElapsedMilliseconds;
			}
			LogResult(result, batchSize);

			void Iterate()
			{
				foreach (var pair in _cacheData)
				{
					if (batchKeys.Count > 0 && batchKeys.Count % batchSize == 0)
					{
						keyValueAction(_cache, batchKeys.ToArray(), batchValues.ToArray());
						batchKeys.Clear();
						batchValues.Clear();
					}
					batchKeys.Add(pair.Key);
					batchValues.Add(pair.Value);
				}

				if (batchKeys.Count == 0)
				{
					return;
				}
				keyValueAction(_cache, batchKeys.ToArray(), batchValues.ToArray());
				batchKeys.Clear();
				batchValues.Clear();
			}
		}

		public async Task BatchExecuteAsync(Func<RedisCache, object[], object[], Task> keyValueFunc, int batchSize)
		{
			var batchKeys = new List<object>();
			var batchValues = new List<object>();
			// Cold start
			await Iterate();

			var timer = new Stopwatch();
			var result = new long[_repeatTimes];
			for (var i = 0; i < _repeatTimes; i++)
			{
				timer.Restart();
				await Iterate();
				timer.Stop();
				result[i] = timer.ElapsedMilliseconds;
			}
			LogResult(result, batchSize);

			async Task Iterate()
			{
				foreach (var pair in _cacheData)
				{
					if (batchKeys.Count > 0 && batchKeys.Count % batchSize == 0)
					{
						await keyValueFunc(_cache, batchKeys.ToArray(), batchValues.ToArray());
						batchKeys.Clear();
						batchValues.Clear();
					}
					batchKeys.Add(pair.Key);
					batchValues.Add(pair.Value);
				}

				if (batchKeys.Count == 0)
				{
					return;
				}
				await keyValueFunc(_cache, batchKeys.ToArray(), batchValues.ToArray());
				batchKeys.Clear();
				batchValues.Clear();
			}
		}

		public void Execute(Action<RedisCache, CacheKey, List<object>> keyValueAction)
		{
			// Cold start
			foreach (var pair in _cacheData)
			{
				keyValueAction(_cache, pair.Key, pair.Value);
			}

			var timer = new Stopwatch();
			var result = new long[_repeatTimes];
			for (var i = 0; i < _repeatTimes; i++)
			{
				timer.Restart();
				foreach (var pair in _cacheData)
				{
					keyValueAction(_cache, pair.Key, pair.Value);
				}
				timer.Stop();
				result[i] = timer.ElapsedMilliseconds;
			}
			LogResult(result, 1);
		}

		public async Task ExecuteAsync(Func<RedisCache, CacheKey, List<object>, Task> keyValueFunc)
		{
			// Cold start
			foreach (var pair in _cacheData)
			{
				await keyValueFunc(_cache, pair.Key, pair.Value);
			}

			var timer = new Stopwatch();
			var result = new long[_repeatTimes];
			for (var i = 0; i < _repeatTimes; i++)
			{
				timer.Restart();
				foreach (var pair in _cacheData)
				{
					await keyValueFunc(_cache, pair.Key, pair.Value);
				}
				timer.Stop();
				result[i] = timer.ElapsedMilliseconds;
			}
			LogResult(result, 1);
		}

		private void LogResult(long[] result, int batchSize)
		{
			Log.Info(
				$"{_operation} operation for {_cacheData.Count} keys with region strategy {_cache.RegionStrategy.GetType().Name}:{Environment.NewLine}" +
				$"Total iterations: {_repeatTimes}{Environment.NewLine}" +
				$"Batch size: {batchSize}{Environment.NewLine}" +
				$"Times per iteration {string.Join(",", result.Select(o => $"{o}ms"))}{Environment.NewLine}" +
				$"Average {result.Average()}ms"
				
			);
		}
	}
}
