using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.SqlServer;

namespace NHibernate.Caches.CoreDistributedCache.SqlServer
{
	/// <summary>
	/// A Redis distributed cache factory. See <see cref="SqlServerCache" />.
	/// </summary>
	public class SqlServerFactory : IDistributedCacheFactory
	{
		private static readonly INHibernateLogger Log = NHibernateLogger.For(typeof(SqlServerFactory));
		private const string _connectionString = "connection-string";
		private const string _schemaName = "schema-name";
		private const string _tableName = "table-name";
		private const string _expiredItemsDeletionInterval = "expired-items-deletion-interval";

		private readonly SqlServerCacheOptions _options;

		/// <summary>
		/// Constructor with explicit configuration properties.
		/// </summary>
		/// <param name="connectionString">See <see cref="SqlServerCacheOptions.ConnectionString" />.</param>
		/// <param name="schemaName">See <see cref="SqlServerCacheOptions.SchemaName" />.</param>
		/// <param name="tableName">See <see cref="SqlServerCacheOptions.TableName" />.</param>
		/// <param name="expiredItemsDeletionInterval">See <see cref="SqlServerCacheOptions.ExpiredItemsDeletionInterval" />.</param>
		public SqlServerFactory(
			string connectionString, string schemaName, string tableName, TimeSpan? expiredItemsDeletionInterval)
		{
			_options = new SqlServerCacheOptions
			{
				ConnectionString = connectionString,
				SchemaName = schemaName,
				TableName = tableName,
				ExpiredItemsDeletionInterval = expiredItemsDeletionInterval
			};
		}

		/// <summary>
		/// Constructor with configuration properties. It supports <c>connection-string</c>, <c>schema-name</c>,
		/// <c>table-name</c> and <c>expired-items-deletion-interval</c> properties.
		/// See <see cref="SqlServerCacheOptions" />.
		/// </summary>
		/// <param name="properties">The configurations properties.</param>
		/// <remarks>
		/// If <c>expired-items-deletion-interval</c> is provided as an integer, this integer will be used as a number
		/// of minutes. Otherwise the setting will be parsed as a <see cref="TimeSpan" />.
		/// </remarks>
		public SqlServerFactory(IDictionary<string, string> properties)
		{
			_options = new SqlServerCacheOptions();

			if (properties == null)
				return;

			if (properties.TryGetValue(_connectionString, out var connectionString))
			{
				_options.ConnectionString = connectionString;
				Log.Info("ConnectionString set as '{0}'", connectionString);
			}
			else
				Log.Warn("No {0} property provided", _connectionString);

			if (properties.TryGetValue(_schemaName, out var schemaName))
			{
				_options.SchemaName = schemaName;
				Log.Info("SchemaName set as '{0}'", schemaName);
			}
			else
				Log.Warn("No {0} property provided", _schemaName);

			if (properties.TryGetValue(_tableName, out var tableName))
			{
				_options.TableName = tableName;
				Log.Info("TableName set as '{0}'", tableName);
			}
			else
				Log.Warn("No {0} property provided", _tableName);

			if (properties.TryGetValue(_expiredItemsDeletionInterval, out var eidi))
			{
				if (eidi != null)
				{
					if (int.TryParse(eidi, out var minutes))
						_options.ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(minutes);
					else if (TimeSpan.TryParse(eidi, out var expirationScanFrequency))
						_options.ExpiredItemsDeletionInterval = expirationScanFrequency;
					else
						Log.Warn(
							"Invalid value '{0}' for {1} setting: it is neither an int nor a TimeSpan. Ignoring.",
							eidi, _expiredItemsDeletionInterval);
				}
				else
				{
					Log.Warn("Invalid property {0}: it lacks a value. Ignoring.", _expiredItemsDeletionInterval);
				}
			}
		}

		/// <inheritdoc />
		public CacheConstraints Constraints { get; } = new CacheConstraints { MaxKeySize = 449 };

		/// <inheritdoc />
		public IDistributedCache BuildCache()
		{
			// According to https://docs.microsoft.com/en-us/aspnet/core/performance/caching/distributed#the-idistributedcache-interface
			// (see its paragraph end note) there is no need for a singleton lifetime.
			return new SqlServerCache(_options);
		}
	}
}
