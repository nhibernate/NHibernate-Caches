using System;
using System.Runtime.Serialization;

namespace NHibernate.Caches.StackExchangeRedis.Messages
{
	/// <summary>
	/// A message used to synchronize cache data.
	/// </summary>
	[Serializable]
	[DataContract]
	public class CacheSynchronizationMessage
	{
		/// <summary>
		/// The id of the client that is sending the message.
		/// </summary>
		[DataMember]
		public int ClientId { get; set; }

		/// <summary>
		/// The operation type to perform.
		/// </summary>
		[DataMember]
		public OperationType OperationType { get; set; }

		/// <summary>
		/// The data for the operation.
		/// </summary>
		[DataMember]
		public object Data { get; set; }

		/// <summary>
		/// The timestamp when the operation was performed.
		/// </summary>
		[DataMember]
		public long Timestamp { get; set; }
	}
}
