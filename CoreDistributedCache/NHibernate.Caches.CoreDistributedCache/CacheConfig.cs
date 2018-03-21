using System.Collections.Generic;
using Microsoft.Extensions.Caching.Distributed;

namespace NHibernate.Caches.CoreDistributedCache
{
	/// <summary>
	/// Cache configuration properties.
	/// </summary>
	public class CacheConfig
	{
		/// <summary>
		/// Build a cache configuration.
		/// </summary>
		/// <param name="factoryClass">The <see cref="IDistributedCacheFactory"/> factory class name to use for getting
		/// <see cref="IDistributedCache"/> instances.</param>
		/// <param name="properties">The cache configuration properties.</param>
		/// <param name="regions">The configured cache regions.</param>
		public CacheConfig(string factoryClass, IDictionary<string, string> properties, RegionConfig[] regions)
		{
			FactoryClass = factoryClass;
			Regions = regions;
			Properties = properties;
		}

		/// <summary>The <see cref="IDistributedCacheFactory"/> factory class name to use for getting
		/// <see cref="IDistributedCache"/> instances.</summary>
		public string FactoryClass { get; }

		/// <summary>The configured cache regions.</summary>
		public RegionConfig[] Regions { get; }

		/// <summary>The cache configuration properties.</summary>
		public IDictionary<string, string> Properties { get; }
	}

	/// <summary>
	/// Region configuration properties.
	/// </summary>
	public class RegionConfig
	{
		/// <summary>
		/// Build a cache region configuration.
		/// </summary>
		/// <param name="region">The configured cache region.</param>
		/// <param name="expiration">The expiration for the region.</param>
		/// <param name="sliding">Whether the expiration should be sliding or not.</param>
		public RegionConfig(string region, string expiration, string sliding)
		{
			Region = region;
			Properties = new Dictionary<string, string>();
			if (!string.IsNullOrEmpty(expiration))
				Properties["expiration"] = expiration;
			if (!string.IsNullOrEmpty(sliding))
				Properties["cache.use_sliding_expiration"] = sliding;
		}

		/// <summary>The region name.</summary>
		public string Region { get; }

		/// <summary>The region configuration properties.</summary>
		public IDictionary<string, string> Properties { get; }
	}
}
