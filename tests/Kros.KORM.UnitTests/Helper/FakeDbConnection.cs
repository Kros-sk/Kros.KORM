using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Kros.KORM.UnitTests.Helper
{
    /// <summary>
    /// Fake <see cref="DbConnection"/> which counts calls, for unit tests.
    /// </summary>
    internal class FakeDbConnection : DbConnection
    {
        private ConnectionState _state = ConnectionState.Closed;

        public int OpenCount { get; private set; }

        public int OpenAsyncCount { get; private set; }

        public int CloseCount { get; private set; }

        public FakeDbTransaction LastTransaction { get; private set; }

        public override string ConnectionString { get; set; } = string.Empty;

        public override string Database => "Fake";

        public override string DataSource => "Fake";

        public override string ServerVersion => "1.0";

        public override ConnectionState State => _state;

        public override void ChangeDatabase(string databaseName)
        {
        }

        public override void Open()
        {
            OpenCount++;
            _state = ConnectionState.Open;
        }

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OpenAsyncCount++;
            _state = ConnectionState.Open;
            return Task.CompletedTask;
        }

        public override void Close()
        {
            CloseCount++;
            _state = ConnectionState.Closed;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            LastTransaction = new FakeDbTransaction(this, isolationLevel);
            return LastTransaction;
        }

        protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
    }

    /// <summary>
    /// Fake <see cref="DbTransaction"/> which counts sync and async calls separately, for unit tests.
    /// </summary>
    internal class FakeDbTransaction : DbTransaction
    {
        private readonly FakeDbConnection _connection;
        private readonly IsolationLevel _isolationLevel;

        public FakeDbTransaction(FakeDbConnection connection, IsolationLevel isolationLevel)
        {
            _connection = connection;
            _isolationLevel = isolationLevel;
        }

        public int CommitCount { get; private set; }

        public int CommitAsyncCount { get; private set; }

        public int RollbackCount { get; private set; }

        public int RollbackAsyncCount { get; private set; }

        /// <summary>
        /// When set, <see cref="Commit"/> throws this exception (simulates a failed commit).
        /// </summary>
        public Exception ThrowOnCommit { get; set; }

        protected override DbConnection DbConnection => _connection;

        public override IsolationLevel IsolationLevel => _isolationLevel;

        public override void Commit()
        {
            if (ThrowOnCommit is not null)
            {
                throw ThrowOnCommit;
            }
            CommitCount++;
        }

        public override Task CommitAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CommitAsyncCount++;
            return Task.CompletedTask;
        }

        public override void Rollback() => RollbackCount++;

        public override Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RollbackAsyncCount++;
            return Task.CompletedTask;
        }
    }
}
