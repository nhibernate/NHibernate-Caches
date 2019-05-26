using System;
using System.Runtime.Serialization;

namespace NHibernate.Caches.StackExchangeRedis.Messages
{
	/// <summary>
	/// A message that signalize whether the lock was obtained.
	/// </summary>
	[Serializable]
	[DataContract]
	public class LockResultMessage
	{
		/// <summary>
		/// The key to lock.
		/// </summary>
		[DataMember]
		public string LockKey { get; set; }

		/// <summary>
		/// Whether the lock was obtained.
		/// </summary>
		[DataMember]
		public bool Success { get; set; }
	}
}
