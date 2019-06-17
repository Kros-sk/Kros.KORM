using System;

namespace Kros.KORM.Converter
{
    internal class NoConverter : IConverter
    {
        public static IConverter Instance { get; } = new NoConverter();

        public object Convert(object value) => throw new NotImplementedException();
        public object ConvertBack(object value) => throw new NotImplementedException();
    }
}
