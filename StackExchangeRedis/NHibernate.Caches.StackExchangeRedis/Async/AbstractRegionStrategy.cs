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
using System.Linq;
using NHibernate.Cache;
using NHibernate.Caches.Common;
using StackExchange.Redis;

namespace NHibernate.Caches.StackExchangeRedis
{
	using System.Threading.Tasks;
	using System.Threading;
	public abstract partial class AbstractRegionStrategy
	{

		/// <summary>
		/// Gets the object that is stored in Redis by its key.
		/// </summary>
		/// <param name="key">The key of the object to retrieve.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		/// <returns>The object behind the key or <see langword="null" /> if the key was not found.</returns>
		public virtual Task<object> GetAsync(object key, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			try
			{
				if (key == null)
				{
					return Task.FromResult<object>(null);
				}

				var cacheKey = GetCacheKey(key);
				Log.Debug("Fetching object with key: '{0}'.", cacheKey);
				return ExecuteGetAsync(cacheKey, cancellationToken);
			}
			catch (Exception ex)
			{
				return Task.FromException<object>(ex);
			}
		}

		/// <summary>
		/// Executes the command to retrieve the key from Redis.
		/// </summary>
		/// <param name="cacheKey">The key of the object to retrieve.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		/// <returns>The object behind the key or <see langword="null" /> if the key was not found.</returns>
		protected virtual async Task<object> ExecuteGetAsync(string cacheKey, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			RedisValue result;
			if (string.IsNullOrEmpty(GetScript))
			{
				cancellationToken.ThrowIfCancellationRequested();
				result = await (Database.StringGetAsync(cacheKey)).ConfigureAwait(false);
			}
			else
			{
				cancellationToken.ThrowIfCancellationRequested();
				result = ((RedisValue[]) await (Database.ScriptEvaluateAsync(
					GetScript,
					AppendAdditionalKeys(new RedisKey[] {cacheKey}),
					AppendAdditionalValues(new RedisValue[]
					{
						UseSlidingExpiration && ExpirationEnabled,
						(long) Expiration.TotalMilliseconds
					}))).ConfigureAwait(false))[0];
			}

			return result.IsNullOrEmpty ? null : Serializer.Deserialize(result);
		}

		/// <summary>
		/// Gets the objects that are stored in Redis by their key.
		/// </summary>
		/// <param name="keys">The keys of the objects to retrieve.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		/// <returns>An array of objects behind the keys or <see langword="null" /> if the key was not found.</returns>
		public virtual Task<object[]> GetManyAsync(object[] keys, CancellationToken cancellationToken)
		{
			if (keys == null)
			{
				throw new ArgumentNullException(nameof(keys));
			}
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object[]>(cancellationToken);
			}
			try
			{

				var cacheKeys = new RedisKey[keys.Length];
				Log.Debug("Fetching {0} objects...", keys.Length);
				for (var i = 0; i < keys.Length; i++)
				{
					cacheKeys[i] = GetCacheKey(keys[i]);
					Log.Debug("Fetching object with key: '{0}'.", cacheKeys[i]);
				}

				return ExecuteGetManyAsync(cacheKeys, cancellationToken);
			}
			catch (Exception ex)
			{
				return Task.FromException<object[]>(ex);
			}
		}

		/// <summary>
		/// Executes the command to retrieve the keys from Redis.
		/// </summary>
		/// <param name="cacheKeys">The keys of the objects to retrieve.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		/// <returns>An array of objects behind the keys or <see langword="null" /> if the key was not found.</returns>
		protected virtual async Task<object[]> ExecuteGetManyAsync(RedisKey[] cacheKeys, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			RedisValue[] results;
			if (string.IsNullOrEmpty(GetManyScript))
			{
				cancellationToken.ThrowIfCancellationRequested();
				results = await (Database.StringGetAsync(cacheKeys)).ConfigureAwait(false);
			}
			else
			{
				cancellationToken.ThrowIfCancellationRequested();
				results = (RedisValue[]) await (Database.ScriptEvaluateAsync(
					GetManyScript,
					AppendAdditionalKeys(cacheKeys),
					AppendAdditionalValues(new RedisValue[]
					{
						UseSlidingExpiration && ExpirationEnabled,
						(long) Expiration.TotalMilliseconds
					}))).ConfigureAwait(false);
			}

			var values = new object[cacheKeys.Length];
			for (var i = 0; i < results.Length; i++)
			{
				var result = results[i];
				if (!result.IsNullOrEmpty)
				{
					values[i] = Serializer.Deserialize(result);
				}
			}

			return values;
		}

