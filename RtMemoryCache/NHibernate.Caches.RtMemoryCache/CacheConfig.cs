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
		/// <param name="priority"></param>
		public CacheConfig(string region, string expiration, string priority)
		{
			regionName = region;
			properties = new Dictionary<string, string> {{"expiration", expiration}, {"priority", priority}};
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