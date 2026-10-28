using Kros.Utils;
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
            private bool _ended = false;

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
                    try
                    {
                        if (useAsync)
                        {
                            await _transaction.Value.CommitAsync(cancellationToken);
                        }
                        else
                        {
                            _transaction.Value.Commit();
                        }
                    }
                    catch
                    {
                        // Failed (or cancelled) commit must not leave this transaction on the stack.
                        EndOnce(false);
                        throw;
                    }
                    EndOnce(true);
                }
                else
                {
                    // A nested transaction was rolled back, so the whole transaction must be rolled back.
                    // Rollback must not be skipped or interrupted just because the token has been cancelled.
                    await RollbackCore(useAsync, CancellationToken.None);
                }
            }

            public void Rollback()
                => OnRollback(false).GetAwaiter().GetResult();

            public Task RollbackAsync(CancellationToken cancellationToken = default)
                => OnRollback(true, cancellationToken);

            private Task OnRollback(bool useAsync, CancellationToken cancellationToken = default)
            {
                _wasCommitOrRollback = true;
                return RollbackCore(useAsync, cancellationToken);
            }

            /// <summary>
            /// Rolls back the DB transaction (if there is one which can still be rolled back)
            /// and removes this transaction from the stack.
            /// </summary>
            private async Task RollbackCore(bool useAsync, CancellationToken cancellationToken)
            {
                try
                {
                    // DbTransaction.Connection is null when the transaction is no longer valid (already committed,
                    // rolled back, or aborted by the server or a lost connection). Rolling it back would throw
                    // and mask the original exception of the caller.
                    if (_transaction.IsValueCreated && _transaction.Value.Connection is not null)
                    {
                        if (useAsync)
                        {
                            await _transaction.Value.RollbackAsync(cancellationToken);
                        }
                        else
                        {
                            _transaction.Value.Rollback();
                        }
                    }
                }
                finally
                {
                    EndOnce(false);
                }
            }

            /// <summary>
            /// Removes this transaction from the stack of <see cref="TransactionHelper"/>, but only once.
            /// A caller usually rolls back after a failed commit, which must not pop the stack again.
            /// </summary>
            private void EndOnce(bool success)
            {
                if (!_ended)
                {
                    _ended = true;
                    _transactionHelper.EndTransaction(success);
                }
            }

            public int CommandTimeout { get; set; } = TIMEOUT_DEFAULT;

            public static implicit operator DbTransaction(Transaction transaction)
                => transaction?._transaction.Value;

            /// <inheritdoc/>
            public void Dispose()
            {
                if (!_wasCommitOrRollback)
                {
                    if (_transaction.IsValueCreated)
                    {
                        Rollback();
                    }
                    else
                    {
                        // No command has run, so there is no DB transaction to roll back.
                        _wasCommitOrRollback = true;
                        EndOnce(false);
                    }
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

        public async Task<ITransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
        {
            if (_transactions.Count == 0)
            {
                ConnectionHelper connectionHelper = await ConnectionHelper.OpenAsync(_connection, cancellationToken);
                _topTransaction = new Transaction(this, connectionHelper, isolationLevel);
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
