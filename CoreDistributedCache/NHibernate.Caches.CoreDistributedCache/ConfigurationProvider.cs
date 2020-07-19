using System;
using NHibernate.Caches.Common;

namespace NHibernate.Caches.CoreDistributedCache
{
	/// <summary>
	/// Base class for the cache configuration settings.
	/// </summary>
	[CLSCompliant(false)]
	public abstract class ConfigurationProvider : ConfigurationProviderBase<CacheConfig, CoreDistributedCacheSectionHandler>
	{
	}
}
