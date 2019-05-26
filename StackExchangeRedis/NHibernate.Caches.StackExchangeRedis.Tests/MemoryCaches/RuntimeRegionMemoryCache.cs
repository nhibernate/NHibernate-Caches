using System;
using System.Runtime.Caching;

namespace NHibernate.Caches.StackExchangeRedis.Tests.MemoryCaches
{
	internal class RuntimeRegionMemoryCache : RegionMemoryCacheBase
	{
		private readonly ObjectCache _cache = MemoryCache.Default;
		private readonly string _keyPrefix;
		private readonly bool _useSlidingExpiration;
		private readonly bool _infiniteAbsoluteExpiration;
		private readonly TimeSpan _expiration;

		public RuntimeRegionMemoryCache(RedisCacheRegionConfiguration configuration, string keyPrefix = null)
		{
			_keyPrefix = keyPrefix;
			_useSlidingExpiration = configuration.UseSlidingExpiration;
			_infiniteAbsoluteExpiration = _useSlidingExpiration || configuration.Expiration == TimeSpan.Zero;
			_expiration = configuration.Expiration;
		}

		public override object Get(string key)
		{
			return _cache.Get(GetCacheKey(key));
		}

		public override void Put(string key, object value)
		{
			_cache.Set(GetCacheKey(key), value,
				new CacheItemPolicy
				{
					AbsoluteExpiration = _infiniteAbsoluteExpiration
						? ObjectCache.InfiniteAbsoluteExpiration
						: DateTimeOffset.UtcNow.Add(_expiration),
					SlidingExpiration = _useSlidingExpiration ? _expiration : ObjectCache.NoSlidingExpiration
				});
		}

		public override void Put(string key, string dependencyKey, object value)
		{
			_cache.Set(GetCacheKey(key), value,
				new CacheItemPolicy
				{
					AbsoluteExpiration = _infiniteAbsoluteExpiration
						? ObjectCache.InfiniteAbsoluteExpiration
						: DateTimeOffset.UtcNow.Add(_expiration),
					SlidingExpiration = _useSlidingExpiration ? _expiration : ObjectCache.NoSlidingExpiration,
					ChangeMonitors = { _cache.CreateCacheEntryChangeMonitor(new[] { GetCacheKey(dependencyKey) }) }
				});
		}

		public override void Put(string key, object value, TimeSpan expiration)
		{
			_cache.Set(GetCacheKey(key), value,
				new CacheItemPolicy
				{
					AbsoluteExpiration = expiration == TimeSpan.Zero
						? ObjectCache.InfiniteAbsoluteExpiration
						: DateTimeOffset.UtcNow.Add(expiration),
					SlidingExpiration = ObjectCache.NoSlidingExpiration
				});
		}

		public override void Put(string key, string dependencyKey, object value, TimeSpan expiration)
		{
			_cache.Set(GetCacheKey(key), value,
				new CacheItemPolicy
				{
					AbsoluteExpiration = expiration == TimeSpan.Zero
						? ObjectCache.InfiniteAbsoluteExpiration
						: DateTimeOffset.UtcNow.Add(expiration),
					SlidingExpiration = ObjectCache.NoSlidingExpiration,
					ChangeMonitors = {_cache.CreateCacheEntryChangeMonitor(new[] { GetCacheKey(dependencyKey)})}
				});
		}

		public override bool Remove(string key)
		{
			return _cache.Remove(GetCacheKey(key)) != null;
		}

		private string GetCacheKey(string key)
		{
			return string.Concat(_keyPrefix, key);
		}
	}
}
