using System.Threading;
using System.Threading.Tasks;

namespace Kros.KORM.Migrations
{
    /// <summary>
    /// Interface which describe class for execution database migrations.
    /// </summary>
    public interface IMigrationsRunner
    {
        /// <summary>
        /// Executes database migrations.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        Task MigrateAsync(CancellationToken cancellationToken = default);
    }
}
