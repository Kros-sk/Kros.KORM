namespace Kros.KORM
{
    /// <summary>
    /// Interface for column value generator.
    /// </summary>
    public interface IValueGenerator
    {
        /// <summary>
        /// Gets value.
        /// </summary>
        object GetValue();
    }
}
