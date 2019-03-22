using System;

namespace Kros.KORM.Data
{
    /// <summary>
    /// Represent database transaction.
    /// Instances of this class are obtained from <see cref="IDatabase.BeginTransaction()" autoUpgrade="true"/> and it is not designed to be directly constructed in your application code.
    /// </summary>
    public interface ITransaction : IDisposable
    {
        /// <summary>
        /// Commits all changes made to the database in the current transaction.
        /// </summary>
        void Commit();

        /// <summary>
        /// Discards all changes made to the database in the current transaction.
        /// </summary>
        void Rollback();

        /// <summary>
        /// The time in seconds to wait for the <see cref="System.Data.Common.DbCommand.CommandTimeout">command</see> in this transaction to execute.
        /// If not set, default value (30 s) will be used.
        /// Caution: Can be set only for main transaction (nested will share this value).
        /// </summary>
        /// <remarks>
        /// A value of 0 indicates no limit (an attempt to execute a command will wait indefinitely).
        /// </remarks>
        int CommandTimeout { get; set; }
    }
}
