using Microsoft.Extensions.Caching.Distributed;

namespace NHibernate.Caches.CoreDistributedCache
{
	/// <summary>
	/// Interface for factories building <see cref="IDistributedCache"/> instances.
	/// </summary>
	public interface IDistributedCacheFactory
	{
		/// <summary>
		/// Build a <see cref="IDistributedCache"/> instance.
		/// </summary>
		/// <returns>A <see cref="IDistributedCache"/> instance.</returns>
		IDistributedCache BuildCache();

		/// <summary>
		/// If the underlying <see cref="IDistributedCache"/> implementation has a limit on key size,
		/// its maximal size. <see langword="null" /> otherwise.
		/// </summary>
		int? MaxKeySize { get; }
	}
}
