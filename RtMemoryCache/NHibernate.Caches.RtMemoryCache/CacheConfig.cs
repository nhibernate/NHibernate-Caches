using System.Collections.Generic;

namespace NHibernate.Caches.RtMemoryCache
{
	/// <summary>
	/// Configuration properties of a cache region.
	/// </summary>
	public class CacheConfig
	{
		private readonly Dictionary<string, string> properties;
		private readonly string regionName;

		/// <summary>
		/// Build a cache region configuration.
		/// </summary>
		/// <param name="region">The cache region name.</param>
		/// <param name="expiration">The cached items expiration.</param>
		/// <param name="sliding">Whether the expiration is sliding or not.</param>
		public CacheConfig(string region, string expiration, string sliding)
		{
			regionName = region;
			properties = new Dictionary<string, string>();
			if (!string.IsNullOrEmpty(expiration))
				properties["expiration"] = expiration;
			if (!string.IsNullOrEmpty(sliding))
				properties["cache.use_sliding_expiration"] = sliding;
		}

		/// <summary>The cache region name.</summary>
		public string Region
		{
			get { return regionName; }
		}

		/// <summary>The cache configuration properties.</summary>
		public IDictionary<string,string> Properties
		{
			get { return properties; }
		}
	}
}
