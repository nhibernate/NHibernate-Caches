using StackExchange.Redis;

namespace NHibernate.Caches.StackExRedis
{
	/// <summary>
	/// Defines a method to provide an <see cref="IDatabase"/> instance.
	/// </summary>
	public interface IDatabaseProvider
	{
		/// <summary>
		/// Provide the <see cref="IDatabase"/> for the given <see cref="IConnectionMultiplexer"/> and database index.
		/// </summary>
		/// <param name="connectionMultiplexer">The connection multiplexer.</param>
		/// <param name="database">The database index.</param>
		/// <returns>The <see cref="IDatabase"/> instance.</returns>
		IDatabase Get(IConnectionMultiplexer connectionMultiplexer, int database);
	}
}
