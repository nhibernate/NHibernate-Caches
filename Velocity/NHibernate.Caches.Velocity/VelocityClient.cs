#region License

//Microsoft Public License (Ms-PL)
//
//This license governs use of the accompanying software. If you use the software, you accept this license. If you do not accept the license, do not use the software.
//
//1. Definitions
//
//The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under U.S. copyright law.
//
//A "contribution" is the original software, or any additions or changes to the software.
//
//A "contributor" is any person that distributes its contribution under this license.
//
//"Licensed patents" are a contributor's patent claims that read directly on its contribution.
//
//2. Grant of Rights
//
//(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
//
//(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
//
//3. Conditions and Limitations
//
//(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
//
//(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
//
//(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
//
//(D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
//
//(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.

#endregion

using System;
using System.Collections.Generic;
using System.Data.Caching;
using NHibernate.Cache;
using CacheException=System.Data.Caching.CacheException;
using CacheFactory=System.Data.Caching.CacheFactory;

namespace NHibernate.Caches.Velocity
{
	/// <summary>
	/// Pluggable cache implementation using the Velocity cache.
	/// </summary>
	public class VelocityClient : CacheBase
	{
		private const string CacheName = "nhibernate";
		private static readonly INHibernateLogger log;
		private readonly System.Data.Caching.Cache cache;
		private readonly string region;

		static VelocityClient()
		{
			log = NHibernateLogger.For(typeof(VelocityClient));
		}

		/// <summary>
		/// Default constructor.
		/// </summary>
		public VelocityClient() : this("nhibernate", null) {}

		/// <summary>
		/// Constructor with no properties.
		/// </summary>
		/// <param name="regionName">The cache region name.</param>
		public VelocityClient(string regionName) : this(regionName, null) {}

		/// <summary>
		/// Full constructor.
		/// </summary>
		/// <param name="regionName">The cache region name.</param>
		/// <param name="properties">The cache configuration properties.</param>
		public VelocityClient(string regionName, IDictionary<string, string> properties)
		{
			region = regionName.GetHashCode().ToString(); //because the region name length is limited
			var cacheCluster = new CacheFactory();
			cache = cacheCluster.GetCache(CacheName);
			try
			{
				cache.CreateRegion(region, true);
			}
			catch (CacheException) {}
		}

		#region CacheBase Members

		/// <inheritdoc />
		public override object Get(object key)
		{
			if (key == null)
			{
				return null;
			}
			log.Debug("fetching object {0} from the cache", key);

			CacheItemVersion version = null;
			return cache.Get(region, key.ToString(), ref version);
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

			cache.Put(region, key.ToString(), value, null, null);
		}

		/// <inheritdoc />
		public override void Remove(object key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			log.Debug("removing item {0}", key);

			if (Get(key.ToString()) != null)
			{
				cache.Remove(region, key.ToString());
			}
		}

		/// <inheritdoc />
		public override void Clear()
		{
			cache.ClearRegion(region);
		}

		/// <inheritdoc />
		public override void Destroy()
		{
			Clear();
		}

		/// <inheritdoc />
		public override object Lock(object key)
		{
			var lockHandle = new LockHandle();
			if (Get(key.ToString()) != null)
			{
				try
				{
					cache.GetAndLock(region, key.ToString(), TimeSpan.FromMilliseconds(Timeout), out lockHandle);
				}
				catch (CacheException) {}
			}

			return lockHandle;
		}

		/// <inheritdoc />
		public override void Unlock(object key, object lockValue)
		{
			var lockHandle = lockValue as LockHandle? ?? new LockHandle();
			if (Get(key.ToString()) != null)
			{
				try
				{
					cache.Unlock(region, key.ToString(), lockHandle);
				}
				catch (CacheException) {}
			}
		}

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

		#endregion
	}
}
