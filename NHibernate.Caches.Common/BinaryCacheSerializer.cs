using System.Runtime.Serialization.Formatters.Binary;
using NHibernate.Util;

namespace NHibernate.Caches.Common
{
	/// <summary>
	/// A binary serializer, using <see cref="BinaryFormatter"/>.
	/// </summary>
	public class BinaryCacheSerializer : CacheSerializerBase
	{
		/// <inheritdoc />
		public override byte[] Serialize(object value)
		{
			return SerializationHelper.Serialize(value);
		}

		/// <inheritdoc />
		public override object Deserialize(byte[] value)
		{
			return SerializationHelper.Deserialize(value);
		}
	}
}
