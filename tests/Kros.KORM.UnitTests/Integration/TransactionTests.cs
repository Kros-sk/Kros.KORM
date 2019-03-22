using FluentAssertions;
using Kros.Data.SqlServer;
using Kros.KORM.Metadata.Attribute;
using Kros.KORM.Query;
using Kros.KORM.UnitTests.Base;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Xunit;

namespace Kros.KORM.UnitTests.Integration
{
    public partial class TransactionTests : DatabaseTestBase
    {
        #region SQL Scripts

        private static string CreateTable_TestTable =
$@"CREATE TABLE [dbo].[Invoices](
    [Id] [int] NOT NULL,
    [Code] [nvarchar](10) NOT NULL,
    [Description] [nvarchar](50) NOT NULL,
    CONSTRAINT [PK_Invoices] PRIMARY KEY CLUSTERED 
    (
        [Id] ASC
    )
) ON [PRIMARY]
";

        private static string CreateProcedure_WaitForTwoSeconds =
$@" CREATE PROCEDURE [dbo].[WaitForTwoSeconds] AS 
    BEGIN
        SET NOCOUNT ON;
        WAITFOR DELAY '00:00:02';
    END";

        #endregion

        #region Tests

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ImplicitTransactionShould_CommitData(bool openConnection)
        {
            DoTestWithConnection(openConnection, ImplicitTransactionCommitData, CreateDatabase);
        }

