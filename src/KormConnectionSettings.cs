namespace Kros.KORM
{
    /// <summary>
    /// Settings for KORM database.
    /// </summary>
    public class KormConnectionSettings
    {
        internal const string DefaultProviderName = Kros.Data.SqlServer.SqlServerDataHelper.ClientId;
        internal const bool DefaultAutoMigrate = false;

        /// <summary>
        /// Connection string for database. This connection string is the same as input connection string,
        /// just without KORM keys.
        /// </summary>
        public string ConnectionString { get; set; }

        private string _kormProvider;

        /// <summary>
        /// KORM provider. If the value is not set, default value Microsoft SQL Server will be used.
        /// </summary>
        public string KormProvider
        {
            get => string.IsNullOrWhiteSpace(_kormProvider) ? DefaultProviderName : _kormProvider;
            set => _kormProvider = value;
        }

        /// <summary>
        /// Automatic migration value. Default is <see langword="false"/>.
        /// </summary>
        public bool AutoMigrate { get; set; } = DefaultAutoMigrate;

        /// <summary>
        /// Application name to be used in the connection string.
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Returns info about KORM connection.
        /// </summary>
        public override string ToString()
            => $"ConnectionString = {ConnectionString}; KormProvider = {KormProvider}; AutoMigrate = {AutoMigrate}";
    }
}
