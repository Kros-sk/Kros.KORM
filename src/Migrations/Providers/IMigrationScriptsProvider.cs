using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kros.KORM.Migrations.Providers
{
    /// <summary>
    /// Interface which describe provider for loading migration scripts.
    /// </summary>
    public interface IMigrationScriptsProvider
    {
        /// <summary>
        /// Get script infos list.
        /// </summary>
        /// <returns>Script infos.</returns>
        IEnumerable<ScriptInfo> GetScripts();

        /// <summary>
        /// Get script content.
        /// </summary>
        /// <param name="scriptInfo">Script info.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>Script content.</returns>
        Task<string> GetScriptAsync(ScriptInfo scriptInfo, CancellationToken cancellationToken = default);
    }
}
