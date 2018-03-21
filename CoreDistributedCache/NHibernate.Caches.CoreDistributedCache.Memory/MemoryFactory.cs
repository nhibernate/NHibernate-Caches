using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace NHibernate.Caches.CoreDistributedCache.Memory
{
	/// <summary>
	/// A memory "distributed" cache factory. Use for testing purpose. Otherwise consider using <c>CoreMemoryCache</c>
	/// instead.
	/// </summary>
	public class MemoryFactory : IDistributedCacheFactory
	{
		private static readonly INHibernateLogger Log = NHibernateLogger.For(typeof(MemoryFactory));
		private const string _expirationScanFrequency = "expiration-scan-frequency";
		private const string _sizeLimit = "size-limit";

		private readonly IDistributedCache _cache;

		/// <summary>
		/// Default constructor.
		/// </summary>
		public MemoryFactory() : this(null)
		{
		}

		/// <summary>
		/// Constructor with explicit configuration properties.
		/// </summary>
		/// <param name="expirationScanFrequency">See <see cref="MemoryDistributedCacheOptions.ExpirationScanFrequency" />.</param>
		/// <param name="sizeLimit">See <see cref="MemoryDistributedCacheOptions.SizeLimit" />.</param>
		public MemoryFactory(TimeSpan? expirationScanFrequency, long? sizeLimit)
		{
			var options = new Options();
			if (expirationScanFrequency.HasValue)
				options.ExpirationScanFrequency = expirationScanFrequency.Value;
			if (sizeLimit.HasValue)
				options.SizeLimit = sizeLimit.Value;

			_cache = new MemoryDistributedCache(options);
		}

		/// <summary>
		/// Constructor with configuration properties. It supports <c>expiration-scan-frequency</c> and
		/// <c>size-limit</c> properties. See <see cref="MemoryDistributedCacheOptions.ExpirationScanFrequency" /> and
		/// <see cref="MemoryDistributedCacheOptions.SizeLimit" />.
		/// </summary>
		/// <param name="properties">The configurations properties.</param>
		/// <remarks>
		/// <para>
		/// If <c>expiration-scan-frequency</c> is provided as an integer, this integer will be used as a number
		/// of minutes. Otherwise the setting will be parsed as a <see cref="TimeSpan" />.
		/// </para>
		/// <para><c>size-limit</c> has to be an integer, expressing the size limit in bytes.</para>
		/// </remarks>
		public MemoryFactory(IDictionary<string, string> properties)
		{
			var options = new Options();

			if (properties != null)
			{
				if (properties.TryGetValue(_expirationScanFrequency, out var esf))
				{
					if (esf != null)
					{
						if (int.TryParse(esf, out var minutes))
							options.ExpirationScanFrequency = TimeSpan.FromMinutes(minutes);
						else if (TimeSpan.TryParse(esf, out var expirationScanFrequency))
							options.ExpirationScanFrequency = expirationScanFrequency;
						else
							Log.Warn(
								"Invalid value '{0}' for {1} setting: it is neither an int nor a TimeSpan. Ignoring.",
								esf, _expirationScanFrequency);
					}
					else
					{
						Log.Warn("Invalid property {0}: it lacks a value. Ignoring.", _expirationScanFrequency);
					}
				}

				if (properties.TryGetValue(_sizeLimit, out var sl))
				{
					if (sl != null)
					{
						if (long.TryParse(sl, out var bytes))
							options.SizeLimit = bytes;
						else
							Log.Warn(
								"Invalid value '{0}' for {1} setting: it is not an integer. Ignoring.",
								sl, _sizeLimit);
					}
					else
					{
						Log.Warn("Invalid property {0}: it lacks a value. Ignoring.", _sizeLimit);
					}
				}
			}

			_cache = new MemoryDistributedCache(options);
		}

		/// <inheritdoc />
		public IDistributedCache BuildCache()
		{
			// Always yields the same instance: its underlying implementation is a MemoryCache which regularly spawn
			// a background task for expiring items. This avoids creating many instances, thus avoiding potentially
			// spawning many such background tasks at once.
			// This also allows to share the cache between all session factories of a process, thus emulating a
			// distributed aspect.
			return _cache;
		}

		private class Options : MemoryDistributedCacheOptions, IOptions<MemoryDistributedCacheOptions>
		{
			MemoryDistributedCacheOptions IOptions<MemoryDistributedCacheOptions>.Value => this;
		}
	}
}
