using System;
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
				var appendHashcode = node.Attributes["append-hashcode"]?.Value;
				var serializer = node.Attributes["serializer"]?.Value;
				if (region != null)
				{
					caches.Add(new RegionConfig(region, expiration, sliding, appendHashcode, serializer));
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

			var appendHashcodeToKey =
// 6.0 TODO: disable for all cases by default (so keep only the else code)
#if NETFX
				!StringComparer.OrdinalIgnoreCase.Equals(
					section.Attributes?["append-hashcode"]?.Value, "false");
#else
				StringComparer.OrdinalIgnoreCase.Equals(
					section.Attributes?["append-hashcode"]?.Value, "true");
#endif

			return new CacheConfig(factoryClass, properties, caches.ToArray(), appendHashcodeToKey);
		}

		#endregion
	}
}
