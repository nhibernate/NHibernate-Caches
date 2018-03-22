using System.Collections.Generic;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Redis;

namespace NHibernate.Caches.CoreDistributedCache.Redis
{
	/// <summary>
	/// A Redis distributed cache factory. See <see cref="RedisCache" />.
	/// </summary>
	public class RedisFactory : IDistributedCacheFactory
	{
		private static readonly INHibernateLogger Log = NHibernateLogger.For(typeof(RedisFactory));
		private const string _configuration = "configuration";
		private const string _instanceName = "instance-name";

		private readonly RedisCacheOptions _options;

		/// <summary>
		/// Constructor with explicit configuration properties.
		/// </summary>
		/// <param name="configuration">See <see cref="RedisCacheOptions.Configuration" />.</param>
		/// <param name="instanceName">See <see cref="RedisCacheOptions.InstanceName" />.</param>
		public RedisFactory(string configuration, string instanceName)
		{
			_options = new RedisCacheOptions
			{
				Configuration = configuration,
				InstanceName = instanceName
			};
		}

		/// <summary>
		/// Constructor with configuration properties. It supports <c>configuration</c> and
		/// <c>instance-name</c> properties. See <see cref="RedisCacheOptions.Configuration" /> and
		/// <see cref="RedisCacheOptions.InstanceName" />.
		/// </summary>
		/// <param name="properties">The configurations properties.</param>
		public RedisFactory(IDictionary<string, string> properties)
		{
			_options = new RedisCacheOptions();

			if (properties == null)
				return;

			if (properties.TryGetValue(_configuration, out var configuration))
			{
				_options.Configuration = configuration;
				Log.Info("Configuration set as '{0}'", configuration);
			}
			else
			{
				// Configuration is supposed to be mandatory.
				Log.Warn("No {0} property provided", _configuration);
			}

			if (properties.TryGetValue(_instanceName, out var instanceName))
			{
				_options.InstanceName = instanceName;
				Log.Info("InstanceName set as '{0}'", instanceName);
			}
			else
				Log.Info("No {0} property provided", _instanceName);
		}

		/// <inheritdoc />
		public int? MaxKeySize => null;

		/// <inheritdoc />
		public IDistributedCache BuildCache()
		{
			// According to https://docs.microsoft.com/en-us/aspnet/core/performance/caching/distributed#the-idistributedcache-interface
			// (see its paragraph end note) there is no need for a singleton lifetime.
			return new RedisCache(_options);
		}
	}
}
