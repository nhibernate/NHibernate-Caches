﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;
using NHibernate.Cache;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Extensions.Caching.Distributed;
using NHibernate.Caches.Util;
using NHibernate.Util;

namespace NHibernate.Caches.CoreDistributedCache
{
	using System.Threading.Tasks;
	using System.Threading;
	public partial class CoreDistributedCache : ICache
	{

		/// <inheritdoc />
		public async Task<object> GetAsync(object key, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (key == null)
			{
				return null;
			}

			var cacheKey = GetCacheKey(key);
			Log.Debug("Fetching object '{0}' from the cache.", cacheKey);

			var cachedData = await (_cache.GetAsync(cacheKey, cancellationToken)).ConfigureAwait(false);
			if (cachedData == null)
				return null;

			var serializer = new BinaryFormatter();
			using (var stream = new MemoryStream(cachedData))
			{
				var entry = serializer.Deserialize(stream) as Tuple<object, object>;
				return Equals(entry?.Item1, key) ? entry.Item2 : null;
			}
		}

		/// <inheritdoc />
		public Task PutAsync(object key, object value, CancellationToken cancellationToken)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key), "null key not allowed");
			}

			if (value == null)
			{
				throw new ArgumentNullException(nameof(value), "null value not allowed");
			}
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			try
			{

				byte[] cachedData;
				var serializer = new BinaryFormatter();
				using (var stream = new MemoryStream())
				{
					var entry = new Tuple<object, object>(key, value);
					serializer.Serialize(stream, entry);
					cachedData = stream.ToArray();
				}

				var cacheKey = GetCacheKey(key);
				var options = new DistributedCacheEntryOptions();
				if (UseSlidingExpiration)
					options.SlidingExpiration = Expiration;
				else
					options.AbsoluteExpirationRelativeToNow = Expiration;

				Log.Debug("putting item with key: {0}", cacheKey);
				return _cache.SetAsync(cacheKey, cachedData, options, cancellationToken);
			}
			catch (Exception ex)
			{
				return Task.FromException<object>(ex);
			}
		}

		/// <inheritdoc />
		public Task RemoveAsync(object key, CancellationToken cancellationToken)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			try
			{

				var cacheKey = GetCacheKey(key);
				Log.Debug("removing item with key: {0}", cacheKey);
				return _cache.RemoveAsync(cacheKey, cancellationToken);
			}
			catch (Exception ex)
			{
				return Task.FromException<object>(ex);
			}
		}

		/// <inheritdoc />
		public Task ClearAsync(CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			try
			{
				Clear();
				return Task.CompletedTask;
			}
			catch (Exception ex)
			{
				return Task.FromException<object>(ex);
			}
		}

		/// <inheritdoc />
		public Task LockAsync(object key, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			try
			{
				Lock(key);
				return Task.CompletedTask;
			}
			catch (Exception ex)
			{
				return Task.FromException<object>(ex);
			}
		}

		/// <inheritdoc />
		public Task UnlockAsync(object key, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			try
			{
				Unlock(key);
				return Task.CompletedTask;
			}
			catch (Exception ex)
			{
				return Task.FromException<object>(ex);
			}
		}
	}
}
