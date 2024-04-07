using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Text;
using Enyim.Caching;
using Enyim.Caching.Configuration;
using NHibernate.Cache;

namespace NHibernate.Caches.EnyimMemcached
{
	/// <summary>
	/// Cache provider using the .NET client EnyimMemcached (http://github.com/enyim/EnyimMemcached)
	/// for memcached, which is located at http://memcached.org/.
	/// </summary>
	public class MemCacheProvider : ICacheProvider
	{
		private static readonly INHibernateLogger log;
		private static MemcachedClient clientInstance;
		private static readonly IMemcachedClientConfiguration config;
		private static readonly object syncObject = new object();
		private static int _usageCount;

		static MemCacheProvider()
		{
			log = NHibernateLogger.For(typeof(MemCacheProvider));
			config = ConfigurationManager.GetSection("enyim.com/memcached") as IMemcachedClientConfiguration;
			if (config == null)
			{
				log.Info("enyim.com/memcached configuration section not found, using default configuration (127.0.0.1:11211).");
				config = new MemcachedClientConfiguration();
				config.Servers.Add(new IPEndPoint(IPAddress.Loopback, 11211));
			}
		}

		#region ICacheProvider Members

		/// <inheritdoc />
#pragma warning disable 618
		public ICache BuildCache(string regionName, IDictionary<string, string> properties)
#pragma warning restore 618
		{
			if (regionName == null)
			{
				regionName = "";
			}
			if (properties == null)
			{
				properties = new Dictionary<string, string>();
			}
			if (log.IsDebugEnabled())
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
				log.Debug("building cache with region: {0}, properties: {1}", regionName, sb);
			}
			return new MemCacheClient(regionName, properties, clientInstance);
		}

		/// <inheritdoc />
		public long NextTimestamp()
		{
			return Timestamper.Next();
		}

		/// <inheritdoc />
		public void Start(IDictionary<string, string> properties)
		{
			// Needs to lock staticly because the pool and the internal maintenance thread
			// are both static, and I want them syncs between starts and stops.
			lock (syncObject)
			{
				if (config == null)
				{
					throw new ConfigurationErrorsException("Configuration for enyim.com/memcached not found");
				}
				if (clientInstance == null)
				{
					clientInstance = new MemcachedClient(config);
				}
				_usageCount++;
			}
		}

		/// <inheritdoc />
		public void Stop()
		{
			lock (syncObject)
			{
				_usageCount--;
				if (_usageCount <= 0)
				{
					clientInstance?.Dispose();
					clientInstance = null;
					_usageCount = 0;
				}
			}
		}

		#endregion
	}
}
