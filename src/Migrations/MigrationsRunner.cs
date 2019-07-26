using Kros.Utils;
using System;
using System.IO;
using System.Reflection;
using System.Linq;
using Kros.KORM.Migrations.Providers;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Text;
using Kros.KORM.Query;

namespace Kros.KORM.Migrations
{
    /// <summary>
    /// Runner for execution database migrations.
    /// </summary>
    public class MigrationsRunner : IMigrationsRunner, IDisposable
    {
        private IDatabase _database;
        private readonly string _connectionString;
        private readonly bool _disposeOfDatabase;
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
            _disposeOfDatabase = false;
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
            _disposeOfDatabase = true;
        }

        private IDatabase Db
        {
            get
            {
                if (_database is null)
                {
                    _database = new Database(_connectionString);
                }
                return _database;
            }
        }

        /// <inheritdoc />
        public async Task MigrateAsync()
        {
            await InitMigrationsHistoryTable();

            var lastMigration = GetLastMigrationInfo() ?? Migration.None;
            var migrationScripts = GetMigrationScriptsToExecute(lastMigration).ToList();

            if (migrationScripts.Any())
            {
                await ExecuteMigrationScripts(migrationScripts);
            }
        }

        private async Task ExecuteMigrationScripts(IEnumerable<ScriptInfo> migrationScripts)
        {
            foreach (var scriptInfo in migrationScripts)
            {
                using (var transaction = Db.BeginTransaction())
                {
                    var script = await scriptInfo.GetScriptAsync();

                    await ExecuteMigrationScript(script);
                    await AddNewMigrationInfo(scriptInfo);

                    transaction.Commit();
                }
            }
        }

        private IEnumerable<ScriptInfo> GetMigrationScriptsToExecute(Migration lastMigration)
            => _migrationOptions.Providers.SelectMany(p => p.GetScripts())
            .OrderBy(p => p.Id)
            .Where(p => p.Id > lastMigration.MigrationId);

        private async Task AddNewMigrationInfo(ScriptInfo scriptInfo)
        {
            const string sql = "INSERT INTO [" + Migration.TableName + "] VALUES (@Id, @Name, @Info, @Updated)";

            await Db.ExecuteNonQueryAsync(
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
            get
            {
                if (_scriptLinesRegex is null)
                {
                    _scriptLinesRegex = new Regex("^GO", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                }
                return _scriptLinesRegex;
            }
        }

        private async Task ExecuteMigrationScript(string script)
        {
            string[] lines = ScriptLinesRegex.Split(script);

            foreach (string line in lines.Where(p => p.Length > 0))
            {
                await Db.ExecuteNonQueryAsync(line);
            }
        }

        private Migration GetLastMigrationInfo()
            => Db.Query<Migration>()
            .OrderByDescending(p => p.MigrationId)
            .FirstOrDefault();

        private async Task InitMigrationsHistoryTable()
        {
            var sql = $"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{Migration.TableName}' AND type = 'U')" +
                Environment.NewLine + await GetResourceContent("Kros.KORM.Resources.MigrationsHistoryTableScript.sql");

            await Db.ExecuteNonQueryAsync(sql);
        }

        private static async Task<string> GetResourceContent(string resourceFile)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceStream = assembly.GetManifestResourceStream(resourceFile);
            using (var reader = new StreamReader(resourceStream, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposeOfDatabase)
            {
                _database?.Dispose();
                _database = null;
            }
        }
    }
}
