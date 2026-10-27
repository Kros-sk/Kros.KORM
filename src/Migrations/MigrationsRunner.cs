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
using System.Threading;
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
        public async Task MigrateAsync(CancellationToken cancellationToken = default)
        {
            using (DatabaseHelper helper = CreateDatabaseHelper())
            {
                await InitMigrationsHistoryTable(helper.Database, cancellationToken);

                Migration lastMigration =
                    await GetLastMigrationInfoAsync(helper.Database, cancellationToken) ?? Migration.None;
                var migrationScripts = GetMigrationScriptsToExecute(lastMigration).ToList();

                if (migrationScripts.Any())
                {
                    await ExecuteMigrationScripts(helper.Database, migrationScripts, cancellationToken);
                    long maxScriptId = migrationScripts.Max(s => s.Id);

                    foreach (var action in _migrationOptions.Actions)
                    {
                        await action(helper.Database, maxScriptId, cancellationToken);
                    }
                }
            }
        }

        private DatabaseHelper CreateDatabaseHelper()
            => _database is null
                ? new DatabaseHelper(new Database(_connectionString), true)
                : new DatabaseHelper(_database, false);

        private async Task ExecuteMigrationScripts(
            IDatabase database,
            IEnumerable<ScriptInfo> migrationScripts,
            CancellationToken cancellationToken)
        {
            foreach (ScriptInfo scriptInfo in migrationScripts)
            {
                using (Data.ITransaction transaction = await database.BeginTransactionAsync(cancellationToken))
                {
                    var script = await scriptInfo.GetScriptAsync(cancellationToken);
                    transaction.CommandTimeout = (int)_migrationOptions.Timeout.TotalSeconds;

                    await ExecuteMigrationScript(database, script, cancellationToken);
                    await AddNewMigrationInfo(database, scriptInfo, cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                }
            }
        }

        private IEnumerable<ScriptInfo> GetMigrationScriptsToExecute(Migration lastMigration)
            => _migrationOptions.Providers.SelectMany(p => p.GetScripts())
            .OrderBy(p => p.Id)
            .Where(p => p.Id > lastMigration.MigrationId);

        private async Task AddNewMigrationInfo(
            IDatabase database,
            ScriptInfo scriptInfo,
            CancellationToken cancellationToken)
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
                },
                cancellationToken);
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

        private async Task ExecuteMigrationScript(
            IDatabase database,
            string script,
            CancellationToken cancellationToken)
        {
            string[] lines = ScriptLinesRegex.Split(script);

            foreach (string line in lines.Where(p => p.Length > 0))
            {
                await database.ExecuteNonQueryAsync(line, cancellationToken);
            }
        }

        private Task<Migration> GetLastMigrationInfoAsync(IDatabase database, CancellationToken cancellationToken)
            => database.Query<Migration>()
            .OrderByDescending(p => p.MigrationId)
            .FirstOrDefaultAsync(cancellationToken);

        private async Task InitMigrationsHistoryTable(IDatabase database, CancellationToken cancellationToken)
        {
            var sql = $"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{Migration.TableName}' AND type = 'U')" +
                Environment.NewLine +
                await GetResourceContent("Kros.KORM.Resources.MigrationsHistoryTableScript.sql", cancellationToken);

            await database.ExecuteNonQueryAsync(sql, cancellationToken);
        }

        private static async Task<string> GetResourceContent(string resourceFile, CancellationToken cancellationToken)
        {
            var assembly = Assembly.GetExecutingAssembly();
            Stream resourceStream = assembly.GetManifestResourceStream(resourceFile);
            using (var reader = new StreamReader(resourceStream, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync(cancellationToken);
            }
        }
    }
}
