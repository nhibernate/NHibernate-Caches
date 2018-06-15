using StackExchange.Redis;

namespace NHibernate.Caches.StackExRedis
{
	/// <summary>
	/// Defines methods for serializing and deserializing objects that will be stored/retrieved for Redis.
	/// </summary>
	public interface IRedisSerializer
	{
		/// <summary>
		/// Serialize the object to a <see cref="RedisValue"/> to be stored into Redis.
		/// </summary>
		/// <param name="value">The object to serialize.</param>
		/// <returns>A serialized <see cref="RedisValue"/> that can be stored into Redis.</returns>
		RedisValue Serialize(object value);

		/// <summary>
		/// Deserialize the <see cref="RedisValue"/> that was retrieved from Redis.
		/// </summary>
		/// <param name="value">The value to deserialize.</param>
		/// <returns>The object that was serialized.</returns>
		object Deserialize(RedisValue value);
	}
}
