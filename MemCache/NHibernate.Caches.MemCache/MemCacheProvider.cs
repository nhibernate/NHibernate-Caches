#region License

//
//  MemCache - A cache provider for NHibernate using the .NET client
//  (http://sourceforge.net/projects/memcacheddotnet) for memcached,
//  which is located at http://www.danga.com/memcached/.
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
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using System.Text;
using Memcached.ClientLibrary;
using NHibernate.Cache;

namespace NHibernate.Caches.MemCache
{
	/// <summary>
	/// Cache provider using the .NET client (http://sourceforge.net/projects/memcacheddotnet)
	///  for memcached, which is located at http://www.danga.com/memcached/
	/// </summary>
	public class MemCacheProvider : ICacheProvider
	{
		private static readonly IInternalLogger log;
		private static readonly string[] servers;
		private static readonly int[] weights;

		private static readonly object syncObject = new object();

		static MemCacheProvider()
		{
			log = LoggerProvider.LoggerFor((typeof(MemCacheProvider)));
			var configs = ConfigurationManager.GetSection("memcache") as MemCacheConfig[];
			if (configs != null)
			{
				var myWeights = new ArrayList();
				var myServers = new ArrayList();
				foreach (MemCacheConfig config in configs)
				{
					myServers.Add(string.Format("{0}:{1}", config.Host, config.Port));
					if (log.IsDebugEnabled)
					{
						log.DebugFormat("adding config for memcached on host {0}", config.Host);
					}
					if (config.Weight > 0)
					{
						myWeights.Add(config.Weight);
					}
				}
				servers = (string[]) myServers.ToArray(typeof (string));
				weights = (int[]) myWeights.ToArray(typeof (int));
			}
		}

		#region ICacheProvider Members

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
				foreach (var pair in properties)
				{
					sb.Append("name=");
					sb.Append(pair.Key);
					sb.Append("&value=");
					sb.Append(pair.Value);
					sb.Append(";");
				}
				log.Debug("building cache with region: " + regionName + ", properties: " + sb);
			}
			return new MemCacheClient(regionName, properties);
		}

		public long NextTimestamp()
		{
			return Timestamper.Next();
		}

		public void Start(IDictionary<string, string> properties)
		{
			// Needs to lock staticly because the pool and the internal maintenance thread
			// are both static, and I want them syncs between starts and stops.
			lock (syncObject)
			{
				SockIOPool pool = SockIOPool.GetInstance(MemCacheClient.PoolName);
				if (pool.Initialized)
				{
					return;
				}

				bool debugEnabled = log.IsDebugEnabled;

				if (servers != null && servers.Length > 0)
				{
					pool.SetServers(servers);
				}
				if (weights != null && weights.Length > 0 && servers != null && weights.Length == servers.Length)
				{
					pool.SetWeights(weights);
				}
				if (properties.ContainsKey("failover"))
				{
					pool.Failover = Convert.ToBoolean(properties["failover"]);
					if (debugEnabled)
					{
						log.DebugFormat("failover set to {0}", pool.Failover);
					}
				}
				if (properties.ContainsKey("initial_connections"))
				{
					pool.InitConnections = Convert.ToInt32(properties["initial_connections"]);
					if (debugEnabled)
					{
						log.DebugFormat("initial_connections set to {0}", pool.InitConnections);
					}
				}
				if (properties.ContainsKey("maintenance_sleep"))
				{
					pool.MaintenanceSleep = Convert.ToInt64(properties["maintenance_sleep"]);
					if (debugEnabled)
					{
						log.DebugFormat("maintenance_sleep set to {0}", pool.MaintenanceSleep);
					}
				}
				if (properties.ContainsKey("max_busy"))
				{
					pool.MaxBusy = Convert.ToInt64(properties["max_busy"]);
					if (debugEnabled)
					{
						log.DebugFormat("max_busy set to {0}", pool.MaxBusy);
					}
				}
				if (properties.ContainsKey("max_connections"))
				{
					pool.MaxConnections = Convert.ToInt32(properties["max_connections"]);
					if (debugEnabled)
					{
						log.DebugFormat("max_connections set to {0}", pool.MaxConnections);
					}
				}
				if (properties.ContainsKey("max_idle"))
				{
					pool.MaxIdle = Convert.ToInt64(properties["max_idle"]);
					if (debugEnabled)
					{
						log.DebugFormat("max_idle set to {0}", pool.MaxIdle);
					}
				}
				if (properties.ContainsKey("min_connections"))
				{
					pool.MinConnections = Convert.ToInt32(properties["min_connections"]);
					if (debugEnabled)
					{
						log.DebugFormat("min_connections set to {0}", pool.MinConnections);
					}
				}
				if (properties.ContainsKey("nagle"))
				{
					pool.Nagle = Convert.ToBoolean(properties["nagle"]);
					if (debugEnabled)
					{
						log.DebugFormat("nagle set to {0}", pool.Nagle);
					}
				}
				if (properties.ContainsKey("socket_timeout"))
				{
					pool.SocketTimeout = Convert.ToInt32(properties["socket_timeout"]);
					if (debugEnabled)
					{
						log.DebugFormat("socket_timeout set to {0}", pool.SocketTimeout);
					}
				}
				if (properties.ContainsKey("socket_connect_timeout"))
				{
					pool.SocketConnectTimeout = Convert.ToInt32(properties["socket_connect_timeout"]);
					if (debugEnabled)
					{
						log.DebugFormat("socket_connect_timeout set to {0}", pool.SocketConnectTimeout);
					}
				}

				pool.Initialize();
			}
		}

		public void Stop()
		{
			// Needs to lock staticly because the pool and the internal maintenance thread
			// are both static, and I want them syncs between starts and stops.
			lock (syncObject)
			{
				SockIOPool pool = SockIOPool.GetInstance(MemCacheClient.PoolName);
				if (pool.Initialized)
				{
					pool.Shutdown();
				}

				// The maintenance thread must be set to null in the hard way
				// Due to a bug in the memcached client, the thread is not set to null which causes the
				// pool to fail if it needs to be restarted. Hopefully this method is not called too many times
				// (only when the SessionFactory is closed)
				FieldInfo maintenanceThread = typeof (SockIOPool).GetField("_maintenanceThread",
				                                                            BindingFlags.NonPublic | BindingFlags.Instance);
				maintenanceThread.SetValue(pool, null);
			}
		}

		#endregion
	}
}