namespace Kros.KORM.Data
{
    /// <summary>
    /// Interface for extended value generator.
    /// </summary>
    public interface IValueGeneratorEx
    {
        /// <summary>
        /// Gets value.
        /// </summary>
        /// <param name="database"></param>
        object GetValue(IDatabase database);
    }
}
