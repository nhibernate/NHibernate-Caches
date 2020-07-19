using System.Collections.Generic;
using System.Xml;
using NHibernate.Caches.Common;

namespace NHibernate.Caches.CoreMemoryCache
{
	/// <summary>
	/// Configuration file provider.
	/// </summary>
	public class CoreMemoryCacheSectionHandler : ICacheConfigurationSectionHandler
	{
		private static readonly INHibernateLogger Log = NHibernateLogger.For(typeof(CoreMemoryCacheSectionHandler));

		#region IConfigurationSectionHandler Members

		/// <inheritdoc />
		/// <returns>A <see cref="T:NHibernate.Caches.CoreMemoryCache.CacheConfig" /> object.</returns>
		public object Create(object parent, object configContext, XmlNode section)
		{
			var caches = new List<RegionConfig>();

			var nodes = section.SelectNodes("cache");
			foreach (XmlNode node in nodes)
			{
				var region = node.Attributes["region"]?.Value;
				var expiration = node.Attributes["expiration"]?.Value;
				var sliding = node.Attributes["sliding"]?.Value;
				if (region != null)
				{
					caches.Add(new RegionConfig(region, expiration, sliding));
				}
				else
				{
					Log.Warn("Found a cache region node lacking a region name: ignored. Node: {0}",
						node.OuterXml);
				}
			}

			var esf = section.Attributes?["expiration-scan-frequency"]?.Value;
			return new CacheConfig(esf, caches.ToArray());
		}

		#endregion

		/// <inheritdoc />
		public string ConfigurationSectionName => "corememorycache";
	}
}
