using System.Data;

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
    }
}
