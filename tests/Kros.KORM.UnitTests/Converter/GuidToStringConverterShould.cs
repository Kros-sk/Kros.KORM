using Kros.KORM.Converter;
using System;
using Xunit;

namespace Kros.KORM.UnitTests.Converter
{
    public class GuidToStringConverterShould
    {
        private const string TEST_GUID = "{f6f266d3-8bb5-41ca-bb93-8b668e83fa2e}";

        [Fact]
        public void ConvertGuidToString()
        {
            var converter = new GuidToStringConverter();
            var actual = converter.Convert(Guid.Parse(TEST_GUID));

            Assert.Equal(TEST_GUID, actual);
        }

        [Fact]
        public void ConvertStringBackToGuid()
        {
            var converter = new GuidToStringConverter();
            var actual = converter.ConvertBack(TEST_GUID);

            Assert.Equal(new Guid(TEST_GUID), actual);
        }
    }
}
