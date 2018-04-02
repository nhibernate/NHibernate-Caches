using System;
using Microsoft.Extensions.Caching.Distributed;

namespace NHibernate.Caches.CoreDistributedCache
{
	/// <summary>
	/// Interface for factories building <see cref="IDistributedCache"/> instances.
	/// </summary>
	[CLSCompliant(false)]
	public interface IDistributedCacheFactory
	{
		/// <summary>
		/// Build a <see cref="IDistributedCache"/> instance.
		/// </summary>
		/// <returns>A <see cref="IDistributedCache"/> instance.</returns>
		IDistributedCache BuildCache();

		/// <summary>
		/// If the underlying <see cref="IDistributedCache"/> implementation has specific constraints,
		/// its constraints, <see langword="null" /> otherwise.
		/// </summary>
		CacheConstraints Constraints { get; }
	}
}
