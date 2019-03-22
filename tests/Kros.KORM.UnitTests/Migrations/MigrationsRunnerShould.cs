using FluentAssertions;
using Kros.Data;
using Kros.KORM.Migrations;
using Kros.KORM.UnitTests.Integration;
using Kros.UnitTests;
using System;
using System.Data.SqlClient;
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

        #endregion

        protected override string BaseConnectionString
            => IntegrationTestConfig.ConnectionString;

        [Fact]
        public async Task ExecuteInitialMigration()
        {
            using (var helper = CreateHelper(nameof(ExecuteInitialMigration)))
            {
                await helper.Runner.MigrateAsync();

                TableShouldExist("__KormMigrationsHistory");
                TableShouldExist("People");

                DatabaseVersionShouldBe(20190301001);
            }
        }

        [Fact]
        public async Task MigrateToLastVersion()
        {
            using (var helper = CreateHelper(nameof(MigrateToLastVersion)))
            {
                InitDatabase();

                await helper.Runner.MigrateAsync();

                TableShouldExist("People");
                TableShouldExist("Projects");
                TableShouldExist("ProjectDetails");
                TableShouldExist("Contacts");

                DatabaseVersionShouldBe(20190301003);
            }
        }

        private void InitDatabase()
        {
            ExecuteCommand((cmd) =>
            {
                foreach (var script in new[] {
                    CreateTable_MigrationHistory ,
                    CreateTable_People,
                    InsertIntoMigrationHistory })
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

        private Helper CreateHelper(string folderName)
        {
            return new Helper(new Database(ServerHelper.Connection), folderName);
        }

        private class Helper : IDisposable
        {
            private readonly IDatabase _database;

            public Helper(Database database, string folderName)
            {
                _database = database;
                var options = new MigrationOptions();

                options.AddAssemblyScriptsProvider(
                    Assembly.GetExecutingAssembly(),
                    $"Kros.KORM.UnitTests.Resources.ScriptsForRunner.{folderName}");

                Runner = new MigrationsRunner(_database, options);
            }

            public MigrationsRunner Runner { get; }

            public void Dispose()
            {
                _database.Dispose();
            }
        }
    }
}
