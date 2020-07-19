using System;
using NHibernate.Caches.Common;

namespace NHibernate.Caches.StackExchangeRedis
{
	/// <summary>
	/// Base class for the cache configuration settings.
	/// </summary>
	public abstract class ConfigurationProvider : ConfigurationProviderBase<CacheConfig, RedisSectionHandler>
	{
	}
}
