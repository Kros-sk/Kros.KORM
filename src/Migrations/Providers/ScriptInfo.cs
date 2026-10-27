using Kros.Utils;
using System.Threading;
using System.Threading.Tasks;

namespace Kros.KORM.Migrations.Providers
{
    /// <summary>
    /// Information about migration script.
    /// </summary>
    public class ScriptInfo
    {
        private readonly IMigrationScriptsProvider _provider;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="provider">Migration scripts provider.</param>
        public ScriptInfo(IMigrationScriptsProvider provider)
        {
            _provider = Check.NotNull(provider, nameof(provider));
        }

        /// <summary>
        /// Migration Id.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Name of migration script.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Path to migration script.
        /// </summary>
        public string Path { get; set; }

        /// <inheritdoc/>
        public override string ToString() => $"{Id}_{Name}";

        /// <summary>
        /// Get script content.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>Script content.</returns>
        public Task<string> GetScriptAsync(CancellationToken cancellationToken = default)
            => _provider.GetScriptAsync(this, cancellationToken);
    }
}
