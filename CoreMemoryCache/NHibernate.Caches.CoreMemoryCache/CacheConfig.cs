using System.Collections.Generic;

namespace NHibernate.Caches.CoreMemoryCache
{
	/// <summary>
	/// Cache configuration properties.
	/// </summary>
	public class CacheConfig
	{
		/// <summary>
		/// Build a cache configuration.
		/// </summary>
		/// <param name="expirationScanFrequency">The frequency at which scans for cleaning expired cached item have to be done.</param>
		/// <param name="regions">The configured cache regions.</param>
		public CacheConfig(string expirationScanFrequency, RegionConfig[] regions)
		{
			ExpirationScanFrequency = expirationScanFrequency;
			Regions = regions;
		}

		/// <summary>The frequency at which scans for cleaning expired cached item have to be done.</summary>
		public string ExpirationScanFrequency { get; }

		/// <summary>The configured cache regions.</summary>
		public RegionConfig[] Regions { get; }
	}

	/// <summary>
	/// Cache region configuration properties.
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

		/// <summary>The configuration properties.</summary>
		public IDictionary<string,string> Properties { get; }
	}
}
