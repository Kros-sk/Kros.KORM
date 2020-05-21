﻿using Kros.KORM.Migrations.Providers;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Kros.KORM.Migrations
{
    /// <summary>
    /// Migration options.
    /// </summary>
    public class MigrationOptions
    {
        private const int DefaultTimeoutInSeconds = 30;

        private List<IMigrationScriptsProvider> _providers = new List<IMigrationScriptsProvider>();

        /// <summary>
        /// List of <see cref="IMigrationScriptsProvider"/>.
        /// </summary>
        public IEnumerable<IMigrationScriptsProvider> Providers => _providers;

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
    }
}
