using System;

namespace NHibernate.Caches.StackExchangeRedis
{
	/// <summary>
	/// A memory cache for storing values for a specific cache region.
	/// The implementation must use the region configuration for expiration
	/// and sliding expiration for methods that do not have explicit parameters for them.
	/// All methods must be thread safe and values must not be serialized.
	/// </summary>
	public abstract class RegionMemoryCacheBase
	{
		/// <summary>
		/// Gets the value of a key.
		/// </summary>
		/// <param name="key">The key to retrieve.</param>
		/// <returns>The value of a key or <see langword="null" /> if the key does not exist.</returns>
		public abstract object Get(string key);

		/// <summary>
		/// Puts the value in the cache.
		/// </summary>
		/// <param name="key">The key where the value will be store.</param>
		/// <param name="value">The value to store.</param>
		public abstract void Put(string key, object value);

		/// <summary>
		/// Puts the value in the cache.
		/// </summary>
		/// <param name="key">The key where the value will be store.</param>
		/// <param name="dependencyKey">The key which <paramref name="key"/> depends on.</param>
		/// <param name="value">The value to store.</param>
		public abstract void Put(string key, string dependencyKey, object value);

		/// <summary>
		/// Puts the value in the cache by using an explicit expiration. When <see cref="TimeSpan.Zero"/>
		/// is used for <paramref name="expiration"/> the key will never expire.
		/// </summary>
		/// <param name="key">The key where the value will be store.</param>
		/// <param name="value">The value to store.</param>
		/// <param name="expiration">The expiration for the key.</param>
		public abstract void Put(string key, object value, TimeSpan expiration);

		/// <summary>
		/// Puts the value in the cache by using an explicit expiration. When <see cref="TimeSpan.Zero"/>
		/// is used for <paramref name="expiration"/> the key will never expire.
		/// </summary>
		/// <param name="key">The key where the value will be store.</param>
		/// <param name="dependencyKey">The key which <paramref name="key"/> depends on.</param>
		/// <param name="value">The value to store.</param>
		/// <param name="expiration">The expiration for the key.</param>
		public abstract void Put(string key, string dependencyKey, object value, TimeSpan expiration);

		/// <summary>
		/// Removes the key and its dependent keys from the cache.
		/// </summary>
		/// <param name="key">The key to remove.</param>
		/// <returns>Whether the key was removed.</returns>
		public abstract bool Remove(string key);
	}
}
