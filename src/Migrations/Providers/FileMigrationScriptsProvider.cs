using Kros.Utils;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Kros.KORM.Migrations.Providers
{
    /// <summary>
    /// Migration scripts provider, which load scripts from disk.
    /// </summary>
    public class FileMigrationScriptsProvider : IMigrationScriptsProvider
    {
        private readonly string _folderPath;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="folderPath">Path to folder with migration scripts.</param>
        public FileMigrationScriptsProvider(string folderPath)
        {
            _folderPath = Check.NotNullOrWhiteSpace(folderPath, nameof(folderPath));
        }

        /// <inheritdoc/>
        public async Task<string> GetScriptAsync(ScriptInfo scriptInfo)
            => await Task.FromResult(File.ReadAllText(scriptInfo.Path));

        /// <inheritdoc/>
        public IEnumerable<ScriptInfo> GetScripts()
            => this.GetScripts(Directory.GetFiles(_folderPath), _folderPath);
    }
}