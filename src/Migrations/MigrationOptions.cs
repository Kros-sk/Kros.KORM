using Kros.KORM.Migrations.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Kros.KORM.Migrations
{
    /// <summary>
    /// Migration options.
    /// </summary>
    public class MigrationOptions
    {
        private const int DefaultTimeoutInSeconds = 30;
        private const string DefaultResourceNamespace = "Resources";
        private const string DefaultRefreshViewsScriptName = "RefreshViews.sql";

        private List<IMigrationScriptsProvider> _providers = [];
        private List<Func<IDatabase, long, Task>> _actions = [];

        /// <summary>
        /// List of <see cref="IMigrationScriptsProvider"/>.
        /// </summary>
        public IEnumerable<IMigrationScriptsProvider> Providers => _providers;


        /// <summary>
        /// List of actions to be executed on database after migration scripts are executed.
        /// </summary>
        public IEnumerable<Func<IDatabase, long, Task>> Actions => _actions;

        /// <summary>
        /// Timeout for the migration script command.
        /// If not set, default value 30s will be used.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(DefaultTimeoutInSeconds);

        /// <summary>
        /// Register new <see cref="IMigrationScriptsProvider"/>.
        /// </summary>
        /// <param name="provider">Migration scripts provider.</param>
        public void AddScriptsProvider(IMigrationScriptsProvider provider)
            => _providers.Add(provider);

        /// <summary>
        /// Register new <see cref="AssemblyMigrationScriptsProvider"/>.
        /// </summary>
        /// <param name="assembly">Assembly, which contains embedded script resources.</param>
        /// <param name="resourceNamespace">Full namespace, where are placed embedded scripts.</param>
        public void AddAssemblyScriptsProvider(Assembly assembly, string resourceNamespace)
            => AddScriptsProvider(new AssemblyMigrationScriptsProvider(assembly, resourceNamespace));

        /// <summary>
        /// Register new <see cref="FileMigrationScriptsProvider"/>.
        /// </summary>
        /// <param name="folderPath">Path to folder where migration scripts are stored.</param>
        public void AddFileScriptsProvider(string folderPath)
            => AddScriptsProvider(new FileMigrationScriptsProvider(folderPath));

        /// <summary>
        /// Add action to be executed on database after migration scripts are executed.
        /// </summary>
        /// <param name="actionToExecute"></param>
        public void AddAfterMigrationAction(Func<IDatabase, long, Task> actionToExecute)
        {
            _actions.Add(actionToExecute);
        }

        /// <summary>
        /// Add action of refreshing all database views.
        /// </summary>
        /// <param name="scriptName">Name of the script containing query for refreshing all views. Default one is Kros.KORM\Resources\RefreshViews.sql </param>
        public void AddRefreshViewsAction(string scriptName = DefaultRefreshViewsScriptName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"{assembly.GetName().Name}.{DefaultResourceNamespace}.{scriptName}";
            AddAfterMigrationAction(async (database, _) =>
            {
                Stream resourceStream = assembly.GetManifestResourceStream(resourceName);
                using var reader = new StreamReader(resourceStream, Encoding.UTF8);
                string script = await reader.ReadToEndAsync();
                await database.ExecuteNonQueryAsync(script);
            });
        }
    }
}
