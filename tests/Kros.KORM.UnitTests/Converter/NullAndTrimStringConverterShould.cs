using Kros.KORM.Converter;
using System;
using System.Collections.Generic;
using Xunit;

namespace Kros.KORM.UnitTests.Converter
{
    public class NullAndTrimStringConverterShould
    {
        [Fact]
        public void NotCreateUselessInstance()
        {
            Action action = () => new NullAndTrimStringConverter(false, false);
            Assert.Throws<ArgumentException>(() => action());
        }

        [Theory]
        [MemberData(nameof(DataConvertNullValuesIsTrue))]
        public void ConvertValuesWhenConvertNullValuesIsTrue(object value, object expected)
        {
            var converter = new NullAndTrimStringConverter(true, false);
            Assert.Equal(expected, converter.ConvertBack(value));
        }

        public static IEnumerable<object[]> DataConvertNullValuesIsTrue()
        {
            yield return new object[] { null, string.Empty };
            yield return new object[] { DBNull.Value, string.Empty };
            yield return new object[] { "  \t ", "  \t " };
            yield return new object[] { "lorem ipsum", "lorem ipsum" };
            yield return new object[] { 123, 123 };
        }

        [Theory]
        [MemberData(nameof(DataTrimStringValuesIsTrue))]
        public void ConvertValuesWhenTrimStringValueIsTrue(object value, object expected)
        {
            var converter = new NullAndTrimStringConverter(false, true);
            Assert.Equal(expected, converter.ConvertBack(value));
        }

        public static IEnumerable<object[]> DataTrimStringValuesIsTrue()
        {
            yield return new object[] { null, null };
            yield return new object[] { DBNull.Value, DBNull.Value };
            yield return new object[] { "  \t ", string.Empty };
            yield return new object[] { " lorem ipsum \t", "lorem ipsum" };
            yield return new object[] { "lorem ipsum", "lorem ipsum" };
            yield return new object[] { 123, 123 };
        }

        [Theory]
        [MemberData(nameof(DataConvertNullValuesAndTrimStringValueAreTrue))]
        public void ConvertValuesWhenConvertNullValuesAndTrimStringValueAreTrue(object value, object expected)
        {
            var converter = new NullAndTrimStringConverter(true, true);
            Assert.Equal(expected, converter.ConvertBack(value));
        }

        public static IEnumerable<object[]> DataConvertNullValuesAndTrimStringValueAreTrue()
        {
            yield return new object[] { null, string.Empty };
            yield return new object[] { DBNull.Value, string.Empty };
            yield return new object[] { "  \t ", string.Empty };
            yield return new object[] { " lorem ipsum \t", "lorem ipsum" };
            yield return new object[] { "lorem ipsum", "lorem ipsum" };
            yield return new object[] { 123, 123 };
        }
    }
}
