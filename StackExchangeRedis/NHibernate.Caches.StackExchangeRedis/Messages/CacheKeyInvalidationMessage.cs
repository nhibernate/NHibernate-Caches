using System;
using System.Runtime.Serialization;

namespace NHibernate.Caches.StackExchangeRedis.Messages
{
	/// <summary>
	/// A message used to invalidate a key in the cache.
	/// </summary>
	[Serializable]
	[DataContract]
	public class CacheKeyInvalidationMessage
	{
		/// <summary>
		/// The key to invalidate.
		/// </summary>
		[DataMember]
		public string Key { get; set; }

		/// <summary>
		/// The id of the client that is sending the message.
		/// </summary>
		[DataMember]
		public int ClientId { get; set; }
	}
}
