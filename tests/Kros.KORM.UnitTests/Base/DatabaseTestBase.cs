using Kros.Data;
using Kros.UnitTests;
using System;
using System.Data.SqlClient;

namespace Kros.KORM.UnitTests.Base
{
    /// <summary>
    /// Base class for database integration tests
    /// </summary>
    public abstract class DatabaseTestBase
    {
        /// <summary>
        /// Connection string to testing database server.
        /// </summary>
        protected virtual string BaseConnectionString { get; private set; }

        /// <summary>
        /// Base database name.
        /// </summary>
        protected virtual string BaseDatabaseName => $"KORM_{this.ToString()}";

        public DatabaseTestBase()
        {
            BaseConnectionString = Integration.IntegrationTestConfig.ConnectionString;
        }

        protected virtual IDatabase CreateDatabase(string initScript)
            => new TestDatabase(new SqlServerTestHelper(BaseConnectionString, BaseDatabaseName, initScript));

        protected virtual IDatabase CreateDatabase(params string[] initDatabaseScripts)
           => new TestDatabase(new SqlServerTestHelper(BaseConnectionString, BaseDatabaseName, initDatabaseScripts));

        protected bool IsAnyReaderOpened(SqlConnection connection, string tableName)
        {
            var connectionString = connection.ConnectionString;
            if (connectionString.IndexOf("MultipleActiveResultSets", StringComparison.InvariantCultureIgnoreCase) >= 0)
            {
                throw new ArgumentException(
                    $"Connection string cannot contains settings MultipleActiveResultSets",
                    nameof(connection));
            }

            using(ConnectionHelper.OpenConnection(connection))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT * FROM {tableName}";
                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                    }
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
        }

        protected class TestDatabase : Database
        {
            private SqlServerTestHelper _sqlServerTestHelper;

            public TestDatabase(SqlServerTestHelper sqlServerTestHelper) : base(sqlServerTestHelper.Connection)
            {
                _sqlServerTestHelper = sqlServerTestHelper;
            }

            public string ConnectionString => _sqlServerTestHelper.Connection.ConnectionString;

            public SqlConnection Connection => _sqlServerTestHelper.Connection;

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                _sqlServerTestHelper?.Dispose();
                _sqlServerTestHelper = null;
            }
        }
    }
}
