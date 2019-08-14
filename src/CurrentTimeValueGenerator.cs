using System;

namespace Kros.KORM
{
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
