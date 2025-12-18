using Kros.KORM.Materializer;
using Kros.KORM.Metadata;
using System.Data.Common;

namespace Kros.KORM.Query
{
    /// <summary>
    /// Interface, which describe factory for creating provider.
    /// </summary>
    public interface IQueryProviderFactory
    {
        /// <summary>
        /// Creates the specified QueryProvider.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="modelBuilder">The model builder.</param>
        /// <param name="databaseMapper">Database mapper.</param>
        /// <returns>
        /// Instance of IQueryProvider.
        /// </returns>
        IQueryProvider Create(DbConnection connection, IModelBuilder modelBuilder, IDatabaseMapper databaseMapper);

        /// <summary>
        /// Creates the specified QueryProvider.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="modelBuilder">The model builder.</param>
        /// <param name="databaseMapper">Database mapper.</param>
        /// <returns>
        ///  Instance of IQueryProvider.
        /// </returns>
        IQueryProvider Create(KormConnectionSettings connectionString, IModelBuilder modelBuilder, IDatabaseMapper databaseMapper);
    }
}
