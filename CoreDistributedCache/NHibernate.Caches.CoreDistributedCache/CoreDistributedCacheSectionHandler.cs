using System.Collections.Generic;
using System.Configuration;
using System.Xml;

namespace NHibernate.Caches.CoreDistributedCache
{
	/// <summary>
	/// Configuration file provider.
	/// </summary>
	public class CoreDistributedCacheSectionHandler : IConfigurationSectionHandler
	{
		private static readonly INHibernateLogger Log = NHibernateLogger.For(typeof(CoreDistributedCacheSectionHandler));

		#region IConfigurationSectionHandler Members

		/// <inheritdoc />
		/// <returns>A <see cref="CacheConfig" /> object.</returns>
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

			var factoryClass = section.Attributes?["factory-class"]?.Value;
			var properties = new Dictionary<string, string>();
			nodes = section.SelectNodes("properties/property");
			foreach (XmlNode node in nodes)
			{
				var name = node.Attributes["name"]?.Value;
				if (name != null)
				{
					properties.Add(name, node.InnerText);
				}
				else
				{
					Log.Warn("Found a cache property node lacking a name: ignored. Node: {0}",
						node.OuterXml);
				}
			}

			return new CacheConfig(factoryClass, properties, caches.ToArray());
		}

		#endregion
	}
}
