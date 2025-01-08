using FluentAssertions;
using Kros.Data;
using Kros.KORM.Migrations;
using Kros.KORM.UnitTests.Integration;
using Kros.UnitTests;
using Microsoft.Data.SqlClient;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Kros.KORM.UnitTests.Migrations
{
    public class MigrationsRunnerShould : SqlServerDatabaseTestBase
    {
        #region Sql Scripts

        private readonly static string CreateTable_MigrationHistory =
$@"CREATE TABLE [dbo].[__KormMigrationsHistory](
    [MigrationId] [bigint] NOT NULL,
    [MigrationName] [nvarchar](255) NOT NULL,
    [ProductInfo] [nvarchar](255) NOT NULL,
    [Updated] [datetime2] NULL,
    CONSTRAINT [PK_MigrationHistory] PRIMARY KEY CLUSTERED
    (
        [MigrationId] ASC
    )
) ON [PRIMARY]
";
        private readonly static string CreateTable_People =
$@"CREATE TABLE [dbo].[People](
    [Id] [int] NOT NULL,
    [Name] [nvarchar](255) NOT NULL
    CONSTRAINT [PK_People] PRIMARY KEY CLUSTERED
    (
        [Id] ASC
    )
) ON [PRIMARY]
";
        private readonly static string InsertIntoMigrationHistory =
$@"INSERT INTO __KormMigrationsHistory VALUES (20190228001, 'Old', 'FromUnitTests', '20190228')
INSERT INTO __KormMigrationsHistory VALUES (20190228002, 'Old', 'FromUnitTests', '20190228')
INSERT INTO __KormMigrationsHistory VALUES (20190301001, 'InitDatabase', 'FromUnitTests', '20190301')";

        private readonly static string CreateView_People =
$@"CREATE VIEW PeopleView AS
SELECT *
FROM dbo.People";
        #endregion

        protected override string BaseConnectionString => IntegrationTestConfig.ConnectionString;

        [Fact]
        public async Task ExecuteInitialMigration()
        {
            var runner = CreateMigrationsRunner(nameof(ExecuteInitialMigration));
            await runner.MigrateAsync();

            TableShouldExist("__KormMigrationsHistory");
            TableShouldExist("People");

            DatabaseVersionShouldBe(20190301001);
        }

        [Fact]
        public async Task MigrateToLastVersion()
        {
            var runner = CreateMigrationsRunner(nameof(MigrateToLastVersion));
            InitDatabase();

            await runner.MigrateAsync();

            TableShouldExist("People");
            TableShouldExist("Projects");
            TableShouldExist("ProjectDetails");
            TableShouldExist("Contacts");

            DatabaseVersionShouldBe(20190301003);
        }

        [Fact]
        public async Task MigrateWithActions()
        {
            var runner = CreateMigrationsRunner(nameof(MigrateWithActions), true);
            InitDatabase();

            await runner.MigrateAsync();
           
            ColumnInViewShouldExist("PeopleView", "Age");
            TableShouldExist("Roles");
        }

        private void InitDatabase()
        {
            ExecuteCommand((cmd) =>
            {
                foreach (var script in new[] {
                    CreateTable_MigrationHistory ,
                    CreateTable_People,
                    InsertIntoMigrationHistory,
                    CreateView_People
                })
                {
                    cmd.CommandText = script;
                    cmd.ExecuteScalar();
                }
            });
        }

        private void TableShouldExist(string tableName)
        {
            ExecuteCommand((cmd) =>
            {
                cmd.CommandText = $"SELECT Count(*) FROM sys.tables WHERE name = '{tableName}' AND type = 'U'";
                ((int)cmd.ExecuteScalar())
                    .Should().Be(1);
            });
        }

        private void ColumnInViewShouldExist(string viewName, string columnName)
        {
            ExecuteCommand((cmd) =>
            {
                cmd.CommandText = $"SELECT Count(*) FROM sys.columns WHERE object_id = OBJECT_ID('{viewName}') AND name = '{columnName}'";
                ((int)cmd.ExecuteScalar())
                    .Should().Be(1);
            });
        }

        private void ExecuteCommand(Action<SqlCommand> action)
        {
            using (ConnectionHelper.OpenConnection(ServerHelper.Connection))
            using (var cmd = ServerHelper.Connection.CreateCommand())
            {
                action(cmd);
            }
        }

        private void DatabaseVersionShouldBe(long databaseVersion)
        {
            ExecuteCommand((cmd) =>
            {
                cmd.CommandText = $"SELECT TOP 1 MigrationId FROM __KormMigrationsHistory ORDER BY MigrationId DESC";
                ((long)cmd.ExecuteScalar())
                    .Should().Be(databaseVersion);
            });
        }

        private MigrationsRunner CreateMigrationsRunner(string folderName, bool migrateWithActions = false)
        {
            var options = new MigrationOptions();
            options.AddAssemblyScriptsProvider(
                Assembly.GetExecutingAssembly(),
                $"Kros.KORM.UnitTests.Resources.ScriptsForRunner.{folderName}");

            if (migrateWithActions)
            {
                options.AddRefreshViewsAction();

                options.AddAfterMigrationAction(async (db, id) =>
                {
                    if (id <= 20250101001)
                    {
                        db.ExecuteNonQuery("CREATE TABLE Departments (Id int);");
                    }

                    if (id <= 20990101001)
                    {
                        db.ExecuteNonQuery("CREATE TABLE Roles (Id int);");
                    }
                    await Task.CompletedTask;
                });
            }

            return new MigrationsRunner(ServerHelper.Connection.ConnectionString, options);
        }
    }
}
