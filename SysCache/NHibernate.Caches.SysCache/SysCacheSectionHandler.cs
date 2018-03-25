using System.Collections.Generic;
using System.Configuration;
using System.Xml;

namespace NHibernate.Caches.SysCache
{
	/// <summary>
	/// Config file provider.
	/// </summary>
	public class SysCacheSectionHandler : IConfigurationSectionHandler
	{
		#region IConfigurationSectionHandler Members

		/// <inheritdoc />
		/// <returns>An array of <see cref="CacheConfig"/> objects.</returns>
		public object Create(object parent, object configContext, XmlNode section)
		{
			var caches = new List<CacheConfig>();
			XmlNodeList nodes = section.SelectNodes("cache");
			foreach (XmlNode node in nodes)
			{
				string region = null;
				string expiration = null;
				string sliding = null;
				var priority = "3";
				var r = node.Attributes["region"];
				var e = node.Attributes["expiration"];
				var s = node.Attributes["sliding"];
				var p = node.Attributes["priority"];
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
				if (p != null)
				{
					priority = p.Value;
				}
				if (region != null)
				{
					caches.Add(new CacheConfig(region, expiration, sliding, priority));
				}
			}
			return caches.ToArray();
		}

		#endregion
	}
}
