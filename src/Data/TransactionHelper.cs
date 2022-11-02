﻿using Kros.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kros.KORM.Data
{
    /// <summary>
    /// Helper class for managing transaction.
    /// </summary>
    internal class TransactionHelper
    {
        public const IsolationLevel DefaultIsolationLevel = IsolationLevel.ReadCommitted;
        public const int TIMEOUT_DEFAULT = 30;

        private readonly DbConnection _connection;
        private Transaction _topTransaction;
        private bool _canCommit = true;
        private readonly Stack<ITransaction> _transactions = new Stack<ITransaction>();

        #region Nested types

        private class Transaction : ITransaction
        {
            private readonly ConnectionHelper _connectionHelper;
            private readonly Lazy<DbTransaction> _transaction;
            private readonly TransactionHelper _transactionHelper;
            private bool _wasCommitOrRollback = false;

            public Transaction(TransactionHelper transactionHelper, ConnectionHelper connectionHelper, IsolationLevel isolationLevel)
            {
                _connectionHelper = connectionHelper;
                _transaction = new Lazy<DbTransaction>(() => connectionHelper.Connection.BeginTransaction(isolationLevel));
                _transactionHelper = transactionHelper;
            }

            public void Commit()
                => OnCommit(false).GetAwaiter().GetResult();

            public Task CommitAsync(CancellationToken cancellationToken = default)
                => OnCommit(true, cancellationToken);

            private async Task OnCommit(bool useAsync, CancellationToken cancellationToken = default)
            {
                _wasCommitOrRollback = true;
                if (_transactionHelper.CanCommitTransaction)
                {
                    if (useAsync)
                    {
                        await _transaction.Value.CommitAsync(cancellationToken);
                    }
                    else
                    {
                        _transaction.Value.Commit();
                    }
                    _transactionHelper.EndTransaction(true);
                }
            }

            public void Rollback()
                => OnRollback(false).GetAwaiter().GetResult();

            public Task RollbackAsync(CancellationToken cancellationToken = default)
                => OnRollback(true, cancellationToken);

            private async Task OnRollback(bool useAsync, CancellationToken cancellationToken = default)
            {
                _wasCommitOrRollback = true;
                if (useAsync)
                {
                    _transaction.Value.Rollback();
                }
                else
                {
                    await _transaction.Value.RollbackAsync(cancellationToken);
                }
                _transactionHelper.EndTransaction(false);
            }

            public int CommandTimeout { get; set; } = TIMEOUT_DEFAULT;

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

            public Task CommitAsync(CancellationToken cancellationToken = default)
            {
                Commit();
                return Task.CompletedTask;
            }

            public Task RollbackAsync(CancellationToken cancellationToken = default)
            {
                Rollback();
                return Task.CompletedTask;
            }

            public int CommandTimeout
            {
                get => _timeout;
                set { }
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
