using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NHibernate.Cache;

namespace NHibernate.Caches.SysCache2
{
	/// <summary>
	/// Cache provider using the System.Web.Caching classes.
	/// </summary>
	public class SysCacheProvider : ICacheProvider
	{
		/// <summary>Pre-configured cache region settings.</summary>
		private static readonly ConcurrentDictionary<string, Lazy<ICache>> CacheRegions = new ConcurrentDictionary<string, Lazy<ICache>>();

		/// <summary>List of pre configured already built cache regions.</summary>
		private static readonly Dictionary<string, CacheRegionElement> CacheRegionSettings;

		/// <summary>Log4net logger.</summary>
		private static readonly IInternalLogger Log;

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
				CacheRegionSettings = new Dictionary<string, CacheRegionElement>(configSection.CacheRegions.Count);
				foreach (var cacheRegion in configSection.CacheRegions)
				{
					if (cacheRegion is CacheRegionElement element)
						CacheRegionSettings.Add(element.Name, element);
				}
			}
			else
			{
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
			if (!string.IsNullOrEmpty(regionName)
				// We do not cache non-configured caches, so must first look-up settings for knowing if it
				// is a configured one.
				&& CacheRegionSettings != null
				&& CacheRegionSettings.TryGetValue(regionName, out var regionSettings))
			{
				// The Lazy<T> is required for ensuring the cache is built only once. ConcurrentDictionary
				// may run concurrently the value factory for the same key, but it will yield only one
				// of the resulting Lazy<T>. The lazy will then actually build the cache when accessing its
				// value after having obtained it, and it will not do that concurrently.
				// https://stackoverflow.com/a/31637510/1178314
				var cache = CacheRegions.GetOrAdd(regionName,
					r => new Lazy<ICache>(() => BuildCache(r, properties, regionSettings)));
				return cache.Value;
			}

			// We will end up creating cache regions here for cache regions that NHibernate
			// uses internally and cache regions that weren't specified in the application config file
			return BuildCache(regionName, properties, null);
		}

		private ICache BuildCache(string regionName, IDictionary<string, string> properties, CacheRegionElement settings)
		{
			if (Log.IsDebugEnabled)
			{
				Log.DebugFormat(
					settings != null
						? "building cache region, '{0}', from configuration"
						: "building non-configured cache region : {0}", regionName);
			}
			return new SysCacheRegion(regionName, settings, properties);
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
