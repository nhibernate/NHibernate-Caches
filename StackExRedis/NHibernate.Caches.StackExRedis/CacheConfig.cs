using System;

namespace NHibernate.Caches.StackExRedis
{
	/// <summary>
	/// Cache configuration properties.
	/// </summary>
	public class CacheConfig
	{
		/// <summary>
		/// Build a cache configuration.
		/// </summary>
		/// <param name="configuration">The redis configuration</param>
		/// <param name="regions">The configured cache regions.</param>
		public CacheConfig(string configuration, RegionConfig[] regions)
		{
			Regions = regions;
			Configuration = configuration;
		}

		/// <summary>
		/// The configured cache regions.
		/// </summary>
		public RegionConfig[] Regions { get; }

		/// <summary>
		/// The StackExchange.Redis configuration string.
		/// </summary>
		public string Configuration { get; }
	}
}
