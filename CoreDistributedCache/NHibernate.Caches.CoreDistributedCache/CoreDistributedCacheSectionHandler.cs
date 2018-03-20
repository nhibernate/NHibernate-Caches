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

		/// <summary>
		/// Parse the config section.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="configContext"></param>
		/// <param name="section"></param>
		/// <returns>An array of CacheConfig objects.</returns>
		public object Create(object parent, object configContext, XmlNode section)
		{
			var caches = new List<CacheConfig>();

			var fc = section.Attributes?["factory-class"];
			if (fc != null)
			{
				caches.Add(new CacheConfig(fc.Value));
			}
			
			var nodes = section.SelectNodes("cache");
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
	}
}
