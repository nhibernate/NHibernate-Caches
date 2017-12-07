using System.Collections.Generic;
using NHibernate.Cache;

namespace NHibernate.Caches.SysCache2
{
	/// <summary>
	/// Cache provider using the System.Web.Caching classes.
	/// </summary>
	public class SysCacheProvider : ICacheProvider
	{
		/// <summary>pre configured cache region settings</summary>
		private static readonly Dictionary<string, SysCacheRegion> CacheRegions;

		/// <summary>list of pre configured already built cache regions</summary>
		private static readonly CacheRegionCollection CacheRegionSettingsList;

		/// <summary>log4net logger</summary>
		private static readonly IInternalLogger Log;

		/// <summary>synchronizing object for the cache regions dictionary</summary>
		private static readonly object RegionsSyncRoot = new object();

		/// <summary>
		/// Initializes the <see cref="SysCacheProvider"/> class.
		/// </summary>
		static SysCacheProvider()
		{
			Log = LoggerProvider.LoggerFor(typeof(SysCacheProvider));
			// We need to determine which cache regions are configured in the configuration file, but we cant create the
			// cache regions at this time because there could be nhibernate configuration values
			// that we need for the cache regions such as connection info to be used for data dependencies. But this info
			// isn't available until build cache is called. So allocate space but only create them on demand.

			var configSection = SysCacheSection.GetSection();

			if (configSection != null && configSection.CacheRegions.Count > 0)
			{
				CacheRegionSettingsList = configSection.CacheRegions;
				CacheRegions = new Dictionary<string, SysCacheRegion>(CacheRegionSettingsList.Count);
			}
			else
			{
				CacheRegions = new Dictionary<string, SysCacheRegion>(0);
				Log.Info(
					"No cache regions specified. Cache regions can be specified in sysCache configuration section with custom settings.");
			}
		}

		#region ICacheProvider Members

		/// <inheritdoc />
		public ICache BuildCache(string regionName, IDictionary<string, string> properties)
		{
			// Return a configured cache region if we have one for the region already.
			// This may happen if there is a query cache specified for a region that is configured,
			// since query caches are not configured at session factory startup. This may also happen
			// if many session factories are built.
			// This cache avoids to duplicate the configured SQL dependencies registration in above cases.
			if (!string.IsNullOrEmpty(regionName) && CacheRegions.TryGetValue(regionName, out var cache))
			{
				return cache;
			}

			// Build the cache from preconfigured values if the region has configuration values
			if (CacheRegionSettingsList != null)
			{
				var regionSettings = regionName == null ? null : CacheRegionSettingsList[regionName];

				if (regionSettings != null)
				{
					SysCacheRegion cacheRegion;

					lock (RegionsSyncRoot)
					{
						// Note that the only reason we have to do this double check is because the query cache
						// can try to create caches at unpredictable times.
						if (CacheRegions.TryGetValue(regionName, out cacheRegion) == false)
						{
							if (Log.IsDebugEnabled)
							{
								Log.DebugFormat("building cache region, '{0}', from configuration", regionName);
							}

							//build the cache region with settings and put it into the list so that this proces will not occur again
							cacheRegion = new SysCacheRegion(regionName, regionSettings, properties);
							CacheRegions[regionName] = cacheRegion;
						}
					}

					return cacheRegion;
				}
			}

			if (Log.IsDebugEnabled)
			{
				Log.DebugFormat("building non-configured cache region : {0}", regionName);
			}

			//we will end up creating cache regions here for cache regions that nhibernate
			//uses internally and cache regions that weren't specified in the application config file
			return new SysCacheRegion(regionName, properties);
		}

		/// <inheritdoc />
		public long NextTimestamp()
		{
			return Timestamper.Next();
		}

		/// <inheritdoc />
		public void Start(IDictionary<string, string> properties)
		{
		}

		/// <inheritdoc />
		public void Stop()
		{
		}

		#endregion
	}
}
