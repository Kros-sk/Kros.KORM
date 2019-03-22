using Kros.KORM.Materializer;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Configuration;
using Kros.KORM.Metadata;

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
        IQueryProvider Create(ConnectionStringSettings connectionString, IModelBuilder modelBuilder, IDatabaseMapper databaseMapper);
    }
}
