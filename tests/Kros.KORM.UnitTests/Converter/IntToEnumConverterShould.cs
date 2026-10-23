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

            Assert.Equal(TestEnum.Value2, actual);
        }

        [Fact]
        public void ConvertEnumBackToIntValue()
        {
            var converter = new IntToEnumConverter(typeof(TestEnum));

            var actual = converter.ConvertBack(TestEnum.Value3);

            Assert.Equal(3, actual);
        }

        private enum TestEnum
        {
            Value1 = 1,
            Value2 = 2,
            Value3 = 3,
        }
    }
}
