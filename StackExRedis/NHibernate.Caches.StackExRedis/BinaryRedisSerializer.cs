using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace NHibernate.Caches.StackExRedis
{
	/// <summary>
	/// A Redis serializer that uses <see cref="BinaryFormatter"/> to serialize and deserialize objects.
	/// </summary>
	public class BinaryRedisSerializer : IRedisSerializer
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
