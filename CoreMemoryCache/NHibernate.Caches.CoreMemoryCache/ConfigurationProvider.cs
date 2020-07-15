using System;
using NHibernate.Caches.Common;

namespace NHibernate.Caches.CoreMemoryCache
{
	/// <summary>
	/// Base class for the cache configuration settings.
	/// </summary>
	[CLSCompliant(false)]
	public abstract class ConfigurationProvider : ConfigurationProviderBase<CacheConfig, CoreMemoryCacheSectionHandler>
	{
	}
}
