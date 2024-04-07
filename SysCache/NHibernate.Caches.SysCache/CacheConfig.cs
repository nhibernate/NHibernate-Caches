using System.Collections.Generic;

namespace NHibernate.Caches.SysCache
{
	/// <summary>
	/// Config properties of a cache region.
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
		/// <param name="priority">The cached items priority.</param>
		public CacheConfig(string region, string expiration, string priority) :
			this(region, expiration, null, priority)
		{
		}

		/// <summary>
		/// build a configuration
		/// </summary>
		/// <param name="region">The cache region name.</param>
		/// <param name="expiration">The cached items expiration.</param>
		/// <param name="sliding">Whether the expiration is sliding or not.</param>
		/// <param name="priority">The cached items priority.</param>
		public CacheConfig(string region, string expiration, string sliding, string priority)
		{
			regionName = region;
			properties = new Dictionary<string, string>();
			if (!string.IsNullOrEmpty(expiration))
				properties["expiration"] = expiration;
			if (!string.IsNullOrEmpty(priority))
				properties["priority"] = priority;
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
