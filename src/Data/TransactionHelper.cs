using Kros.KORM.Properties;
using Kros.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace Kros.KORM.Data
{
    /// <summary>
    /// Helper class for managing transaction.
    /// </summary>
    internal class TransactionHelper
    {
        public const IsolationLevel DefaultIsolationLevel = IsolationLevel.ReadCommitted;
        private const int TIMEOUT_DEFAULT = 30;

        private DbConnection _connection;
        private Transaction _topTransaction;
        private bool _canCommit = true;
        private Stack<ITransaction> _transactions = new Stack<ITransaction>();

        #region Nested types

        private class Transaction : ITransaction
        {
            private readonly ConnectionHelper _connectionHelper;
            private Lazy<DbTransaction> _transaction;
            private bool _wasCommitOrRollback = false;
            private TransactionHelper _transactionHelper;

            public Transaction(TransactionHelper transactionHelper, ConnectionHelper connectionHelper, IsolationLevel isolationLevel)
            {
                _connectionHelper = connectionHelper;
                _transaction = new Lazy<DbTransaction>(() => connectionHelper.Connection.BeginTransaction(isolationLevel));
                _transactionHelper = transactionHelper;
            }

            public void Commit()
            {
                _wasCommitOrRollback = true;
                if (_transactionHelper.CanCommitTransaction)
                {
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

            public int CommandTimeout { get; set; } = TIMEOUT_DEFAULT;

            public static implicit operator DbTransaction(Transaction transaction) =>
                transaction?._transaction.Value;

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
            private TransactionHelper _transactionHelper;
            private bool _wasCommitOrRollback = false;

            public NestedTransaction(TransactionHelper transactionHelper)
            {
                _transactionHelper = transactionHelper;
            }

            public void Commit()
            {
                _transactionHelper.EndTransaction(true);
                _wasCommitOrRollback = true;
            }

            public void Dispose()
            {
                if (!_wasCommitOrRollback)
                {
                    Rollback();
                }
            }

            public void Rollback()
            {
                _transactionHelper.EndTransaction(false);
                _wasCommitOrRollback = true;
            }

            public int CommandTimeout
            {
                get { return TIMEOUT_DEFAULT; }
                set { throw new InvalidOperationException(Resources.NestedTransactionCommandTimeoutIsReadonly); }
            }
        }

        #endregion

        public TransactionHelper(DbConnection connection)
        {
            Check.NotNull(connection, nameof(connection));

            _connection = connection;
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
                _transactions.Push(new NestedTransaction(this));
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
            var cmd = _connection.CreateCommand();
            if (_topTransaction != null)
            {
                cmd.Transaction = _topTransaction as Transaction;
                cmd.CommandTimeout = _topTransaction.CommandTimeout;
            }

            return cmd;
        }
    }
}
