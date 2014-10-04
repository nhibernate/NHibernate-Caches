using System.Collections.Generic;
using System.Configuration;
using System.Xml;

namespace NHibernate.Caches.RtMemoryCache
{
	/// <summary>
	/// Config file provider
	/// </summary>
	public class RtMemoryCacheSectionHandler : IConfigurationSectionHandler
	{
		#region IConfigurationSectionHandler Members

		/// <summary>
		/// parse the config section
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="configContext"></param>
		/// <param name="section"></param>
		/// <returns>an array of CacheConfig objects</returns>
		public object Create(object parent, object configContext, XmlNode section)
		{
			var caches = new List<CacheConfig>();
			XmlNodeList nodes = section.SelectNodes("cache");
			foreach (XmlNode node in nodes)
			{
				string region = null;
				string expiration = null;
				string priority = "3";
				XmlAttribute r = node.Attributes["region"];
				XmlAttribute e = node.Attributes["expiration"];
				XmlAttribute p = node.Attributes["priority"];
				if (r != null)
				{
					region = r.Value;
				}
				if (e != null)
				{
					expiration = e.Value;
				}
				if (p != null)
				{
					priority = p.Value;
				}
				if (region != null && expiration != null)
				{
					caches.Add(new CacheConfig(region, expiration, priority));
				}
			}
			return caches.ToArray();
		}

		#endregion
	}
}