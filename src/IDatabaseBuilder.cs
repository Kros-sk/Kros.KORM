using Kros.KORM.Materializer;
using Kros.KORM.Query;
using System.Configuration;
using System.Data.Common;

namespace Kros.KORM
{
    /// <summary>
    /// Interface which describe builder for creating <see cref="IDatabase"/>.
    /// </summary>
    public interface IDatabaseBuilder
    {
        /// <summary>
        /// Use <paramref name="connectionString"/> which instance of <see cref="IDatabase"/> will use for accessing to database.
        /// </summary>
        /// <param name="connectionString">Connection string settings.</param>
        /// <returns>Database builder.</returns>
        IDatabaseBuilder UseConnection(ConnectionStringSettings connectionString);

        /// <summary>
        /// Use <paramref name="connectionString"/> which instance of <see cref="IDatabase"/> will use for accessing to database.
        /// </summary>
        /// <param name="connectionString">Connection string settings.</param>
        /// <param name="adoClientName">Ado client name. (System.Data.SqlClient/System.Data.OleDb)</param>
        /// <returns>Database builder.</returns>
        IDatabaseBuilder UseConnection(string connectionString, string adoClientName);

        /// <summary>
        /// Use <paramref name="connection"/> which instance of <see cref="IDatabase"/> will use for accessing to database.
        /// </summary>
        /// <param name="connection">Connection.</param>
        /// <returns>Database builder.</returns>
        IDatabaseBuilder UseConnection(DbConnection connection);

        /// <summary>
        /// Use <paramref name="queryProviderFactory"/> for creating <see cref="IQueryProvider"/>.
        /// </summary>
        /// <param name="queryProviderFactory">
        /// The query provider factory, which know create <see cref="IQueryProvider"/>.
        /// </param>
        /// <returns>Database builder.</returns>
        IDatabaseBuilder UseQueryProviderFactory(IQueryProviderFactory queryProviderFactory);

        /// <summary>
        /// Use <paramref name="modelFactory"/> for mapping classes to relation database.
        /// </summary>
        /// <param name="modelFactory">Model mapper, which will be used for mapping Object to Relation database.</param>
        /// <returns>Database builder.</returns>
        IDatabaseBuilder UseModelFactory(IModelFactory modelFactory);

        /// <summary>
        /// Use database configuration.
        /// </summary>
        /// <typeparam name="TConfiguration">Configuration type.</typeparam>
        /// <returns>Database builder.</returns>
        IDatabaseBuilder UseDatabaseConfiguration<TConfiguration>() where TConfiguration : DatabaseConfigurationBase, new();

        /// <summary>
        /// Use database configuration.
        /// </summary>
        /// <param name="databaseConfiguration">Instance of database configuration.</param>
        /// <returns>Database builder.</returns>
        IDatabaseBuilder UseDatabaseConfiguration(DatabaseConfigurationBase databaseConfiguration);

        /// <summary>
        /// Build <see cref="IDatabase"/>.
        /// </summary>
        /// <returns>KORM database access.</returns>
        IDatabase Build();
    }
}
