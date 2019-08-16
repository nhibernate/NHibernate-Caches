using StackExchange.Redis;

namespace NHibernate.Caches.StackExchangeRedis.Tests.Providers
{
	public class ConnectionMultiplexerProvider : IConnectionMultiplexerProvider
	{
		private readonly IConnectionMultiplexerProvider _default = new DefaultConnectionMultiplexerProvider();

		public IConnectionMultiplexer Get(string configuration)
		{
			var connectionMultiplexer = (ConnectionMultiplexer) _default.Get(configuration);
			connectionMultiplexer.IncludeDetailInExceptions = true;
			connectionMultiplexer.IncludePerformanceCountersInExceptions = true;
			return connectionMultiplexer;
		}
	}
}
