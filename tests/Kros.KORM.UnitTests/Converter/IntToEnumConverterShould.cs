using FluentAssertions;
using Kros.KORM.Converter;
using Xunit;

namespace Kros.KORM.UnitTests.Converter
{
    public class IntToEnumConverterShould
    {
        [Fact]
        public void ConvertIntToEnumValue()
        {
            var converter = new IntToEnumConverter(typeof(TestEnum));

            var actual = converter.Convert(2);

            actual.Should().Be(TestEnum.Value2);
        }

        [Fact]
        public void ConvertEnumBackToIntValue()
        {
            var converter = new IntToEnumConverter(typeof(TestEnum));

            var actual = converter.ConvertBack(TestEnum.Value3);

            actual.Should().Be(3);            
        }

        private enum TestEnum
        {
            Value1 = 1,
            Value2 = 2,
            Value3 = 3,
        }
    }
}
