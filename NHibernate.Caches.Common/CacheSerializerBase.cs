using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHibernate.Caches.Common
{
	/// <summary>
	/// Base class for serializing objects that will be stored in a distributed cache.
	/// </summary>
	public abstract class CacheSerializerBase
	{
		/// <summary>
		/// Serialize the object.
		/// </summary>
		/// <param name="value">The object to serialize.</param>
		/// <returns>The serialized object.</returns>
		public abstract byte[] Serialize(object value);

		/// <summary>
		/// Deserialize the object.
		/// </summary>
		/// <param name="data">The data of the object to deserialize.</param>
		/// <returns>The deserialized object.</returns>
		public abstract object Deserialize(byte[] data);
	}
}
