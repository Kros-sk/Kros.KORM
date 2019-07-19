using System;

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
    }

    public interface IValueGenerator<T> : IValueGenerator
    {
        T GetValue();
    }

    //public class CurrentTimeValueGenerator : IValueGenerator<DateTimeOffset>
    //{
    //    public DateTimeOffset GetValue() => DateTimeOffset.UtcNow;

    //    object IValueGenerator.GetValue() => GetValue();
    //}
}
