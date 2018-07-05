using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHibernate.Caches.Common
{
	/// <summary>
	/// Defines methods for serializing objects that will be stored in a distributed cache.
	/// </summary>
	public interface ICacheSerializer
	{
		/// <summary>
		/// Serialize the object.
		/// </summary>
		/// <param name="value">The object to serialize.</param>
		/// <returns>The serialized object.</returns>
		byte[] Serialize(object value);

		/// <summary>
		/// Deserialize the object.
		/// </summary>
		/// <param name="data">The data of the object to deserialize.</param>
		/// <returns>The deserialized object.</returns>
		object Deserialize(byte[] data);
	}
}
