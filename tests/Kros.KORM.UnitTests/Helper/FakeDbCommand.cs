using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Kros.KORM.UnitTests.Helper
{
    /// <summary>
    /// Fake <see cref="DbCommand"/> which returns given reader from asynchronous execution, for unit tests.
    /// </summary>
    /// <remarks>
    /// NSubstitute cannot intercept non-virtual <c>ExecuteReaderAsync</c> overloads, so this class overrides
    /// <see cref="ExecuteDbDataReaderAsync(CommandBehavior, CancellationToken)"/> instead.
    /// Synchronous execution throws.
    /// </remarks>
    internal class FakeDbCommand : DbCommand
    {
        private readonly DbDataReader _reader;

        public FakeDbCommand(DbConnection connection, DbDataReader reader)
        {
            DbConnection = connection;
            _reader = reader;
        }

        public bool ExecuteReaderAsyncCalled { get; private set; }

        public override string CommandText { get; set; } = string.Empty;
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection => throw new NotSupportedException();
        protected override DbTransaction DbTransaction { get; set; }

        public override void Cancel()
        {
        }

        public override int ExecuteNonQuery() => throw new NotSupportedException();

        public override object ExecuteScalar() => throw new NotSupportedException();

        public override void Prepare()
        {
        }

        protected override DbParameter CreateDbParameter() => throw new NotSupportedException();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
            => throw new InvalidOperationException("Synchronous ExecuteReader must not be used");

        protected override Task<DbDataReader> ExecuteDbDataReaderAsync(
            CommandBehavior behavior,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ExecuteReaderAsyncCalled = true;
            return Task.FromResult(_reader);
        }
    }
}
