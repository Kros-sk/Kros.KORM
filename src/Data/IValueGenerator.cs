using System;

namespace Kros.KORM.Data
{
    public interface IValueGenerator
    {
        object GetValue();
    }

    public interface IValueGenerator<T> : IValueGenerator
    {
        T GetValue();
    }

    public class CurrentTimeValueGenerator : IValueGenerator<DateTimeOffset>
    {
        public DateTimeOffset GetValue() => DateTimeOffset.UtcNow;

        object IValueGenerator.GetValue() => GetValue();
    }
}
