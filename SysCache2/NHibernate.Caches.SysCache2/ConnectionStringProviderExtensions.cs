using System;

namespace NHibernate.Caches.SysCache2
{
    public static class ConnectionStringProviderExtensions
    {
        public static string GetConnectionStringFromName(this IConnectionStringProvider provider, string connectionName)
        {
            return String.IsNullOrEmpty(connectionName)
                ? provider.GetConnectionString()
                : provider.GetConnectionString(connectionName);
        }

        public static bool IsCompatibleWithCommandCacheDependencies(
            this IConnectionStringProvider connectionStringProvider, string connectionName)
        {
            var conString = connectionStringProvider.GetConnectionStringFromName(connectionName);
            return IsMSSQL(conString);
        }

        private static bool IsMSSQL(string conString)
        {
            return conString.Contains("Initial Catalog=");
        }
    }
}
