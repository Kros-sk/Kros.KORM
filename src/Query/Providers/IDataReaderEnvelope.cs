using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Kros.KORM.Query.Providers
{
    /// <summary>
    /// Envelope over some other inner <see cref="IDataReader"/>.
    /// Implementation can introduce own logic how to iterate over inner reader.
    /// </summary>
    public interface IDataReaderEnvelope : IDataReader
    {
        /// <summary>
        /// Sets inner reader over which is implementation iterating.
        /// </summary>
        /// <param name="innerReader">Inner reader.</param>
        void SetInnerReader(IDataReader innerReader);

        /// <summary>
        /// Asynchronously advances the reader to the next record.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>
        /// Task with result <see langword="true"/> if there are more rows, otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Default implementation calls synchronous <see cref="IDataReader.Read"/>. Override it to read
        /// the inner reader asynchronously.
        /// </remarks>
        Task<bool> ReadAsync(CancellationToken cancellationToken)
            => cancellationToken.IsCancellationRequested
                ? Task.FromCanceled<bool>(cancellationToken)
                : Task.FromResult(Read());
    }
}
