using System.Collections.Generic;

namespace Kros.KORM.Data
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

        /// <summary>
        /// Supported command types.
        /// </summary>
        IEnumerable<DbCommandType> SupportedCommandTypes { get; }
    }

    public interface IValueGenerator<T> : IValueGenerator
    {
        T GetValue();

        IEnumerable<DbCommandType> SupportedCommandTypes { get; }
    }

    //public class CurrentTimeValueGenerator : IValueGenerator<DateTimeOffset>
    //{
    //    public DateTimeOffset GetValue() => DateTimeOffset.UtcNow;

    //    object IValueGenerator.GetValue() => GetValue();
    //}

    /// <summary>
    /// Types of database command.
    /// </summary>
    public enum DbCommandType
    {
        /// <summary>
        /// None.
        /// </summary>
        None,

        /// <summary>
        /// Insert command.
        /// </summary>
        Insert,

        /// <summary>
        /// Update command.
        /// </summary>
        Update,

        /// <summary>
        /// Delete command.
        /// </summary>
        Delete
    }
}
