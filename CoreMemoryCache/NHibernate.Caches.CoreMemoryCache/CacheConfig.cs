using System.Collections.Generic;

namespace NHibernate.Caches.CoreMemoryCache
{
	/// <summary>
	/// Cache config properties.
	/// </summary>
	public class CacheConfig
	{
		/// <summary>
		/// Build a cache global configuration.
		/// </summary>
		/// <param name="expirationScanFrequency">The frequency at which scans for cleaning expired cached item have to be done.</param>
		public CacheConfig(string expirationScanFrequency)
		{
			ExpirationScanFrequency = expirationScanFrequency;
			Global = true;
		}

		/// <summary>
		/// Build a cache region configuration.
		/// </summary>
		/// <param name="region">The configured cache region.</param>
		/// <param name="expiration">The expiration for the region.</param>
		/// <param name="sliding">Whether the expiration should be sliding or not.</param>
		public CacheConfig(string region, string expiration, string sliding)
		{
			Region = region;
			Properties = new Dictionary<string, string>();
			if (!string.IsNullOrEmpty(expiration))
				Properties["expiration"] = expiration;
			if (!string.IsNullOrEmpty(sliding))
				Properties["cache.use_sliding_expiration"] = sliding;
		}

		/// <summary>Whether this configuration is global or is a cache region configuration.</summary>
		public bool Global { get; }

		/// <summary>The region name.</summary>
		public string Region { get; }

		/// <summary>The frequency at which scans for cleaning expired cached item have to be done.</summary>
		public string ExpirationScanFrequency { get; }

		/// <summary>The configuration properties.</summary>
		public IDictionary<string,string> Properties { get; }
	}
}
