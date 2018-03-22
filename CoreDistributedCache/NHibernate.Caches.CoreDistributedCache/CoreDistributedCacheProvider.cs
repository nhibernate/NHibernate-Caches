#region License

//
//  CoreDistributedCache - A cache provider for NHibernate using Microsoft.Extensions.Caching.Distributed.IDistributedCache.
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

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using NHibernate.Cache;
using NHibernate.Util;

namespace NHibernate.Caches.CoreDistributedCache
{
	/// <summary>
	/// Cache provider using the System.Runtime.Caching classes
	/// </summary>
	public class CoreDistributedCacheProvider : ICacheProvider
	{
		private static readonly Dictionary<string, IDictionary<string, string>> ConfiguredCachesProperties;
		private static readonly INHibernateLogger Log;
		private static readonly System.Type[] CacheFactoryCtorWithPropertiesSignature = { typeof(IDictionary<string, string>) };

		/// <summary>
		/// The <see cref="IDistributedCacheFactory"/> factory to use for getting <see cref="IDistributedCache"/>
		/// instances. By default, its value is initialized from <c>factory-class</c> attribute of the
		/// <c>coredistributedcache</c> configuration section.
		/// </summary>
		/// <remarks>
		/// Changes to this property affect only caches built after the change.
		/// </remarks>
		public static IDistributedCacheFactory CacheFactory { get; set; }

		static CoreDistributedCacheProvider()
		{
			Log = NHibernateLogger.For(typeof(CoreDistributedCacheProvider));
			ConfiguredCachesProperties = new Dictionary<string, IDictionary<string, string>>();

			if (!(ConfigurationManager.GetSection("coredistributedcache") is CacheConfig config))
				return;

			if (!string.IsNullOrEmpty(config.FactoryClass))
			{
				try
				{
					var factoryClass = ReflectHelper.ClassForName(config.FactoryClass);
					var ctorWithProperties = factoryClass.GetConstructor(CacheFactoryCtorWithPropertiesSignature);

					CacheFactory = (IDistributedCacheFactory) (ctorWithProperties != null ?
						ctorWithProperties.Invoke(new object[] { config.Properties }):
						Cfg.Environment.BytecodeProvider.ObjectsFactory.CreateInstance(factoryClass));
				}
				catch (Exception e)
				{
					throw new HibernateException(
						$"Could not create the {nameof(IDistributedCacheFactory)} factory from '{config.FactoryClass}'. " +
						$"(It must implement {nameof(IDistributedCacheFactory)} and have a constructor accepting a " +
						$"{nameof(IDictionary<string, string>)} or have a parameterless constructor.)",
						e);
				}
			}

			foreach (var cache in config.Regions)
			{
				ConfiguredCachesProperties.Add(cache.Region, cache.Properties);
			}
		}

		#region ICacheProvider Members

		/// <inheritdoc />
		public ICache BuildCache(string regionName, IDictionary<string, string> properties)
		{
			if (CacheFactory == null)
				throw new InvalidOperationException(
					$"{nameof(CacheFactory)} is null, cannot build a distributed cache without a cache factory. " +
					$"Please provide coredistributedcache configuration section with a factory-class attribute or set" +
					$"{nameof(CacheFactory)} before building a session factory.");

			if (regionName == null)
			{
				regionName = string.Empty;
			}

			if (ConfiguredCachesProperties.TryGetValue(regionName, out var configuredProperties) && configuredProperties.Count > 0)
			{
				if (properties != null)
				{
					// Duplicate it for not altering the global configuration
					properties = new Dictionary<string, string>(properties);
					foreach (var prop in configuredProperties)
					{
						properties[prop.Key] = prop.Value;
					}
				}
				else
				{
					properties = configuredProperties;
				}
			}

			// create cache
			if (properties == null)
			{
				properties = new Dictionary<string, string>(1);
			}

			if (Log.IsDebugEnabled())
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

				Log.Debug("building cache with region: {0}, properties: {1}, factory: {2}" , regionName, sb.ToString(), CacheFactory.GetType().FullName);
			}
			return new CoreDistributedCache(CacheFactory.BuildCache(), CacheFactory.MaxKeySize, regionName, properties);
		}

		/// <inheritdoc />
		public long NextTimestamp()
		{
			return Timestamper.Next();
		}

		/// <inheritdoc />
		public void Start(IDictionary<string, string> properties)
		{
		}

		/// <inheritdoc />
		public void Stop()
		{
		}

		#endregion
	}
}
