#region License

//
//  CoreMemoryCache - A cache provider for NHibernate using Microsoft.Extensions.Caching.Memory.
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
using Microsoft.Extensions.Caching.Memory;
using NHibernate.Cache;

namespace NHibernate.Caches.CoreMemoryCache
{
	/// <summary>
	/// Cache provider using the System.Runtime.Caching classes
	/// </summary>
	public class CoreMemoryCacheProvider : ICacheProvider
	{
		private static readonly Dictionary<string, IDictionary<string, string>> ConfiguredCachesProperties;
		private static readonly INHibernateLogger Log;
		/// <summary>
		/// The frequency at which scans for cleaning expired cached item have to be done. By default, its value is
		/// initialized from <c>expiration-scan-frequency</c> attribute of the <c>corememorycache</c> configuration
		/// section. See <see cref="MemoryCacheOptions.ExpirationScanFrequency" />.
		/// </summary>
		/// <value>
		/// <see langword="null" /> if not configured, letting <see cref="MemoryCache" /> use its default setting.
		/// </value>
		/// <remarks>
		/// <para>
		/// For this property to be taken into account, it must be set before any cache is built.
		/// </para>
		/// <para>
		/// If <c>cache.expiration_scan_frequency</c> is provided as an integer, this integer will be used as a number
		/// of minutes. Otherwise the setting will be parsed as a <see cref="TimeSpan" />.
		/// </para>
		/// </remarks>
		public static TimeSpan? ExpirationScanFrequency { get; set; }

		static CoreMemoryCacheProvider()
		{
			Log = NHibernateLogger.For(typeof(CoreMemoryCacheProvider));
			ConfiguredCachesProperties = new Dictionary<string, IDictionary<string, string>>();

			if (!(ConfigurationManager.GetSection("corememorycache") is CacheConfig[] list))
				return;
			foreach (var cache in list)
			{
				if (cache.Global)
				{
					if (cache.ExpirationScanFrequency != null)
					{
						if (int.TryParse(cache.ExpirationScanFrequency, out var minutes))
							ExpirationScanFrequency = TimeSpan.FromMinutes(minutes);
						else if (TimeSpan.TryParse(cache.ExpirationScanFrequency, out var expirationScanFrequency))
							ExpirationScanFrequency = expirationScanFrequency;
						if (!ExpirationScanFrequency.HasValue)
							Log.Warn(
								"Invalid value '{0}' for cache.expiration_scan_frequency setting: it is neither an int nor a TimeSpan. Ignoring.",
								cache.ExpirationScanFrequency);
					}
					continue;
				}

				ConfiguredCachesProperties.Add(cache.Region, cache.Properties);
			}
		}

		#region ICacheProvider Members

		/// <inheritdoc />
		public ICache BuildCache(string regionName, IDictionary<string, string> properties)
		{
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

				Log.Debug("building cache with region: {0}, properties: {1}" , regionName, sb.ToString());
			}
			return new CoreMemoryCache(regionName, properties);
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
