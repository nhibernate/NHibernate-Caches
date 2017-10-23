using System.Collections.Generic;

namespace NHibernate.Caches.RtMemoryCache
{
	/// <summary>
	/// Config properties
	/// </summary>
	public class CacheConfig
	{
		private readonly Dictionary<string, string> properties;
		private readonly string regionName;

		/// <summary>
		/// build a configuration
		/// </summary>
		/// <param name="region"></param>
		/// <param name="expiration"></param>
		/// <param name="sliding"></param>
		public CacheConfig(string region, string expiration, string sliding)
		{
			regionName = region;
			properties = new Dictionary<string, string>();
			if (!string.IsNullOrEmpty(expiration))
				properties["expiration"] = expiration;
			if (!string.IsNullOrEmpty(sliding))
				properties["cache.use_sliding_expiration"] = sliding;
		}

		/// <summary></summary>
		public string Region
		{
			get { return regionName; }
		}

		/// <summary></summary>
		public IDictionary<string,string> Properties
		{
			get { return properties; }
		}
	}
}
