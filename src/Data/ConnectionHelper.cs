using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kros.KORM.Data
{
    /// <summary>
    /// Class for helping with connection.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    internal class ConnectionHelper : IDisposable
    {
        private bool _disposedValue = false;
        private DbConnection _connection;

        private ConnectionHelper(DbConnection connection, bool closeConnection)
        {
            _connection = connection;
            CloseConnection = closeConnection;
        }

        public ConnectionHelper(DbConnection connection)
            : this(connection, !connection.State.HasFlag(ConnectionState.Open))
        {
            if (CloseConnection)
            {
                _connection.Open();
            }
        }

        /// <summary>
        /// Opens the <paramref name="connection"/> asynchronously, if it is not already open.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>Helper which closes the connection on dispose, if this call opened it.</returns>
        internal static async Task<ConnectionHelper> OpenAsync(
            DbConnection connection,
            CancellationToken cancellationToken)
        {
            bool closeConnection = !connection.State.HasFlag(ConnectionState.Open);
            if (closeConnection)
            {
                await connection.OpenAsync(cancellationToken);
            }
            return new ConnectionHelper(connection, closeConnection);
        }

        public bool CloseConnection { get; }

        public DbConnection Connection => _connection;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (CloseConnection)
                    {
                        _connection.Close();
                        _connection = null;
                    }
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
