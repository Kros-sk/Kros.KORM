using FluentAssertions;
using Kros.KORM.Converter;
using Xunit;

namespace Kros.KORM.UnitTests.Converter
{
    public class TypeConverterShould
    {
        [Fact]
        public void ConvertDoubleToInt()
        {
            var converter = new TypeConverter(typeof(int), typeof(double));

            var actual = converter.Convert(156.5);

            actual.Should().Be(156);
        }

        [Fact]
        public void ConvertDecimalToDouble()
        {
            var converter = new TypeConverter(typeof(double), typeof(decimal));

            var actual = converter.Convert((decimal)156.5);

            actual.Should().Be(156.5);
        }

        [Fact]
        public void ConvertIntBackToDouble()
        {
            var converter = new TypeConverter(typeof(int), typeof(double));

            var actual = converter.ConvertBack(156);

            actual.Should().Be(156D);
        }
    }
}