		/// <summary>
		/// Stores the object into Redis by the given key.
		/// </summary>
		/// <param name="key">The key to store the object.</param>
		/// <param name="value">The object to store.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		public virtual Task PutAsync(object key, object value, CancellationToken cancellationToken)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			try
			{

				var cacheKey = GetCacheKey(key);
				Log.Debug("Putting object with key: '{0}'.", cacheKey);
				return ExecutePutAsync(cacheKey, value, cancellationToken);
			}
			catch (Exception ex)
			{
				return Task.FromException<object>(ex);
			}
		}

		/// <summary>
		/// Executes the put command.
		/// </summary>
		/// <param name="cacheKey">The key parameter for the command.</param>
		/// <param name="value">The value parameter for the command.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		protected virtual Task ExecutePutAsync(string cacheKey, object value, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			try
			{
				if (string.IsNullOrEmpty(PutScript))
				{
					cancellationToken.ThrowIfCancellationRequested();
					return Database.StringSetAsync(cacheKey, Serializer.Serialize(value), ExpirationEnabled ? Expiration : (TimeSpan?) null);
				}
				else
				{
					cancellationToken.ThrowIfCancellationRequested();
					return Database.ScriptEvaluateAsync(
						PutScript,
						AppendAdditionalKeys(new RedisKey[] {cacheKey}),
						AppendAdditionalValues(new RedisValue[]
						{
							Serializer.Serialize(value),
							ExpirationEnabled,
							(long) Expiration.TotalMilliseconds
						}));
				}
			}
			catch (Exception ex)
			{
				return Task.FromException<object>(ex);
			}
		}

		/// <summary>
		/// Stores the objects into Redis by the given keys.
		/// </summary>
		/// <param name="keys">The keys to store the objects.</param>
		/// <param name="values">The objects to store.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		public virtual Task PutManyAsync(object[] keys, object[] values, CancellationToken cancellationToken)
		{
			if (keys == null)
			{
				throw new ArgumentNullException(nameof(keys));
			}

			if (values == null)
			{
				throw new ArgumentNullException(nameof(values));
			}

			if (keys.Length != values.Length)
			{
				throw new ArgumentException($"Length of {nameof(keys)} array does not match with {nameof(values)} array.");
			}
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			try
			{

				Log.Debug("Putting {0} objects...", keys.Length);
				return ExecutePutManyAsync(keys, values, cancellationToken);
			}
			catch (Exception ex)
			{
				return Task.FromException<object>(ex);
			}
		}

		/// <summary>
		/// Stores the objects into Redis by the given keys.
		/// </summary>
		/// <param name="keys">The keys to store the objects.</param>
		/// <param name="values">The objects to store.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		protected virtual async Task ExecutePutManyAsync(object[] keys, object[] values, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (string.IsNullOrEmpty(PutManyScript))
			{
				if (ExpirationEnabled)
				{
					throw new NotSupportedException($"{nameof(PutManyAsync)} operation with expiration is not supported.");
				}

				var pairs = new KeyValuePair<RedisKey, RedisValue>[keys.Length];
				for (var i = 0; i < keys.Length; i++)
				{
					pairs[i] = new KeyValuePair<RedisKey, RedisValue>(GetCacheKey(keys[i]), Serializer.Serialize(values[i]));
					Log.Debug("Putting object with key: '{0}'.", pairs[i].Key);
				}
				cancellationToken.ThrowIfCancellationRequested();

				await (Database.StringSetAsync(pairs)).ConfigureAwait(false);
				return;
			}


			var cacheKeys = new RedisKey[keys.Length];
			var cacheValues = new RedisValue[keys.Length + 2];
			for (var i = 0; i < keys.Length; i++)
			{
				cacheKeys[i] = GetCacheKey(keys[i]);
				cacheValues[i] = Serializer.Serialize(values[i]);
				Log.Debug("Putting object with key: '{0}'.", cacheKeys[i]);
			}

			cacheValues[cacheKeys.Length] = ExpirationEnabled;
			cacheValues[cacheKeys.Length + 1] = (long) Expiration.TotalMilliseconds;
			cancellationToken.ThrowIfCancellationRequested();
			await (Database.ScriptEvaluateAsync(PutManyScript, AppendAdditionalKeys(cacheKeys), AppendAdditionalValues(cacheValues))).ConfigureAwait(false);
		}

		/// <summary>
		/// Removes the key from Redis.
		/// </summary>
		/// <param name="key">The key to remove.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		public virtual Task<bool> RemoveAsync(object key, CancellationToken cancellationToken)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<bool>(cancellationToken);
			}
			try
			{

				var cacheKey = GetCacheKey(key);
				Log.Debug("Removing object with key: '{0}'.", cacheKey);
				return ExecuteRemoveAsync(cacheKey, cancellationToken);
			}
			catch (Exception ex)
			{
				return Task.FromException<bool>(ex);
			}
		}

		/// <summary>
		/// Executes the remove command.
		/// </summary>
		/// <param name="cacheKey">The key to remove</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		protected virtual async Task<bool> ExecuteRemoveAsync(string cacheKey, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return string.IsNullOrEmpty(RemoveScript)
				? await (Database.KeyDeleteAsync(cacheKey)).ConfigureAwait(false)
				: (bool) ((RedisValue[]) await (Database.ScriptEvaluateAsync(
					RemoveScript,
					AppendAdditionalKeys(new RedisKey[] {cacheKey}),
					GetAdditionalValues())).ConfigureAwait(false))[0];
		}

		/// <summary>
		/// Locks the key.
		/// </summary>
		/// <param name="key">The key to lock.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		/// <returns>The value used to lock the key.</returns>
		public virtual Task<string> LockAsync(object key, CancellationToken cancellationToken)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<string>(cancellationToken);
			}
			try
			{

				var cacheKey = GetCacheKey(key);
				Log.Debug("Locking object with key: '{0}'.", cacheKey);
				return _keyLocker.LockAsync(cacheKey, LockScript, GetAdditionalKeys(), GetAdditionalValues(), cancellationToken);
			}
			catch (Exception ex)
			{
				return Task.FromException<string>(ex);
			}
		}

		/// <summary>
		/// Locks many keys at once.
		/// </summary>
		/// <param name="keys">The keys to lock.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		/// <returns>The value used to lock the keys.</returns>
		public virtual Task<string> LockManyAsync(object[] keys, CancellationToken cancellationToken)
		{
			if (keys == null)
			{
				throw new ArgumentNullException(nameof(keys));
			}

			if (string.IsNullOrEmpty(LockManyScript))
			{
				throw new NotSupportedException($"{nameof(LockManyAsync)} operation is not supported.");
			}
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<string>(cancellationToken);
			}
			try
			{

				Log.Debug("Locking {0} objects...", keys.Length);
				var cacheKeys = new string[keys.Length];
				for (var i = 0; i < keys.Length; i++)
				{
					cacheKeys[i] = GetCacheKey(keys[i]);
					Log.Debug("Locking object with key: '{0}'.", cacheKeys[i]);
				}

				return _keyLocker.LockManyAsync(cacheKeys, LockManyScript, GetAdditionalKeys(), GetAdditionalValues(), cancellationToken);
			}
			catch (Exception ex)
			{
				return Task.FromException<string>(ex);
			}
		}

		/// <summary>
		/// Unlocks the key.
		/// </summary>
		/// <param name="key">The key to unlock.</param>
		/// <param name="lockValue">The value used to lock the key.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		/// <returns>Whether the key was unlocked</returns>
		public virtual Task<bool> UnlockAsync(object key, string lockValue, CancellationToken cancellationToken)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<bool>(cancellationToken);
			}
			return InternalUnlockAsync();
			async Task<bool> InternalUnlockAsync()
			{

				var cacheKey = GetCacheKey(key);
				Log.Debug("Unlocking object with key: '{0}'.", cacheKey);
				var unlocked = await (_keyLocker.UnlockAsync(cacheKey, lockValue, UnlockScript, GetAdditionalKeys(), GetAdditionalValues(), cancellationToken)).ConfigureAwait(false);
				Log.Debug("Unlock key '{0}' result: {1}", cacheKey, unlocked);
				return unlocked;
			}
		}

		/// <summary>
		/// Unlocks many keys at once.
		/// </summary>
		/// <param name="keys">The keys to unlock.</param>
		/// <param name="lockValue">The value used to lock the keys.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		/// <returns>The number of unlocked keys.</returns>
		public virtual Task<int> UnlockManyAsync(object[] keys, string lockValue, CancellationToken cancellationToken)
		{
			if (keys == null)
			{
				throw new ArgumentNullException(nameof(keys));
			}

			if (string.IsNullOrEmpty(UnlockManyScript))
			{
				throw new NotSupportedException($"{nameof(UnlockManyAsync)} operation is not supported.");
			}
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<int>(cancellationToken);
			}
			return InternalUnlockManyAsync();
			async Task<int> InternalUnlockManyAsync()
			{

				Log.Debug("Unlocking {0} objects...", keys.Length);
				var cacheKeys = new string[keys.Length];
				for (var i = 0; i < keys.Length; i++)
				{
					cacheKeys[i] = GetCacheKey(keys[i]);
					Log.Debug("Unlocking object with key: '{0}'.", cacheKeys[i]);
				}

				var unlockedKeys =
					await (_keyLocker.UnlockManyAsync(cacheKeys, lockValue, UnlockManyScript, GetAdditionalKeys(), GetAdditionalValues(), cancellationToken)).ConfigureAwait(false);
				if (Log.IsDebugEnabled())
				{
					Log.Debug("Number of unlocked objects with keys ({0}): {1}", string.Join(",", cacheKeys.Select(o => $"'{o}'")),
						unlockedKeys);
				}

				return unlockedKeys;
			}
		}

		/// <summary>
		/// Clears all the keys from the region.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		public abstract Task ClearAsync(CancellationToken cancellationToken);
	}
}
