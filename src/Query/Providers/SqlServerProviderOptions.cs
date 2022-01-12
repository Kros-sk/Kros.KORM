using Kros.Data.BulkActions.SqlServer;
using Microsoft.Data.SqlClient;

namespace Kros.KORM.Query
{
    /// <summary>
    /// Options for SQL Server database provider.
    /// </summary>
    public class SqlServerProviderOptions
    {
        /// <summary>
        /// Bitwise flag that specifies one or more options to use with an instance of Microsoft.Data.SqlClient.SqlBulkCopy.
        /// </summary>
        public SqlBulkCopyOptions BulkCopy { get; set; } = SqlServerBulkInsert.DefaultBulkCopyOptions;
    }
}
