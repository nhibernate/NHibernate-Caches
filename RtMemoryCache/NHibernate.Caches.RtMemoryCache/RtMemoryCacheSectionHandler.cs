using System.Collections.Generic;
using System.Xml;
using NHibernate.Caches.Common;

namespace NHibernate.Caches.RtMemoryCache
{
	/// <summary>
	/// Config file provider.
	/// </summary>
	public class RtMemoryCacheSectionHandler : ICacheConfigurationSectionHandler
	{
		private static readonly INHibernateLogger Log = NHibernateLogger.For(typeof(RtMemoryCacheSectionHandler));

		#region IConfigurationSectionHandler Members

		/// <inheritdoc />
		/// <returns>An array of <see cref="CacheConfig" /> objects.</returns>
		public object Create(object parent, object configContext, XmlNode section)
		{
			var caches = new List<CacheConfig>();
			XmlNodeList nodes = section.SelectNodes("cache");
			foreach (XmlNode node in nodes)
			{
				string region = null;
				string expiration = null;
				string sliding = null;
				var r = node.Attributes["region"];
				var e = node.Attributes["expiration"];
				var s = node.Attributes["sliding"];
				if (r != null)
				{
					region = r.Value;
				}
				if (e != null)
				{
					expiration = e.Value;
				}
				if (s != null)
				{
					sliding = s.Value;
				}
				if (region != null)
				{
					caches.Add(new CacheConfig(region, expiration, sliding));
				}
				else
				{
					Log.Warn("Found a cache node lacking a region name: ignored. Node: {0}",
						node.OuterXml);
				}
			}
			return caches.ToArray();
		}

		#endregion

		/// <inheritdoc />
		public string ConfigurationSectionName => "rtmemorycache";
	}
}
