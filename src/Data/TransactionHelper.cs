using Kros.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace Kros.KORM.Data
{
    /// <summary>
    /// Helper class for managing transaction.
    /// </summary>
    internal class TransactionHelper
    {
        public const IsolationLevel DefaultIsolationLevel = IsolationLevel.ReadCommitted;
        private const int DefaultCommandTimeout = 30;

        private readonly DbConnection _connection;
        private Transaction _topTransaction;
        private bool _canCommit = true;
        private readonly Stack<ITransaction> _transactions = new Stack<ITransaction>();

        #region Nested types

        private class Transaction : ITransaction
        {
            private readonly TransactionHelper _transactionHelper;
            private readonly ConnectionHelper _connectionHelper;
            private readonly Lazy<DbTransaction> _transaction;
            private readonly TransactionHelper _transactionHelper;
            private bool _wasCommitOrRollback = false;

            public Transaction(
                TransactionHelper transactionHelper,
                ConnectionHelper connectionHelper,
                IsolationLevel isolationLevel)
            {
                _transactionHelper = transactionHelper;
                _connectionHelper = connectionHelper;
                _transaction = new Lazy<DbTransaction>(() => connectionHelper.Connection.BeginTransaction(isolationLevel));
            }

            public void Commit()
            {
                if (_transactionHelper.CanCommitTransaction)
                {
                    _wasCommitOrRollback = true;
                    _transaction.Value.Commit();
                    _transactionHelper.EndTransaction(true);
                }
            }

            public void Rollback()
            {
                _wasCommitOrRollback = true;
                _transaction.Value.Rollback();
                _transactionHelper.EndTransaction(false);
            }

            public int CommandTimeout { get; set; } = DefaultCommandTimeout;

            public static implicit operator DbTransaction(Transaction transaction)
                => transaction?._transaction.Value;

            /// <inheritdoc/>
            public void Dispose()
            {
                if (!_wasCommitOrRollback)
                {
                    Rollback();
                }
                if (_transaction.IsValueCreated)
                {
                    _transaction.Value.Dispose();
                }
                _connectionHelper.Dispose();
            }
        }

        private class NestedTransaction : ITransaction
        {
            private readonly TransactionHelper _transactionHelper;
            private bool _wasCommitOrRollback = false;
            private readonly int _timeout;

            public NestedTransaction(TransactionHelper transactionHelper, int timeout)
            {
                _transactionHelper = transactionHelper;
                _timeout = timeout;
            }

            public void Commit()
            {
                _wasCommitOrRollback = true;
                _transactionHelper.EndTransaction(true);
            }

            public void Rollback()
            {
                _wasCommitOrRollback = true;
                _transactionHelper.EndTransaction(false);
            }

            public void Dispose()
            {
                if (!_wasCommitOrRollback)
                {
                    Rollback();
                }
            }

            public int CommandTimeout
            {
                get => DefaultCommandTimeout;
                set => throw new InvalidOperationException(Resources.NestedTransactionCommandTimeoutIsReadonly);
            }
        }

        #endregion

        public TransactionHelper(DbConnection connection)
        {
            _connection = Check.NotNull(connection, nameof(connection));
        }

        public ITransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            if (_transactions.Count == 0)
            {
                _topTransaction = new Transaction(this, new ConnectionHelper(_connection), isolationLevel);
                _transactions.Push(_topTransaction);
                _canCommit = true;
            }
            else
            {
                _transactions.Push(new NestedTransaction(this, _topTransaction.CommandTimeout));
            }
            return _transactions.Peek();
        }

        public ITransaction BeginTransaction() => BeginTransaction(DefaultIsolationLevel);

        private bool CanCommitTransaction => _canCommit;

        public DbTransaction CurrentTransaction => _topTransaction;

        private void EndTransaction(bool success)
        {
            _canCommit &= success;
            _transactions.Pop();
            if (!_transactions.Any())
            {
                _topTransaction = null;
            }
        }

        public DbCommand CreateCommand()
        {
            DbCommand cmd = _connection.CreateCommand();
            if (_topTransaction != null)
            {
                cmd.Transaction = _topTransaction;
                cmd.CommandTimeout = _topTransaction.CommandTimeout;
            }
            return cmd;
        }
    }
}
