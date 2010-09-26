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

using System;
using System.Collections.Generic;
using MergeSystem.Indexus.WinServiceCommon.Provider.Cache;
using NHibernate.Cache;

namespace NHibernate.Caches.SharedCache
{
	public class SharedCacheClient : ICache
	{
		private static readonly IInternalLogger log;
		private readonly string region;

		static SharedCacheClient()
		{
			log = LoggerProvider.LoggerFor(typeof(SharedCacheClient));
		}

		public SharedCacheClient() : this("nhibernate", null) {}

		public SharedCacheClient(string regionName) : this(regionName, null) {}

		public SharedCacheClient(string regionName, IDictionary<string, string> properties)
		{
			region = regionName;

			if (properties != null) {}
		}

		public object Get(object key)
		{
			if (key == null)
			{
				return null;
			}
			if (log.IsDebugEnabled)
			{
				log.DebugFormat("fetching object {0} from the cache", key);
			}

			return IndexusDistributionCache.SharedCache.Get(key.ToString());
		}

		public void Put(object key, object value)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key", "null key not allowed");
			}
			if (value == null)
			{
				throw new ArgumentNullException("value", "null value not allowed");
			}

			if (log.IsDebugEnabled)
			{
				log.DebugFormat("setting value for item {0}", key);
			}

			IndexusDistributionCache.SharedCache.Add(key.ToString(), value);
		}

		public void Remove(object key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			if (log.IsDebugEnabled)
			{
				log.DebugFormat("removing item {0}", key);
			}

			IndexusDistributionCache.SharedCache.Remove(key.ToString());
		}

		public void Clear()
		{
			IndexusDistributionCache.SharedCache.Clear();
		}

		public void Destroy()
		{
			Clear();
		}

		public void Lock(object key) {}

		public void Unlock(object key) {}

		public long NextTimestamp()
		{
			return Timestamper.Next();
		}

		public int Timeout
		{
			get { return Timestamper.OneMs * 60000; } // 60 seconds
		}

		public string RegionName
		{
			get { return region; }
		}
	}
}