using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Distributed;

namespace NHibernate.Caches.CoreDistributedCache
{
	/// <summary>
	/// Cache configuration properties.
	/// </summary>
	public class CacheConfig
	{
		/// <summary>
		/// Build a cache configuration.
		/// </summary>
		/// <param name="factoryClass">The <see cref="IDistributedCacheFactory"/> factory class name to use for getting
		/// <see cref="IDistributedCache"/> instances.</param>
		/// <param name="properties">The cache configuration properties.</param>
		/// <param name="regions">The configured cache regions.</param>
		// Since 5.4
		[Obsolete("Use overload with appendHashcodeToKey additional parameter")]
		public CacheConfig(string factoryClass, IDictionary<string, string> properties, RegionConfig[] regions) :
			this(factoryClass, properties, regions, true)
		{
		}

		/// <summary>
		/// Build a cache configuration.
		/// </summary>
		/// <param name="factoryClass">The <see cref="IDistributedCacheFactory"/> factory class name to use for getting
		/// <see cref="IDistributedCache"/> instances.</param>
		/// <param name="properties">The cache configuration properties.</param>
		/// <param name="regions">The configured cache regions.</param>
		/// <param name="appendHashcodeToKey">Should the keys be appended with their hashcode?</param>
		public CacheConfig(
			string factoryClass, IDictionary<string, string> properties, RegionConfig[] regions, bool appendHashcodeToKey)
		{
			FactoryClass = factoryClass;
			Regions = regions;
			Properties = properties;
			AppendHashcodeToKey = appendHashcodeToKey;
		}

		/// <summary>The <see cref="IDistributedCacheFactory"/> factory class name to use for getting
		/// <see cref="IDistributedCache"/> instances.</summary>
		public string FactoryClass { get; }

		/// <summary>Should the keys be appended with their hashcode?</summary>
		/// <remarks>This option is a workaround for distinguishing composite-id missing an
		/// <see cref="object.ToString"/> override.</remarks>
		public bool AppendHashcodeToKey { get; }

		/// <summary>The configured cache regions.</summary>
		public RegionConfig[] Regions { get; }

		/// <summary>The cache configuration properties.</summary>
		public IDictionary<string, string> Properties { get; }
	}

	/// <summary>
	/// Region configuration properties.
	/// </summary>
	public class RegionConfig
	{
		/// <summary>
		/// Build a cache region configuration.
		/// </summary>
		/// <param name="region">The configured cache region.</param>
		/// <param name="expiration">The expiration for the region.</param>
		/// <param name="sliding">Whether the expiration should be sliding or not.</param>
		// Since 5.5
		[Obsolete("Use overload with appendHashcodeToKey additional parameter")]
		public RegionConfig(string region, string expiration, string sliding) : this(region, expiration, sliding, null)
		{
		}

		/// <summary>
		/// Build a cache region configuration.
		/// </summary>
		/// <param name="region">The configured cache region.</param>
		/// <param name="expiration">The expiration for the region.</param>
		/// <param name="sliding">Whether the expiration should be sliding or not.</param>
		/// <param name="appendHashcodeToKey">Should the keys be appended with their hashcode?</param>
		// Since 5.7
		[Obsolete("Use overload with appendHashcodeToKey additional parameter")]
		public RegionConfig(string region, string expiration, string sliding, string appendHashcodeToKey)
			: this(region, expiration, sliding, appendHashcodeToKey, null)
		{
			Region = region;
			Properties = new Dictionary<string, string>();
			if (!string.IsNullOrEmpty(expiration))
				Properties["expiration"] = expiration;
			if (!string.IsNullOrEmpty(sliding))
				Properties["cache.use_sliding_expiration"] = sliding;
			if (!string.IsNullOrEmpty(appendHashcodeToKey))
				Properties["cache.append_hashcode_to_key"] = appendHashcodeToKey;
		}

		/// <summary>
		/// Build a cache region configuration.
		/// </summary>
		/// <param name="region">The configured cache region.</param>
		/// <param name="expiration">The expiration for the region.</param>
		/// <param name="sliding">Whether the expiration should be sliding or not.</param>
		/// <param name="appendHashcodeToKey">Should the keys be appended with their hashcode?</param>
		/// <param name="serializer">The serializer class name for the region.</param>
		public RegionConfig(string region, string expiration, string sliding, string appendHashcodeToKey, string serializer)
		{
			Region = region;
			Properties = new Dictionary<string, string>();
			if (!string.IsNullOrEmpty(expiration))
				Properties["expiration"] = expiration;
			if (!string.IsNullOrEmpty(sliding))
				Properties["cache.use_sliding_expiration"] = sliding;
			if (!string.IsNullOrEmpty(appendHashcodeToKey))
				Properties["cache.append_hashcode_to_key"] = appendHashcodeToKey;
			if (!string.IsNullOrEmpty(serializer))
				Properties["cache.serializer"] = serializer;
		}

		/// <summary>The region name.</summary>
		public string Region { get; }

		/// <summary>The region configuration properties.</summary>
		public IDictionary<string, string> Properties { get; }
	}
}
