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
            Assert.Throws<ArgumentException>(action);
        }

        [Theory]
        [MemberData(nameof(DataConvertNullValuesIsTrue))]
        public void ConvertValuesWhenConvertNullValuesIsTrue(object value, object expected, string because)
        {
            var converter = new NullAndTrimStringConverter(true, false);
            Assert.Equal(expected, converter.ConvertBack(value));
        }

        public static IEnumerable<object[]> DataConvertNullValuesIsTrue()
        {
            yield return new object[] { null, string.Empty, "Input is null." };
            yield return new object[] { DBNull.Value, string.Empty, "Input is DBNull.Value." };
            yield return new object[] { "  \t ", "  \t ", "Input is whitespace string a trimming is off." };
            yield return new object[] { "lorem ipsum", "lorem ipsum", "Input is string." };
            yield return new object[] { 123, 123, "Input is not string." };
        }

        [Theory]
        [MemberData(nameof(DataTrimStringValuesIsTrue))]
        public void ConvertValuesWhenTrimStringValueIsTrue(object value, object expected, string because)
        {
            var converter = new NullAndTrimStringConverter(false, true);
            Assert.Equal(expected, converter.ConvertBack(value));
        }

        public static IEnumerable<object[]> DataTrimStringValuesIsTrue()
        {
            yield return new object[] { null, null, "Input is null and null converting is off." };
            yield return new object[] { DBNull.Value, DBNull.Value, "Input is DBNull.Value and null converting is off." };
            yield return new object[] { "  \t ", string.Empty, "Input is whitespace string." };
            yield return new object[] { " lorem ipsum \t", "lorem ipsum", "Input is string with whitespaces at the ends." };
            yield return new object[] { "lorem ipsum", "lorem ipsum", "Input is string." };
            yield return new object[] { 123, 123, "Input is not string." };
        }

        [Theory]
        [MemberData(nameof(DataConvertNullValuesAndTrimStringValueAreTrue))]
        public void ConvertValuesWhenConvertNullValuesAndTrimStringValueAreTrue(object value, object expected, string because)
        {
            var converter = new NullAndTrimStringConverter(true, true);
            Assert.Equal(expected, converter.ConvertBack(value));
        }

        public static IEnumerable<object[]> DataConvertNullValuesAndTrimStringValueAreTrue()
        {
            yield return new object[] { null, string.Empty, "Input is null." };
            yield return new object[] { DBNull.Value, string.Empty, "Input is DBNull.Value." };
            yield return new object[] { "  \t ", string.Empty, "Input is whitespace string." };
            yield return new object[] { " lorem ipsum \t", "lorem ipsum", "Input is string with whitespaces at the ends." };
            yield return new object[] { "lorem ipsum", "lorem ipsum", "Input is string." };
            yield return new object[] { 123, 123, "Input is not string." };
        }
    }
}
