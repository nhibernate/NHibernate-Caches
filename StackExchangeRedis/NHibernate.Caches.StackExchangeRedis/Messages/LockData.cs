using System;
using System.Runtime.Serialization;

namespace NHibernate.Caches.StackExchangeRedis.Messages
{
	/// <summary>
	/// Data for <see cref="OperationType.Lock"/> operation.
	/// </summary>
	[Serializable]
	[DataContract]
	public class LockData
	{
		/// <summary>
		/// The key to lock.
		/// </summary>
		[DataMember]
		public string LockKey { get; set; }

		/// <summary>
		/// The lock value.
		/// </summary>
		[DataMember]
		public string LockValue { get; set; }
	}
}