        private void ImplicitTransactionCommitData(TestDatabase korm)
        {
            var dbSet = korm.Query<Invoice>().AsDbSet();

            dbSet.Add(CreateTestData());
            dbSet.CommitChanges();

            DatabaseShouldContainInvoices(korm.ConnectionString, CreateTestData());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ImplicitTransactionShould_CommitDataWhenBulkInsertWasCalled(bool openConnection)
        {
            DoTestWithConnection(
                openConnection,
                (db) => ImplicitTransactionBulkInsertCommit(db, BulkInsertAddItems),
                CreateDatabase);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ImplicitTransactionShould_CommitDataWhenBulkInsertEnumerableWasCalled(bool openConnection)
        {
            DoTestWithConnection(
                openConnection,
                (db) => ImplicitTransactionBulkInsertCommit(db, BulkInsertEnumerableItems),
                CreateDatabase);
        }

        private void ImplicitTransactionBulkInsertCommit(TestDatabase korm, Action<IDbSet<Invoice>> action)
        {
            var dbSet = korm.Query<Invoice>().AsDbSet();

            action.Invoke(dbSet);

            DatabaseShouldContainInvoices(korm.ConnectionString, CreateTestData());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ImplicitTransactionShould_BulkInsertThrowsException(bool openConnection)
        {
            DoTestWithConnection(openConnection, ImplicitTransactionBulkInsertThrowsException, CreateDatabase);
        }

        private void ImplicitTransactionBulkInsertThrowsException(TestDatabase korm)
        {
            var dbSet = korm.Query<Invoice>().AsDbSet();

            Action bulkInsertAction = () => dbSet.BulkInsert(null);
            bulkInsertAction.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExplicitTransactionShould_CommitData(bool openConnection)
        {
            DoTestWithConnection(openConnection, ExplicitTransactionCommitData, CreateDatabase);
        }

        private void ExplicitTransactionCommitData(TestDatabase korm)
        {
            using (var transaction = korm.BeginTransaction())
            {
                var dbSet = korm.Query<Invoice>().AsDbSet();

                dbSet.Add(CreateTestData());
                dbSet.CommitChanges();

                transaction.Commit();

                DatabaseShouldContainInvoices(korm.ConnectionString, CreateTestData());
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExplicitTransactionShould_CommitDataWhenBulkInsertWasCalled(bool openConnection)
        {
            DoTestWithConnection(
                openConnection,
                (db) => ExplicitTransactionBulkInsertCommit(db, BulkInsertAddItems),
                CreateDatabase);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExplicitTransactionShould_CommitDataWhenBulkInsertEnumerableWasCalled(bool openConnection)
        {
            DoTestWithConnection(
                openConnection,
                (db) => ExplicitTransactionBulkInsertCommit(db, BulkInsertEnumerableItems),
                CreateDatabase);
        }

        private void ExplicitTransactionBulkInsertCommit(TestDatabase korm, Action<IDbSet<Invoice>> action)
        {
            using (var transaction = korm.BeginTransaction())
            {
                var dbSet = korm.Query<Invoice>().AsDbSet();

                action.Invoke(dbSet);

                transaction.Commit();

                DatabaseShouldContainInvoices(korm.ConnectionString, CreateTestData());
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExplicitTransactionShould_CommitDataAfterOtherTransactionEndWithRollback(bool openConnection)
        {
            DoTestWithConnection(openConnection, ExplicitTransactionCommitDataAfterOtherTransactionEndWithRollback, CreateDatabase);
        }

        private void ExplicitTransactionCommitDataAfterOtherTransactionEndWithRollback(TestDatabase korm)
        {
            using (var transaction = korm.BeginTransaction())
            {
                var dbSet = korm.Query<Invoice>().AsDbSet();

                dbSet.Add(CreateTestData());
                dbSet.CommitChanges();

                transaction.Rollback();

                DatabaseShouldBeEmpty(korm);
            }
            using (var transaction = korm.BeginTransaction())
            {
                var dbSet = korm.Query<Invoice>().AsDbSet();

                dbSet.Add(CreateTestData());
                dbSet.CommitChanges();

                transaction.Commit();

                DatabaseShouldContainInvoices(korm.ConnectionString, CreateTestData());
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExplicitTransactionShould_RollbackData(bool openConnection)
        {
            DoTestWithConnection(openConnection, ExplicitTransactionRollbackData, CreateDatabase);
        }

        private void ExplicitTransactionRollbackData(TestDatabase korm)
        {
            using (var transaction = korm.BeginTransaction())
            {
                var dbSet = korm.Query<Invoice>().AsDbSet();

                dbSet.Add(CreateTestData());
                dbSet.CommitChanges();

                transaction.Rollback();

                DatabaseShouldBeEmpty(korm);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExplicitTransactionShould_RollbackDataWhenBulkInsertWasCalled(bool openConnection)
        {
            DoTestWithConnection(
                openConnection,
                (db) => ExplicitTransactionRollbackBulkInsert(db, BulkInsertAddItems),
                CreateDatabase);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExplicitTransactionShould_RollbackDataWhenBulkInsertEnumerableWasCalled(bool openConnection)
        {
            DoTestWithConnection(
                openConnection,
                (db) => ExplicitTransactionRollbackBulkInsert(db, BulkInsertEnumerableItems),
                CreateDatabase);
        }

        private void ExplicitTransactionRollbackBulkInsert(TestDatabase korm, Action<IDbSet<Invoice>> action)
        {
            using (var transaction = korm.BeginTransaction())
            {
                var dbSet = korm.Query<Invoice>().AsDbSet();

                action.Invoke(dbSet);

                transaction.Rollback();

                DatabaseShouldBeEmpty(korm);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExplicitTransactionShould_NotChangeDataWhenBulkInsertCommitWasNotCalled(bool openConnection)
        {
            DoTestWithConnection(
                openConnection,
                (db) => ExplicitTransactionBulkInsertCommitNotCalled(db, BulkInsertAddItems),
                CreateDatabase);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExplicitTransactionShould_NotChangeDataWhenBulkInsertEnumerableCommitWasNotCalled(bool openConnection)
        {
            DoTestWithConnection(
                openConnection,
                (db) => ExplicitTransactionBulkInsertCommitNotCalled(db, BulkInsertEnumerableItems),
                CreateDatabase);
        }

        private void ExplicitTransactionBulkInsertCommitNotCalled(TestDatabase database, Action<IDbSet<Invoice>> action)
        {
            using (var korm = CreateDatabase())
            {
                using (var transaction = korm.BeginTransaction())
                {
                    var dbSet = korm.Query<Invoice>().AsDbSet();

                    action.Invoke(dbSet);
                }
                DatabaseShouldBeEmpty(korm);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DataShould_BeAccessibleFromTransaction(bool openConnection)
        {
            DoTestWithConnection(openConnection, DataAccessibleFromTransaction, CreateDatabase);
        }

        private void DataAccessibleFromTransaction(TestDatabase korm)
        {
            using (var transaction = korm.BeginTransaction())
            {
                var dbSet = korm.Query<Invoice>().AsDbSet();

                dbSet.Add(CreateTestData());
                dbSet.CommitChanges();

                DatabaseShouldContainInvoices(korm, CreateTestData());

                transaction.Rollback();

                korm.Query<Invoice>().Should().BeEmpty();
                DatabaseShouldBeEmpty(korm);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExplicitTransactionShould_RollbackMultipleCommit(bool openConnection)
        {
            DoTestWithConnection(openConnection, ExplicitTransactionRollbackMultipleCommits, CreateDatabase);
        }

        private void ExplicitTransactionRollbackMultipleCommits(TestDatabase korm)
        {
            using (var transaction = korm.BeginTransaction())
            {
                var dbSet = korm.Query<Invoice>().AsDbSet();

                dbSet.Add(CreateTestData());
                dbSet.CommitChanges();

                DatabaseShouldContainInvoices(korm, CreateTestData());

                dbSet.Add(new Invoice() { Id = 4, Code = "0004", Description = "Item 4" });
                dbSet.CommitChanges();

                transaction.Rollback();

                korm.Query<Invoice>().Should().BeEmpty();
                DatabaseShouldBeEmpty(korm);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExplicitTransactionShould_KeepMasterConnectionStateWhenCommitWasCalled(bool openConnection)
        {
            DoTestWithConnection(openConnection, ExplicitTransactionCommit, CreateDatabase);
        }

        private void ExplicitTransactionCommit(TestDatabase database)
        {
            using (var korm = new Database(database.ConnectionString, SqlServerDataHelper.ClientId))
            using (var transaction = korm.BeginTransaction())
            {
                var dbSet = korm.Query<Invoice>().AsDbSet();

                dbSet.Add(CreateTestData());
                dbSet.CommitChanges();

                transaction.Commit();

                DatabaseShouldContainInvoices(database.ConnectionString, CreateTestData());
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExplicitTransactionShould_KeepMasterConnectionStateWhenRollbackWasCalled(bool openConnection)
        {
            DoTestWithConnection(openConnection, ExplicitTransactionRollback, CreateDatabase);
        }

        private void ExplicitTransactionRollback(TestDatabase database)
        {
            using (var korm = new Database(database.ConnectionString, SqlServerDataHelper.ClientId))
            using (var transaction = korm.BeginTransaction())
            {
                var dbSet = korm.Query<Invoice>().AsDbSet();

                dbSet.Add(CreateTestData());
                dbSet.CommitChanges();

                transaction.Rollback();

                korm.Query<Invoice>().Should().BeEmpty();
                DatabaseShouldBeEmpty(database);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExplicitTransactionShould_KeepMasterConnectionStateWhenRollbackWasCalledAfterBulkInsert(bool openConnection)
        {
            DoTestWithConnection(openConnection, ExplicitTransactionRollbackAfterBulkInsert, CreateDatabase);
        }

        private void ExplicitTransactionRollbackAfterBulkInsert(TestDatabase database)
        {
            using (var korm = new Database(database.ConnectionString, SqlServerDataHelper.ClientId))
            using (var transaction = korm.BeginTransaction())
            {
                var dbSet = korm.Query<Invoice>().AsDbSet();

                dbSet.Add(CreateTestData());
                dbSet.BulkInsert();

                transaction.Rollback();

                korm.Query<Invoice>().Should().BeEmpty();
                DatabaseShouldBeEmpty(database);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExplicitTransactionShould_KeepMasterConnectionStateWhenCommitWasCalledAfterBulkInsert(bool openConnection)
        {
            DoTestWithConnection(openConnection, ExplicitTransactionCommitAfterBulkInsert, CreateDatabase);
        }

        private void ExplicitTransactionCommitAfterBulkInsert(TestDatabase database)
        {
            using (var korm = new Database(database.ConnectionString, SqlServerDataHelper.ClientId))
            using (var transaction = korm.BeginTransaction())
            {
                var dbSet = korm.Query<Invoice>().AsDbSet();

                dbSet.Add(CreateTestData());
                dbSet.BulkInsert();

                transaction.Commit();
            }

            DatabaseShouldContainInvoices(database.ConnectionString, CreateTestData());
        }

        [Fact]
        public void ExplicitTransactionShould_ThrowCommandTimeoutExceptionWhenIsSetTooSmall()
        {
            using (var database = CreateAndInitDatabase(CreateProcedure_WaitForTwoSeconds))
            using (var korm = new Database(database.ConnectionString, SqlServerDataHelper.ClientId))
            using (var transaction = korm.BeginTransaction())
            {
                transaction.CommandTimeout = 1;

                string sql = @"EXEC WaitForTwoSeconds";
                Action commit = () => { korm.ExecuteScalar(sql); };

                commit.Should().Throw<SqlException>().Which.Message.Contains("Timeout");
            }
        }

        [Fact]
        public void ExplicitTransactionShould_ThrowInvalidOperationExceptionWhenCommandTimeoutSetForNestedTransaction()
        {
            using (var database = CreateAndInitDatabase(CreateProcedure_WaitForTwoSeconds))
            using (var korm = new Database(database.ConnectionString, SqlServerDataHelper.ClientId))
            using (var mainTransaction = korm.BeginTransaction())
            {
                using (var nestedTransaction = korm.BeginTransaction())
                {
                    mainTransaction.CommandTimeout = 1;
                    Action setCommandTimeout = () => { nestedTransaction.CommandTimeout = 3; };

                    setCommandTimeout.Should().Throw<InvalidOperationException>();
                }
            }
        }

        [Fact]
        public void ExplicitTransactionShould_NotThrowCommandTimeoutExceptionWhenIsSetSufficient()
        {
            using (var database = CreateAndInitDatabase(CreateProcedure_WaitForTwoSeconds))
            using (var korm = new Database(database.ConnectionString, SqlServerDataHelper.ClientId))
            using (var transaction = korm.BeginTransaction())
            {
                transaction.CommandTimeout = 3;
                Action commit = () => { korm.ExecuteStoredProcedure<Object>("WaitForTwoSeconds"); };

                commit.Should().NotThrow<SqlException>();
            }
        }

        #endregion

        #region Helpers

        private void DoTestWithConnection(
            bool openConnection,
            Action<TestDatabase> testAction,
            Func<TestDatabase> createDatabaseAction)
        {
            using (var database = createDatabaseAction())
            {
                if (openConnection) database.Connection.Open();
                testAction(database);
                database.Connection.State.Should().Be(openConnection ? ConnectionState.Open : ConnectionState.Closed);
            }
        }

        private void DatabaseShouldContainInvoices(Database korm, IEnumerable<Invoice> expected)
        {
            korm.Query<Invoice>().Should().BeEquivalentTo(expected);
        }

        private void DatabaseShouldBeEmpty(TestDatabase korm)
        {
            DatabaseShouldContainInvoices(korm.ConnectionString, new List<Invoice>());
        }

        private void DatabaseShouldContainInvoices(string connectionString, IEnumerable<Invoice> expected)
        {
            using (var korm = new Database(connectionString, SqlServerDataHelper.ClientId))
            {
                DatabaseShouldContainInvoices(korm, expected);
            }
        }

        private IEnumerable<Invoice> CreateTestData() =>
            new List<Invoice>() {
                new Invoice() { Id = 1, Code = "0001", Description = "Item 1"},
                new Invoice() { Id = 2, Code = "0002", Description = "Item 2"},
                new Invoice() { Id = 3, Code = "0002", Description = "Item 3"} };

        private TestDatabase CreateDatabase() => CreateDatabase(CreateTable_TestTable) as DatabaseTestBase.TestDatabase;
        private TestDatabase CreateAndInitDatabase(string initScript)
            => CreateDatabase(CreateTable_TestTable, initScript) as DatabaseTestBase.TestDatabase;

        private void BulkInsertAddItems(IDbSet<Invoice> dbSet)
        {
            dbSet.Add(CreateTestData());
            dbSet.BulkInsert();
        }

        private void BulkInsertEnumerableItems(IDbSet<Invoice> dbSet)
        {
            dbSet.BulkInsert(CreateTestData());
        }

        [Alias("Invoices")]
        public class Invoice
        {
            public int Id { get; set; }

            public string Code { get; set; }

            public string Description { get; set; }
        }

        #endregion
    }
}
