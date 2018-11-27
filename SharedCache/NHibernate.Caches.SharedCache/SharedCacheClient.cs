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
	/// <summary>
	/// Pluggable cache implementation using indeXus.Net Shared Cache.
	/// </summary>
	public class SharedCacheClient : CacheBase
	{
		private static readonly INHibernateLogger log;
		private readonly string region;

		static SharedCacheClient()
		{
			log = NHibernateLogger.For(typeof(SharedCacheClient));
		}

		/// <summary>
		/// Default constructor.
		/// </summary>
		public SharedCacheClient() : this("nhibernate", null) {}

		/// <summary>
		/// Constructor with no properties.
		/// </summary>
		/// <param name="regionName">The cache region name.</param>
		public SharedCacheClient(string regionName) : this(regionName, null) {}

		/// <summary>
		/// Full constructor.
		/// </summary>
		/// <param name="regionName">The cache region name.</param>
		/// <param name="properties">The cache configuration properties.</param>
		public SharedCacheClient(string regionName, IDictionary<string, string> properties)
		{
			region = regionName;

			if (properties != null) {}
		}

		/// <inheritdoc />
		public override object Get(object key)
		{
			if (key == null)
			{
				return null;
			}
			log.Debug("fetching object {0} from the cache", key);

			return IndexusDistributionCache.SharedCache.Get(key.ToString());
		}

		/// <inheritdoc />
		public override void Put(object key, object value)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key", "null key not allowed");
			}
			if (value == null)
			{
				throw new ArgumentNullException("value", "null value not allowed");
			}

			log.Debug("setting value for item {0}", key);

			IndexusDistributionCache.SharedCache.Add(key.ToString(), value);
		}

		/// <inheritdoc />
		public override void Remove(object key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			log.Debug("removing item {0}", key);

			IndexusDistributionCache.SharedCache.Remove(key.ToString());
		}

		/// <inheritdoc />
		public override void Clear()
		{
			IndexusDistributionCache.SharedCache.Clear();
		}

		/// <inheritdoc />
		public override void Destroy()
		{
			Clear();
		}

		/// <inheritdoc />
		public override object Lock(object key) => null;

		/// <inheritdoc />
		public override void Unlock(object key, object lockValue) {}

		/// <inheritdoc />
		public override long NextTimestamp()
		{
			return Timestamper.Next();
		}

		/// <inheritdoc />
		public override int Timeout
		{
			get { return Timestamper.OneMs * 60000; } // 60 seconds
		}

		/// <inheritdoc />
		public override string RegionName
		{
			get { return region; }
		}
	}
}
