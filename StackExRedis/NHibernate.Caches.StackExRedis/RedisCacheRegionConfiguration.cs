using System;
using System.Text;

namespace NHibernate.Caches.StackExRedis
{
	/// <summary>
	/// Cache configuration for a region.
	/// </summary>
	public class RedisCacheRegionConfiguration
	{
		/// <summary>
		/// Creates a cache region configuration.
		/// </summary>
		/// <param name="regionName">The name of the region.</param>
		public RedisCacheRegionConfiguration(string regionName)
		{
			RegionName = regionName;
		}

		/// <summary>
		/// The key representing the region that is composed of <see cref="CacheKeyPrefix"/>, 
		/// <see cref="EnvironmentName"/>, <see cref="RegionPrefix"/> and <see cref="RegionName"/>.
		/// </summary>
		public string RegionKey => $"{CacheKeyPrefix}{EnvironmentName}{RegionPrefix}{RegionName}";

		/// <summary>
		/// The region name.
		/// </summary>
		public string RegionName { get; }

		/// <summary>
		/// The name of the environment that will be prepended before each cache key in order to allow having
		/// multiple environments on the same Redis database.
		/// </summary>
		public string EnvironmentName { get; internal set; }

		/// <summary>
		/// The prefix that will be prepended before each cache key in order to avoid having collisions when multiple clients
		/// uses the same Redis database.
		/// </summary>
		public string CacheKeyPrefix { get; internal set; }

		/// <summary>
		/// The expiration time for the keys to expire.
		/// </summary>
		public TimeSpan Expiration { get; internal set; }

		/// <summary>
		/// The prefix that will be prepended before the region name when building a cache key.
		/// </summary>
		public string RegionPrefix { get; internal set; }

		/// <summary>
		/// The Redis database index.
		/// </summary>
		public int Database { get; internal set; }

		/// <summary>
		/// Whether the expiration is sliding or not.
		/// </summary>
		public bool UseSlidingExpiration { get; internal set; }

		/// <summary>
		/// The <see cref="AbstractRegionStrategy"/> type.
		/// </summary>
		public System.Type RegionStrategy { get; internal set; }

		/// <summary>
		/// Whether the hash code of the key should be added to the cache key.
		/// </summary>
		public bool AppendHashcode { get; internal set; }

		/// <summary>
		/// The <see cref="IRedisSerializer"/> to be used.
		/// </summary>
		public IRedisSerializer Serializer { get; internal set; }

		/// <summary>
		/// The <see cref="IDatabaseProvider"/> instance.
		/// </summary>
		public IDatabaseProvider DatabaseProvider { get; internal set; }

		/// <summary>
		/// The configuration for locking keys.
		/// </summary>
		public RedisCacheLockConfiguration LockConfiguration { get; internal set; }

		/// <inheritdoc />
		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendFormat("RegionName={0}", RegionName);
			sb.AppendFormat("EnvironmentName={0}", EnvironmentName);
			sb.AppendFormat("CacheKeyPrefix={0}", CacheKeyPrefix);
			sb.AppendFormat("Expiration={0}s", Expiration.TotalSeconds);
			sb.AppendFormat("Database={0}", Database);
			sb.AppendFormat("UseSlidingExpiration={0}", UseSlidingExpiration);
			sb.AppendFormat("AppendHashcode={0}", AppendHashcode);
			sb.AppendFormat("RegionStrategy={0}", RegionStrategy);
			sb.AppendFormat("Serializer={0}", Serializer);
			sb.AppendFormat("DatabaseProvider={0}", DatabaseProvider);
			sb.AppendFormat("LockConfiguration=({0})", LockConfiguration);
			return sb.ToString();
		}
	}
}
