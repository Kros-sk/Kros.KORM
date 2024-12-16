using Kros.KORM.Migrations.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Kros.KORM.Migrations
{
    /// <summary>
    /// Migration options.
    /// </summary>
    public class MigrationOptions
    {
        private const int DefaultTimeoutInSeconds = 30;
        private const string DefaultResourceNamespace = "SqlScripts.PostMigrationScripts";
        private const string DefaultRefreshViewsScriptName = "RefreshViews.sql";

        private List<IMigrationScriptsProvider> _providers = new List<IMigrationScriptsProvider>();
        private List<Func<IDatabase, Task>> _actions = new List<Func<IDatabase, Task>>();

        /// <summary>
        /// List of <see cref="IMigrationScriptsProvider"/>.
        /// </summary>
        public IEnumerable<IMigrationScriptsProvider> Providers => _providers;


        /// <summary>
        /// List of actions to be executed on database after migration scripts.
        /// </summary>
        public IEnumerable<Func<IDatabase, Task>> Actions => _actions;

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
        /// Add action to be executed on database after migration scripts.
        /// </summary>
        /// <param name="action"></param>
        public void AddAction(Func<IDatabase, Task> action)
            => _actions.Add(action);

        /// <summary>
        /// Refresh all views in database after migration scripts.
        /// Script for refreshing views is loaded from assembly. Default script name is 'RefreshViews.sql'.
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="scriptName"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void RefreshViews(Assembly assembly, string scriptName = DefaultRefreshViewsScriptName)
        {
            var resourceName = $"{assembly.GetName().Name}.{DefaultResourceNamespace}.{scriptName}";
            AddAction(async (database) =>
            {
                using Stream stream = assembly.GetManifestResourceStream(resourceName)
                    ?? throw new InvalidOperationException($"Resource '{resourceName}' not found.");
                using StreamReader reader = new StreamReader(stream);
                string script = await reader.ReadToEndAsync();
                await database.ExecuteNonQueryAsync(script);
            });
        }
    }
}
