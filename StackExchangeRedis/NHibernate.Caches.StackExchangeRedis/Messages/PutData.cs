using System;
using System.Runtime.Serialization;

namespace NHibernate.Caches.StackExchangeRedis.Messages
{
	/// <summary>
	/// Data for <see cref="OperationType.Put"/> operation.
	/// </summary>
	[Serializable]
	[DataContract]
	public class PutData
	{
		/// <summary>
		/// The key of the value.
		/// </summary>
		[DataMember]
		public string Key { get; set; }

		/// <summary>
		/// The value to store.
		/// </summary>
		[DataMember]
		public object Value { get; set; }
	}
}
