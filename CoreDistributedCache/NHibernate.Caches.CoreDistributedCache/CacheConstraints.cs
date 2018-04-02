using System;
using Microsoft.Extensions.Caching.Distributed;

namespace NHibernate.Caches.CoreDistributedCache
{
	/// <summary>
	/// Constraints of the <see cref="IDistributedCache"/> implementation.
	/// </summary>
	public class CacheConstraints
	{
		/// <summary>
		/// If the underlying <see cref="IDistributedCache"/> implementation has a limit on key size,
		/// its maximal size, <see langword="null" /> otherwise.
		/// </summary>
		public int? MaxKeySize { get; set; }

		/// <summary>
		/// If the underlying <see cref="IDistributedCache"/> implementation has constraints on what a key may contain,
		/// a function sanitizing provided key, <see langword="null" /> otherwise.
		/// </summary>
		/// <remarks>
		/// <para>
		/// If the sanitization function causes two different keys to be equal after sanitization, additional cache
		/// misses may occur. (But yielded cached values will not be mixed: either the expected value or
		/// <see langword="null" /> will be yielded.)
		/// </para>
		/// <para>
		/// If <see cref="MaxKeySize"/> is also provided, the provided key will already respect it. The yielded value
		/// will not be checked again for its maximal length.
		/// </para>
		/// </remarks>
		public Func<string, string> KeySanitizer { get; set; }
	}
}
