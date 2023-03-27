using Castle.Core.Internal;
using FluentAssertions;
using Kros.Extensions;
using Kros.KORM.Data;
using Kros.KORM.UnitTests.Helper;
using System;
using System.Collections.Generic;
using Xunit;

namespace Kros.KORM.UnitTests.Data
{
    public class DataReaderExtensionsShould
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(null)]
        public void GetNullableBoolean(bool? value)
        {
            using InMemoryDataReader dataReader = CreateReader(value);

            dataReader.GetNullableBoolean(0).Should().Be(value);
        }

        [Theory]
        [InlineData((byte)0)]
        [InlineData((byte)1)]
        [InlineData(null)]
        public void GetNullableByte(byte? value)
        {
            using InMemoryDataReader dataReader = CreateReader(value);

            dataReader.GetNullableByte(0).Should().Be(value);
        }

        [Theory]
        [InlineData('g')]
        [InlineData('*')]
        [InlineData(null)]
        public void GetNullableChar(char? value)
        {
            using InMemoryDataReader dataReader = CreateReader(value);

            dataReader.GetNullableChar(0).Should().Be(value);
        }

        [Theory]
        [InlineData("2021-04-05")]
        [InlineData("2020-03-06")]
        [InlineData(null)]
        public void GetNullableDateTime(string value)
        {
            DateTime? dateTime = value.IsNullOrEmpty() ? null : value.ParseDateTime();
            using InMemoryDataReader dataReader = CreateReader(dateTime);

            dataReader.GetNullableDateTime(0).Should().Be(dateTime);
        }

        [Theory]
        [InlineData(0.45)]
        [InlineData(22.0)]
        [InlineData(null)]
        public void GetNullableDecimal(double? value)
        {
            decimal? d = (decimal?)value;
            using InMemoryDataReader dataReader = CreateReader(d);

            dataReader.GetNullableDecimal(0).Should().Be(d);
        }

        [Theory]
        [InlineData(11.0)]
        [InlineData(0.785)]
        [InlineData(null)]
        public void GetNullableDouble(double? value)
        {
            using InMemoryDataReader dataReader = CreateReader(value);

            dataReader.GetNullableDouble(0).Should().Be(value);
        }

        [Theory]
        [InlineData("{3D6F4D25-60E8-432B-B6A7-3ADDBD331812}")]
        [InlineData("{39AFEC5B-A784-45B5-9233-6488DC1BFB1D}")]
        [InlineData(null)]
        public void GetNullableGuid(string value)
        {
            Guid? g = value.IsNullOrEmpty() ? null : new Guid(value);
            using InMemoryDataReader dataReader = CreateReader(g);

            dataReader.GetNullableGuid(0).Should().Be(g);
        }

        [Theory]
        [InlineData((short)11)]
        [InlineData((short)33)]
        [InlineData(null)]
        public void GetNullableInt16(short? value)
        {
            using InMemoryDataReader dataReader = CreateReader(value);

            dataReader.GetNullableInt16(0).Should().Be(value);
        }

        [Theory]
        [InlineData(22)]
        [InlineData(44)]
        [InlineData(null)]
        public void GetNullableInt32(int? value)
        {
            using InMemoryDataReader dataReader = CreateReader(value);

            dataReader.GetNullableInt32(0).Should().Be(value);
        }

        [Theory]
        [InlineData(1L)]
        [InlineData(125L)]
        [InlineData(null)]
        public void GetNullableInt64(long? value)
        {
            using InMemoryDataReader dataReader = CreateReader(value);

            dataReader.GetNullableInt64(0).Should().Be(value);
        }

        [Theory]
        [InlineData(1.0F)]
        [InlineData(125.0F)]
        [InlineData(null)]
        public void GetNullableFloat(float? value)
        {
            using InMemoryDataReader dataReader = CreateReader(value);

            dataReader.GetNullableFloat(0).Should().Be(value);
        }

        private static InMemoryDataReader CreateReader<T>(T value)
        {
            var dataReader = new InMemoryDataReader(
                new Dictionary<string, object>[] { new() { { "value", value } } }, new Type[] { typeof(T) });
            dataReader.Read();

            return dataReader;
        }
    }
}
