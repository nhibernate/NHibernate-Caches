﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Data.Caching;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Cache;
using CacheException=System.Data.Caching.CacheException;
using CacheFactory=System.Data.Caching.CacheFactory;

namespace NHibernate.Caches.Velocity
{
	public partial class VelocityClient : ICache
	{

		#region ICache Members

		public Task<object> GetAsync(object key, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			try
			{
				return Task.FromResult<object>(Get(key));
			}
			catch (Exception ex)
			{
				return Task.FromException<object>(ex);
			}
		}

		public Task PutAsync(object key, object value, CancellationToken cancellationToken)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key", "null key not allowed");
			}
			if (value == null)
			{
				throw new ArgumentNullException("value", "null value not allowed");
			}
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			try
			{
				Put(key, value);
				return Task.CompletedTask;
			}
			catch (Exception ex)
			{
				return Task.FromException<object>(ex);
			}
		}

		public Task RemoveAsync(object key, CancellationToken cancellationToken)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			return InternalRemoveAsync();
			async Task InternalRemoveAsync()
			{
				if (log.IsDebugEnabled)
				{
					log.DebugFormat("removing item {0}", key);
				}

				if (await (GetAsync(key.ToString(), cancellationToken)).ConfigureAwait(false) != null)
				{
					cache.Remove(region, key.ToString());
				}
			}
		}

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

		public async Task LockAsync(object key, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var lockHandle = new LockHandle();
			if (await (GetAsync(key.ToString(), cancellationToken)).ConfigureAwait(false) != null)
			{
				try
				{
					cache.GetAndLock(region, key.ToString(), TimeSpan.FromMilliseconds(Timeout), out lockHandle);
				}
				catch (CacheException) {}
			}
		}

		public async Task UnlockAsync(object key, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var lockHandle = new LockHandle();
			if (await (GetAsync(key.ToString(), cancellationToken)).ConfigureAwait(false) != null)
			{
				try
				{
					cache.Unlock(region, key.ToString(), lockHandle);
				}
				catch (CacheException) {}
			}
		}

		#endregion
	}
}
