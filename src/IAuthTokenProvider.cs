namespace Kros.KORM
{
    /// <summary>
    /// Support for token-based authentication for SQL Server.
    /// </summary>
    public interface IAuthTokenProvider
    {
        /// <summary>
        /// Returns authentication token, or <see langword="null" /> value, if token can not be obtained.
        /// </summary>
        /// <returns>Authentication token.</returns>
        string GetToken();
    }
}
