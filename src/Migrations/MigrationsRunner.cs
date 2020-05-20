using Kros.KORM.Migrations.Providers;
using Kros.KORM.Query;
using Kros.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kros.KORM.Migrations
{
    /// <summary>
    /// Runner for execution database migrations.
    /// </summary>
    public class MigrationsRunner : IMigrationsRunner
    {
        #region Nested Types

        private class DatabaseHelper : IDisposable
        {
            private readonly bool _disposeOfDatabase;

            public DatabaseHelper(IDatabase database, bool disposeOfDatabase)
            {
                Database = database;
                _disposeOfDatabase = disposeOfDatabase;
            }

            public IDatabase Database { get; }

            public void Dispose()
            {
                if (_disposeOfDatabase)
                {
                    Database.Dispose();
                }
            }
        }

        #endregion

        private readonly IDatabase _database;
        private readonly string _connectionString;
        private readonly MigrationOptions _migrationOptions;

        /// <summary>
        /// Initializes new instance of <see cref="MigrationsRunner"/> with specified settings.
        /// </summary>
        /// <param name="database">Database connection. As the database is passed from outside, it <b>is not disposed of</b>,
        /// when the runner is disposed. It is the caller's responsibility to dispose of the database.</param>
        /// <param name="migrationOptions">Migration options</param>
        public MigrationsRunner(IDatabase database, MigrationOptions migrationOptions)
        {
            _database = Check.NotNull(database, nameof(database));
            _migrationOptions = Check.NotNull(migrationOptions, nameof(migrationOptions));
        }

        /// <summary>
        /// Initializes new instance of <see cref="MigrationsRunner"/> with specified settings.
        /// </summary>
        /// <param name="connectionString">Database connection string.</param>
        /// <param name="migrationOptions">Migration options.</param>
        public MigrationsRunner(string connectionString, MigrationOptions migrationOptions)
        {
            _connectionString = Check.NotNullOrWhiteSpace(connectionString, nameof(connectionString));
            _migrationOptions = Check.NotNull(migrationOptions, nameof(migrationOptions));
        }

        /// <inheritdoc />
        public async Task MigrateAsync()
        {
            using (DatabaseHelper helper = CreateDatabaseHelper())
            {
                await InitMigrationsHistoryTable(helper.Database);

                Migration lastMigration = GetLastMigrationInfo(helper.Database) ?? Migration.None;
                var migrationScripts = GetMigrationScriptsToExecute(lastMigration).ToList();

                if (migrationScripts.Any())
                {
                    await ExecuteMigrationScripts(helper.Database, migrationScripts);
                }
            }
        }

        private DatabaseHelper CreateDatabaseHelper()
            => _database is null
                ? new DatabaseHelper(new Database(_connectionString), true)
                : new DatabaseHelper(_database, false);

        private async Task ExecuteMigrationScripts(IDatabase database, IEnumerable<ScriptInfo> migrationScripts)
        {
            foreach (ScriptInfo scriptInfo in migrationScripts)
            {
                using (Data.ITransaction transaction = database.BeginTransaction())
                {
                    transaction.CommandTimeout = _migrationOptions.Timeout;
                    var script = await scriptInfo.GetScriptAsync();

                    await ExecuteMigrationScript(database, script);
                    await AddNewMigrationInfo(database, scriptInfo);

                    transaction.Commit();
                }
            }
        }

        private IEnumerable<ScriptInfo> GetMigrationScriptsToExecute(Migration lastMigration)
            => _migrationOptions.Providers.SelectMany(p => p.GetScripts())
            .OrderBy(p => p.Id)
            .Where(p => p.Id > lastMigration.MigrationId);

        private async Task AddNewMigrationInfo(IDatabase database, ScriptInfo scriptInfo)
        {
            const string sql = "INSERT INTO [" + Migration.TableName + "] VALUES (@Id, @Name, @Info, @Updated)";

            await database.ExecuteNonQueryAsync(
                sql,
                new CommandParameterCollection()
                {
                    { "@Id", scriptInfo.Id },
                    { "@Name", scriptInfo.Name },
                    { "@Info", Assembly.GetEntryAssembly().FullName },
                    { "@Updated", DateTime.Now }
                });
        }

        private static Regex _scriptLinesRegex;

        private static Regex ScriptLinesRegex
        {
            get {
                if (_scriptLinesRegex is null)
                {
                    _scriptLinesRegex = new Regex("^GO", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                }
                return _scriptLinesRegex;
            }
        }

        private async Task ExecuteMigrationScript(IDatabase database, string script)
        {
            string[] lines = ScriptLinesRegex.Split(script);

            foreach (string line in lines.Where(p => p.Length > 0))
            {
                await database.ExecuteNonQueryAsync(line);
            }
        }

        private Migration GetLastMigrationInfo(IDatabase database)
            => database.Query<Migration>()
            .OrderByDescending(p => p.MigrationId)
            .FirstOrDefault();

        private async Task InitMigrationsHistoryTable(IDatabase database)
        {
            var sql = $"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{Migration.TableName}' AND type = 'U')" +
                Environment.NewLine + await GetResourceContent("Kros.KORM.Resources.MigrationsHistoryTableScript.sql");

            await database.ExecuteNonQueryAsync(sql);
        }

        private static async Task<string> GetResourceContent(string resourceFile)
        {
            var assembly = Assembly.GetExecutingAssembly();
            Stream resourceStream = assembly.GetManifestResourceStream(resourceFile);
            using (var reader = new StreamReader(resourceStream, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }
}
