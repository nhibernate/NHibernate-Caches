using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace NHibernate.Caches.StackExRedis
{
	/// <inheritdoc />
	public class DefaultConnectionMultiplexerProvider : IConnectionMultiplexerProvider
	{
		private static readonly INHibernateLogger Log = NHibernateLogger.For(typeof(DefaultConnectionMultiplexerProvider));

		/// <inheritdoc />
		public IConnectionMultiplexer Get(string configuration)
		{
			TextWriter textWriter = Log.IsDebugEnabled() ? new NHibernateTextWriter(Log) : null;
			var connectionMultiplexer = ConnectionMultiplexer.Connect(configuration, textWriter);
			connectionMultiplexer.PreserveAsyncOrder = false; // Recommended setting
			return connectionMultiplexer;
		}
	}
}
