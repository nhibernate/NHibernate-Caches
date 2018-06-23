using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHibernate.Caches.StackExRedis
{
	/// <summary>
	/// Region configuration properties.
	/// </summary>
	public class RegionConfig
	{
		/// <summary>
		/// Build a cache region configuration.
		/// </summary>
		/// <param name="region">The configured cache region.</param>
		public RegionConfig(string region) : this(region, null, null, null, null, null)
		{
		}

		/// <summary>
		/// Build a cache region configuration.
		/// </summary>
		/// <param name="region">The configured cache region.</param>
		/// <param name="expiration">The expiration for the region.</param>
		/// <param name="useSlidingExpiration">Whether the expiration should be sliding or not.</param>
		/// <param name="database">The database for the region.</param>
		/// <param name="regionStrategy">The strategy for the region.</param>
		/// <param name="useHashCode">Whether the hash code of the key should be added to the cache key.</param>
		public RegionConfig(string region, TimeSpan? expiration, bool? useSlidingExpiration, int? database, System.Type regionStrategy,
			bool? useHashCode)
		{
			Region = region;
			Expiration = expiration;
			UseSlidingExpiration = useSlidingExpiration;
			Database = database;
			RegionStrategy = regionStrategy;
			UseHashCode = useHashCode;
		}

		/// <summary>
		/// The region name.
		/// </summary>
		public string Region { get; }

		/// <summary>
		/// The expiration time for the keys to expire.
		/// </summary>
		public TimeSpan? Expiration { get; }

		/// <summary>
		/// Whether the expiration is sliding or not.
		/// </summary>
		public bool? UseSlidingExpiration { get; }

		/// <summary>
		/// The Redis database index.
		/// </summary>
		public int? Database { get; }

		/// <summary>
		/// The <see cref="AbstractRegionStrategy"/> type.
		/// </summary>
		public System.Type RegionStrategy { get; }

		/// <summary>
		/// Whether the hash code of the key should be added to the cache key.
		/// </summary>
		public bool? UseHashCode { get; }
	}
}
