using StackExchange.Redis;

namespace NHibernate.Caches.StackExRedis
{
	/// <summary>
	/// Defines a method to provide an <see cref="IConnectionMultiplexer"/> instance.
	/// </summary>
	public interface IConnectionMultiplexerProvider
	{
		/// <summary>
		/// Provide the <see cref="IConnectionMultiplexer"/> for the StackExchange.Redis configuration string.
		/// </summary>
		/// <param name="configuration">The StackExchange.Redis configuration string</param>
		/// <returns>The <see cref="IConnectionMultiplexer"/> instance.</returns>
		IConnectionMultiplexer Get(string configuration);
	}
}
