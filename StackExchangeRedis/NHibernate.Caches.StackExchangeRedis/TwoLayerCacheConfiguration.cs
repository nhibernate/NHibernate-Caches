using System;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Caches.Common;
using StackExchange.Redis;

namespace NHibernate.Caches.StackExchangeRedis
{
	internal class TwoLayerCacheConfiguration
	{
		public string RegionKey { get; set; }

		public IConnectionMultiplexer ConnectionMultiplexer { get; set; }

		public RegionMemoryCacheBase MemoryCache { get; set; }

		public CacheSerializerBase Serializer { get; set; }

		public INHibernateLogger Log { get; set; }

		public IDatabase Database { get; set; }

		public Func<string, object> RedisGet { get; set; }

		public Func<string, CancellationToken, Task<object>> RedisGetAsync { get; set; }

		public Func<RedisKey[], object[]> RedisGetMany { get; set; }

		public Func<RedisKey[], CancellationToken, Task<object[]>> RedisGetManyAsync { get; set; }

		public string PutScript { get; set; }

		public string RemoveScript { get; set; }

		public bool ExpirationEnabled { get; set; }

		public bool UseSlidingExpiration { get; set; }

		public TimeSpan Expiration { get; set; }

		public Func<RedisKey[], RedisKey[]> AppendAdditionalKeys { get; set; }

		public Func<RedisValue[], RedisValue[]> AppendAdditionalValues { get; set; }

		public Action<string> LogErrorMessage { get; set; }

		public bool UsePipelining { get; set; }

		public TimeSpan MaxSynchronizationTime { get; set; }

		public int ClientId { get; set; }
	}
}
