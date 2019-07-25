namespace Kros.KORM
{
    /// <summary>
    /// Extension methods for <see cref="IDatabaseBuilder"/>.
    /// </summary>
    public static class IDatabaseBuilderExtensions
    {
        /// <summary>
        /// Use <paramref name="connectionString"/> which instance of <see cref="IDatabase"/> will use for accessing to database.
        /// </summary>
        /// <param name="builder">Databse builder.</param>
        /// <param name="connectionString">Connection string.</param>
        /// <returns>Database builder.</returns>
        public static IDatabaseBuilder UseConnection(this IDatabaseBuilder builder, string connectionString)
            => builder.UseConnection(KormConnectionSettings.Parse(connectionString));
    }
}
