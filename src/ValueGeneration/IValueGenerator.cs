using System;

namespace Kros.KORM.ValueGeneration
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

    /// <summary>
    /// Current time value generator.
    /// </summary>
    /// <remarks>
    /// Generator generates date and time that are set to the current Coordinated Universal Time (UTC).
    /// </remarks>
    public class CurrentTimeValueGenerator : IValueGenerator
    {
        /// <inheritdoc />
        public object GetValue() => DateTimeOffset.UtcNow;
    }
}
