using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace NHibernate.Caches.Common
{
	/// <inheritdoc />
	public class BinaryCacheSerializer : ICacheSerializer
	{
		/// <inheritdoc />
		public byte[] Serialize(object value)
		{
			var serializer = new BinaryFormatter();
			using (var stream = new MemoryStream())
			{
				serializer.Serialize(stream, value);
				return stream.ToArray();
			}
		}

		/// <inheritdoc />
		public object Deserialize(byte[] value)
		{
			var serializer = new BinaryFormatter();
			using (var stream = new MemoryStream(value))
			{
				return serializer.Deserialize(stream);
			}
		}
	}
}
