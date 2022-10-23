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
using System.Threading;
using System.Threading.Tasks;
using MergeSystem.Indexus.WinServiceCommon.Provider.Cache;
using NHibernate.Cache;

namespace NHibernate.Caches.SharedCache
{
	// 6.0 TODO: replace that class by its base
	/// <summary>
	/// Pluggable cache implementation using indeXus.Net Shared Cache.
	/// </summary>
	public class SharedCacheClient : SharedCacheClientBase,
#pragma warning disable 618
		ICache
#pragma warning restore 618
	{
		/// <summary>
		/// Default constructor.
		/// </summary>
		public SharedCacheClient()
		{
		}

		/// <summary>
		/// Constructor with no properties.
		/// </summary>
		/// <param name="regionName">The region of the cache.</param>
		public SharedCacheClient(string regionName)
			: base(regionName)
		{
		}

		/// <summary>
		/// Full constructor.
		/// </summary>
		/// <param name="regionName">The cache region name.</param>
		/// <param name="properties">The configuration properties.</param>
		public SharedCacheClient(string regionName, IDictionary<string, string> properties)
			: base(regionName, properties)
		{
		}

		/// <inheritdoc />
		public new Task<object> GetAsync(object key, CancellationToken cancellationToken)
			=> base.GetAsync(key, cancellationToken);

		/// <inheritdoc />
		public new Task PutAsync(object key, object value, CancellationToken cancellationToken)
			=> base.PutAsync(key, value, cancellationToken);

		/// <inheritdoc />
		public new Task RemoveAsync(object key, CancellationToken cancellationToken)
			=> base.RemoveAsync(key, cancellationToken);

		/// <inheritdoc />
		public new Task ClearAsync(CancellationToken cancellationToken)
			=> base.ClearAsync(cancellationToken);

		/// <inheritdoc />
		public new Task LockAsync(object key, CancellationToken cancellationToken)
			=> base.LockAsync(key, cancellationToken);

		/// <inheritdoc />
		public Task UnlockAsync(object key, CancellationToken cancellationToken)
			=> base.UnlockAsync(key, null, cancellationToken);

		/// <inheritdoc />
		public new string RegionName => base.RegionName;

		/// <inheritdoc />
		public new object Get(object key)
			=> base.Get(key);

		/// <inheritdoc />
		public new void Put(object key, object value)
			=> base.Put(key, value);

		/// <inheritdoc />
		public new void Remove(object key)
			=> base.Remove(key);

		/// <inheritdoc />
		public new void Clear()
			=> base.Clear();

		/// <inheritdoc />
		public new void Destroy()
			=> base.Destroy();

		/// <inheritdoc />
		public new void Lock(object key)
			=> base.Lock(key);

		/// <inheritdoc />
		public void Unlock(object key)
			=> base.Unlock(key, null);

		/// <inheritdoc />
		public new long NextTimestamp()
			=> base.NextTimestamp();

		/// <inheritdoc />
		public new int Timeout => base.Timeout;
	}

	/// <summary>
	/// Pluggable cache implementation using indeXus.Net Shared Cache.
	/// </summary>
	public abstract class SharedCacheClientBase : CacheBase
	{
		private static readonly INHibernateLogger log;
		private readonly string region;

		static SharedCacheClientBase()
		{
			log = NHibernateLogger.For(typeof(SharedCacheClient));
		}

		/// <summary>
		/// Default constructor.
		/// </summary>
		public SharedCacheClientBase() : this("nhibernate", null) {}

		/// <summary>
		/// Constructor with no properties.
		/// </summary>
		/// <param name="regionName">The cache region name.</param>
		public SharedCacheClientBase(string regionName) : this(regionName, null) {}

		/// <summary>
		/// Full constructor.
		/// </summary>
		/// <param name="regionName">The cache region name.</param>
		/// <param name="properties">The cache configuration properties.</param>
		public SharedCacheClientBase(string regionName, IDictionary<string, string> properties)
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
