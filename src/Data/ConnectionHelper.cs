using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

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

        public ConnectionHelper(DbConnection connection)
        {
            _connection = connection;
            CloseConnection = !_connection.State.HasFlag(ConnectionState.Open);
            if (CloseConnection)
            {
                _connection.Open();
            }
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
