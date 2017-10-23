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
				if (region != null && expiration != null)
				{
					caches.Add(new CacheConfig(region, expiration, sliding));
				}
			}
			return caches.ToArray();
		}

		#endregion
	}
}
