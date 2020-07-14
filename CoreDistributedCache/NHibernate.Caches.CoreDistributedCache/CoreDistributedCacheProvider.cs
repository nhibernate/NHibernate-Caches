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
using NHibernate.Caches.Common;
using NHibernate.Util;

namespace NHibernate.Caches.CoreDistributedCache
{
	/// <summary>
	/// Cache provider using <see cref="IDistributedCache"/> implementations.
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
		[CLSCompliant(false)]
		public static IDistributedCacheFactory CacheFactory { get; set; }

		/// <summary>
		/// The default serializer for all regions.
		/// </summary>
		[CLSCompliant(false)]
		public static CacheSerializerBase DefaultSerializer { get; set; } = new BinaryCacheSerializer();

		/// <summary>Should the keys be appended with their hashcode?</summary>
		/// <value>By default <see langword="true" /> for the .Net Framework build,
		/// <see langword="false" /> otherwise. This setting will be <c>false</c> by default
		/// for all runtime in the next major release (6.0).</value>
		/// <remarks>
		/// <para>This option is a workaround for distinguishing composite-id missing an
		/// <see cref="object.ToString"/> override. It may causes trouble if the cache is shared
		/// between processes running another runtime than .Net Framework, or with future versions
		/// of .Net Framework: the hashcode is not guaranteed to be stable.
		/// </para>
		/// <para>
		/// Changes to this property affect only caches built after the change, and whose configuration node
		/// does not define their own <c>append-hashcode</c> attribute.
		/// </para>
		/// <para>
		/// The value of this property can be set with the attribute <c>append-hashcode</c> of the
		/// <c>coredistributedcache</c> configuration section.
		/// </para>
		/// </remarks>
		public static bool AppendHashcodeToKey { get; set; }

		/// <summary>
		/// Set a region configuration.
		/// </summary>
		/// <param name="configuration">The region configuration.</param>
		public static void SetRegionConfiguration(RegionConfig configuration)
		{
			ConfiguredCachesProperties[configuration.Region] = configuration.Properties;
		}

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
						Cfg.Environment.ObjectsFactory.CreateInstance(factoryClass));
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

			AppendHashcodeToKey = config.AppendHashcodeToKey;

			DefaultSerializer = GetSerializer(config.Properties) ?? new BinaryCacheSerializer();
		}

		internal static CacheSerializerBase GetSerializer(IDictionary<string, string> props)
		{
			if (props == null || !props.TryGetValue("cache.serializer", out var serializer) || string.IsNullOrEmpty(serializer))
				return null;

			var serializerClass = ReflectHelper.ClassForName(serializer);
			return (CacheSerializerBase) Cfg.Environment.ObjectsFactory.CreateInstance(serializerClass);
		}

		#region ICacheProvider Members

		/// <inheritdoc />
#pragma warning disable 618
		public ICache BuildCache(string regionName, IDictionary<string, string> properties)
#pragma warning restore 618
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

			properties = properties != null
				// Duplicate it for not altering the global configuration
				? new Dictionary<string, string>(properties)
				: new Dictionary<string, string>();

			properties["cache.append_hashcode_to_key"] = AppendHashcodeToKey.ToString();

			if (ConfiguredCachesProperties.TryGetValue(regionName, out var configuredProperties) && configuredProperties.Count > 0)
			{
				foreach (var prop in configuredProperties)
				{
					properties[prop.Key] = prop.Value;
				}
			}

			// create cache

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
			return
				new CoreDistributedCache(CacheFactory.BuildCache(), CacheFactory.Constraints, regionName, properties);
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
