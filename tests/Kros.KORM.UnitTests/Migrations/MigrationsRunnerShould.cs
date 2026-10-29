using Kros.Data;
using Kros.KORM.Migrations;
using Kros.KORM.Migrations.Providers;
using Kros.UnitTests;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kros.KORM.UnitTests.Migrations
{
    [Collection(KormTestsCollection.Name)]
    public class MigrationsRunnerShould(KormTestsFixture kormContext) : SqlServerDatabaseTestBase
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

        private readonly KormTestsFixture _kormContext = kormContext;

        protected override string BaseConnectionString => _kormContext.GetConnectionString();

        [Fact]
        public async Task ExecuteInitialMigration()
        {
            var runner = CreateMigrationsRunner(nameof(ExecuteInitialMigration));
            await runner.MigrateAsync(TestContext.Current.CancellationToken);

            TableShouldExist("__KormMigrationsHistory");
            TableShouldExist("People");

            DatabaseVersionShouldBe(20190301001);
        }

        [Fact]
        public async Task MigrateToLastVersion()
        {
            var runner = CreateMigrationsRunner(nameof(MigrateToLastVersion));
            InitDatabase();

            await runner.MigrateAsync(TestContext.Current.CancellationToken);

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

            await runner.MigrateAsync(TestContext.Current.CancellationToken);

            ColumnInViewShouldExist("PeopleView", "Age");
            TableShouldExist("Roles");
        }

        [Fact]
        public async Task MigrateAsync_ThrowsWhenTokenAlreadyCancelled()
        {
            var runner = CreateMigrationsRunner(nameof(ExecuteInitialMigration));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => runner.MigrateAsync(new CancellationToken(canceled: true)));

            TableShouldNotExist(Migration.TableName);
            TableShouldNotExist("People");
        }

        [Fact]
        public async Task MigrateAsync_CancelledBetweenScripts_DoesNotRunRemainingScripts()
        {
            using var cts = new CancellationTokenSource();
            var innerProvider = new AssemblyMigrationScriptsProvider(
                Assembly.GetExecutingAssembly(),
                $"Kros.KORM.UnitTests.Resources.ScriptsForRunner.{nameof(MigrateToLastVersion)}");
            var provider = new CancellingScriptsProvider(innerProvider, cts, cancelAfterScriptNumber: 2);
            var options = new MigrationOptions();
            options.AddScriptsProvider(provider);
            var runner = new MigrationsRunner(ServerHelper.Connection.ConnectionString, options);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runner.MigrateAsync(cts.Token));

            MigrationsHistoryShouldContainOnly(20190228001);
            TableShouldNotExist("People");
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
                Assert.Equal(1, (int)cmd.ExecuteScalar());
            });
        }

        private void TableShouldNotExist(string tableName)
        {
            ExecuteCommand((cmd) =>
            {
                cmd.CommandText = $"SELECT Count(*) FROM sys.tables WHERE name = '{tableName}' AND type = 'U'";
                Assert.Equal(0, (int)cmd.ExecuteScalar());
            });
        }

        private void MigrationsHistoryShouldContainOnly(long migrationId)
        {
            ExecuteCommand((cmd) =>
            {
                cmd.CommandText = "SELECT Count(*) FROM __KormMigrationsHistory";
                Assert.Equal(1, (int)cmd.ExecuteScalar());
                cmd.CommandText = "SELECT MigrationId FROM __KormMigrationsHistory";
                Assert.Equal(migrationId, (long)cmd.ExecuteScalar());
            });
        }

        private void ColumnInViewShouldExist(string viewName, string columnName)
        {
            ExecuteCommand((cmd) =>
            {
                cmd.CommandText = $"SELECT Count(*) FROM sys.columns WHERE object_id = OBJECT_ID('{viewName}') AND name = '{columnName}'";
                Assert.Equal(1, (int)cmd.ExecuteScalar());
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
                Assert.Equal(databaseVersion, (long)cmd.ExecuteScalar());
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

                options.AddAfterMigrationAction(async (db, id, cancellationToken) =>
                {
                    if (id <= 20250101001)
                    {
                        await db.ExecuteNonQueryAsync("CREATE TABLE Departments (Id int);", cancellationToken);
                    }

                    if (id <= 20990101001)
                    {
                        await db.ExecuteNonQueryAsync("CREATE TABLE Roles (Id int);", cancellationToken);
                    }
                });
            }

            return new MigrationsRunner(ServerHelper.Connection.ConnectionString, options);
        }

        /// <summary>
        /// Wraps another provider and cancels <paramref name="cts"/> right after returning the content
        /// of the script number <paramref name="cancelAfterScriptNumber"/> (1-based).
        /// </summary>
        private sealed class CancellingScriptsProvider(
            IMigrationScriptsProvider inner,
            CancellationTokenSource cts,
            int cancelAfterScriptNumber) : IMigrationScriptsProvider
        {
            private int _returnedScripts;

            // Script infos must point to this provider, so that ScriptInfo.GetScriptAsync calls back here.
            public IEnumerable<ScriptInfo> GetScripts()
                => inner.GetScripts()
                    .Select(s => new ScriptInfo(this) { Id = s.Id, Name = s.Name, Path = s.Path })
                    .ToList();

            public async Task<string> GetScriptAsync(ScriptInfo scriptInfo, CancellationToken cancellationToken = default)
            {
                string script = await inner.GetScriptAsync(scriptInfo, cancellationToken);
                if (++_returnedScripts == cancelAfterScriptNumber)
                {
                    cts.Cancel();
                }
                return script;
            }
        }
    }
}
