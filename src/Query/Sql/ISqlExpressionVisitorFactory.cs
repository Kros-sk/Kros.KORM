using System.Data;

namespace Kros.KORM.Query.Sql
{
    /// <summary>
    /// Factory for creating <see cref="ISqlExpressionVisitor"/> implementations for database connection.
    /// </summary>
    public interface ISqlExpressionVisitorFactory
    {
        /// <summary>
        /// Creates an <see cref="ISqlExpressionVisitor"/> for specified <paramref name="connection"/>.
        /// </summary>
        /// <param name="connection">Database connection.</param>
        /// <returns>Different implementations of <see cref="ISqlExpressionVisitor"/> can be returned for different
        /// connections. For example different <see cref="ISqlExpressionVisitor"/> is returned for various
        /// SQL server versions.</returns>
        ISqlExpressionVisitor CreateVisitor(IDbConnection connection);
    }
}
