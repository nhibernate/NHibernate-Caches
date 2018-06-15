using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using StackExchange.Redis;

namespace NHibernate.Caches.StackExRedis
{
	/// <summary>
	/// A Redis serializer that uses <see cref="BinaryFormatter"/> to serialize and deserialize objects.
	/// </summary>
	public class BinaryRedisSerializer : IRedisSerializer
	{
		/// <inheritdoc />
		public RedisValue Serialize(object value)
		{
			var serializer = new BinaryFormatter();
			using (var stream = new MemoryStream())
			{
				serializer.Serialize(stream, value);
				return stream.ToArray();
			}
		}

		/// <inheritdoc />
		public object Deserialize(RedisValue value)
		{
			if (value.IsNull)
			{
				return null;
			}

			var serializer = new BinaryFormatter();
			using (var stream = new MemoryStream(value))
			{
				return serializer.Deserialize(stream);
			}
		}
	}
}
