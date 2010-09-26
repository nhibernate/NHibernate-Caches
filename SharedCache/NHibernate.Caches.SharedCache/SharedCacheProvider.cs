#region License

//
//  SharedCache - A cache provider for NHibernate using indeXus.Net Shared Cache
//  (http://www.sharedcache.com/).
//
//  This library is free software; you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation; either
//  version 2.1 of the License, or (at your option) any later version.
//
//  This library is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//  Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// CLOVER:OFF
//

#endregion

using System.Collections.Generic;
using System.Configuration;
using System.Text;
using NHibernate.Cache;

namespace NHibernate.Caches.SharedCache
{
	/// <summary>
	/// SharedCache - A cache provider for NHibernate using indeXus.Net Shared Cache
	///  (http://www.sharedcache.com/)
	/// </summary>
	public class SharedCacheProvider : ICacheProvider
	{
		private static readonly IInternalLogger log;

		static SharedCacheProvider()
		{
			log = LoggerProvider.LoggerFor((typeof(SharedCacheProvider)));
			var configs = ConfigurationManager.GetSection("sharedcache") as SharedCacheConfig[];
		}

		/// <summary>
		/// Configure the cache
		/// </summary>
		/// <param name="regionName">the name of the cache region</param>
		/// <param name="properties">configuration settings</param>
		/// <returns></returns>
		public ICache BuildCache(string regionName, IDictionary<string, string> properties)
		{
			if (regionName == null)
			{
				regionName = "";
			}
			if (properties == null)
			{
				properties = new Dictionary<string, string>();
			}
			if (log.IsDebugEnabled)
			{
				var sb = new StringBuilder();
				foreach (var de in properties)
				{
					sb.Append("name=");
					sb.Append(de.Key);
					sb.Append("&value=");
					sb.Append(de.Value);
					sb.Append(";");
				}
				log.Debug("building cache with region: " + regionName + ", properties: " + sb);
			}
			return new SharedCacheClient(regionName, properties);
		}

		/// <summary>
		/// generate a timestamp
		/// </summary>
		/// <returns></returns>
		public long NextTimestamp()
		{
			return Timestamper.Next();
		}

		/// <summary>
		/// Callback to perform any necessary initialization of the underlying cache implementation
		/// during ISessionFactory construction.
		/// </summary>
		/// <param name="properties">current configuration settings</param>
		public void Start(IDictionary<string, string> properties) {}

		/// <summary>
		/// Callback to perform any necessary cleanup of the underlying cache implementation
		/// during <see cref="M:NHibernate.ISessionFactory.Close"/>.
		/// </summary>
		public void Stop() {}
	}
}