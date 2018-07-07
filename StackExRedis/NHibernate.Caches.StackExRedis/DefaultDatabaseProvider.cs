using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace NHibernate.Caches.StackExRedis
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
