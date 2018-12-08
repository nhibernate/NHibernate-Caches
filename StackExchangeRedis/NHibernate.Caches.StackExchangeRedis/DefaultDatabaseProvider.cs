using StackExchange.Redis;

namespace NHibernate.Caches.StackExchangeRedis
{
	/// <inheritdoc />
	public class DefaultDatabaseProvider : IDatabaseProvider
	{
		/// <inheritdoc />
		public IDatabase Get(IConnectionMultiplexer connectionMultiplexer, int database)
		{
			return connectionMultiplexer.GetDatabase(database);
		}
	}
}
