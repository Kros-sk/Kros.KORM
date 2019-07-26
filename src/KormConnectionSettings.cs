using Kros.Utils;
using System.Data.Common;

namespace Kros.KORM
{
    /// <summary>
    /// Settings for KORM database.
    /// </summary>
    public class KormConnectionSettings
    {
        private const string DefaultProviderName = Kros.Data.SqlServer.SqlServerDataHelper.ClientId;
        private const bool DefaultAutoMigrate = false;

        /// <summary>
        /// Key for connection string for setting KORM database provider. If the provider is not set in connection string,
        /// Microsoft SQL Server provider is used.
        /// </summary>
        public const string KormProviderKey = "KormProvider";

        /// <summary>
        /// Key for connection string for setting if automatic migrations are enabled.
        /// If the value is not set in connection string, automatic migrations are disabled.
        /// </summary>
        public const string KormAutoMigrateKey = "KormAutoMigrate";

        private static string GetKormProvider(DbConnectionStringBuilder cnstrBuilder)
        {
            if (cnstrBuilder.TryGetValue(KormProviderKey, out object cnstrProviderName))
            {
                cnstrBuilder.Remove(KormProviderKey);
                string providerName = (string)cnstrProviderName;
                if (!string.IsNullOrWhiteSpace(providerName))
                {
                    return providerName;
                }
            }
            return DefaultProviderName;
        }

        private static bool GetKormAutoMigrate(DbConnectionStringBuilder cnstrBuilder)
        {
            if (cnstrBuilder.TryGetValue(KormAutoMigrateKey, out object cnstrAutoMigrate))
            {
                cnstrBuilder.Remove(KormAutoMigrateKey);
                if (bool.TryParse((string)cnstrAutoMigrate, out bool autoMigrate))
                {
                    return autoMigrate;
                }
            }
            return DefaultAutoMigrate;
        }

        /// <summary>
        /// Parses input <paramref name="connectionString"/> string. Values of KORM keys are removed from connection string
        /// and set to appropriate properties. Missing KORM keys are set to default values. If KORM provider is not set
        /// in connection string, Microsoft SQL Server will be used.
        /// </summary>
        /// <param name="connectionString">Input connection string.</param>
        public KormConnectionSettings(string connectionString)
        {
            Check.NotNullOrWhiteSpace(connectionString, nameof(connectionString));

            var cnstrBuilder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };
            KormProvider = GetKormProvider(cnstrBuilder);
            AutoMigrate = GetKormAutoMigrate(cnstrBuilder);
            ConnectionString = cnstrBuilder.ConnectionString; // Previous methods remove keys, so we want clean connection string.
        }

        /// <summary>
        /// Connection string for database. This connection string is the same as input connection string,
        /// just without KORM keys.
        /// </summary>
        public string ConnectionString { get; }

        /// <summary>
        /// KORM provider, parsed from input connection: <c>KormProvider</c> key.
        /// If the key is not present in the connection string, default value for Microsoft SQL Server will be used.
        /// </summary>
        public string KormProvider { get; }

        /// <summary>
        /// Automatic migration value parsed from input connection: <c>KormAutoMigrate</c> key.
        /// If the key was not present in the connection string, default value <see langword="false"/> will be used.
        /// </summary>
        public bool AutoMigrate { get; }
    }
}
