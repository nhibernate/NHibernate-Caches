using System;
using System.Runtime.Serialization;

namespace NHibernate.Caches.StackExchangeRedis.Messages
{
	/// <summary>
	/// Data for <see cref="OperationType.LockMany"/> operation.
	/// </summary>
	[Serializable]
	[DataContract]
	public class LockManyData
	{
		/// <summary>
		/// The keys to lock.
		/// </summary>
		[DataMember]
		public string[] LockKeys { get; set; }

		/// <summary>
		/// The lock value.
		/// </summary>
		[DataMember]
		public string LockValue { get; set; }
	}
}
