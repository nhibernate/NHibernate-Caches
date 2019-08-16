using System;
using System.Runtime.Serialization;

namespace NHibernate.Caches.StackExchangeRedis.Messages
{
	/// <summary>
	/// Synchronization operation types used for synchronizing the cache data.
	/// </summary>
	[Serializable]
	[DataContract]
	public enum OperationType
	{
		/// <summary>
		/// Operation for putting a value into the cache.
		/// </summary>
		Put,
		/// <summary>
		/// Operation for removing a key from the cache.
		/// </summary>
		Remove,
		/// <summary>
		/// Operation for clearing the cache.
		/// </summary>
		Clear,
		/// <summary>
		/// Operation for locking a key.
		/// </summary>
		Lock,
		/// <summary>
		/// Operation for locking multiple keys.
		/// </summary>
		LockMany,
		/// <summary>
		/// Operation for unlocking a key.
		/// </summary>
		Unlock,
		/// <summary>
		/// Operation for unlocking multiple keys.
		/// </summary>
		UnlockMany
	}
}
