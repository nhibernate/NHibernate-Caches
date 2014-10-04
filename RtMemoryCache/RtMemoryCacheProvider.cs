#region License

//
//  RtMemoryCache - A cache provider for NHibernate using System.Runtime.Caching.MemoryCache.
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

#endregion

using System.Collections.Generic;
using System.Configuration;
using System.Text;
using NHibernate.Cache;

namespace NHibernate.Caches.RtMemoryCache
{
	/// <summary>
	/// Cache provider using the System.Runtime.Caching classes
	/// </summary>
	public class RtMemoryCacheProvider : ICacheProvider
	{
		private static readonly Dictionary<string, ICache> caches;
		private static readonly IInternalLogger log;

        static RtMemoryCacheProvider()
		{
            log = LoggerProvider.LoggerFor(typeof(RtMemoryCacheProvider));
			caches = new Dictionary<string, ICache>();

			var list = ConfigurationManager.GetSection("rtmemorycache") as CacheConfig[];
			if (list != null)
			{
				foreach (CacheConfig cache in list)
				{
                    caches.Add(cache.Region, new RtMemoryCache(cache.Region, cache.Properties));
				}
			}
		}

		#region ICacheProvider Members

		/// <summary>
		/// build a new RtMemoryCache
		/// </summary>
		/// <param name="regionName"></param>
		/// <param name="properties"></param>
		/// <returns></returns>
		public ICache BuildCache(string regionName, IDictionary<string, string> properties)
		{
			if (regionName == null)
			{
				regionName = string.Empty;
			}

			ICache result;
			if(caches.TryGetValue(regionName, out result))
			{
				return result;
			}

			// create cache
			if (properties == null)
			{
				properties = new Dictionary<string, string>(1);
			}
				
			if (log.IsDebugEnabled)
			{
				var sb = new StringBuilder();
				sb.Append("building cache with region: ").Append(regionName).Append(", properties: ");

				foreach (KeyValuePair<string, string> de in properties)
				{
					sb.Append("name=");
					sb.Append(de.Key);
					sb.Append("&value=");
					sb.Append(de.Value);
					sb.Append(";");
				}
				log.Debug(sb.ToString());
			}
            return new RtMemoryCache(regionName, properties);
		}

		public long NextTimestamp()
		{
			return Timestamper.Next();
		}

		public void Start(IDictionary<string, string> properties) {}

		public void Stop() {}

		#endregion
	}
}