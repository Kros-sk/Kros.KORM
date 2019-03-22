using FluentAssertions;
using Kros.KORM.Query.Providers;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Data;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Providers
{
    public class LimitOffsetDataReaderShould
    {
        #region Tests

        [Fact]
        public void ThrowCorrectExceptionsWhenInstantiatedWithIncorrectArguments()
        {
            Action action;

            action = () => new LimitOffsetDataReader(-1, 10);
            action.Should().Throw<ArgumentException>()
                .And.ParamName.Should().Be("limit", "Limit must be equal or greater than 0.");

            action = () => new LimitOffsetDataReader(10, -1);
            action.Should().Throw<ArgumentException>()
                .And.ParamName.Should().Be("offset", "Offset must be equal or greater than 0.");

            action = () => new LimitOffsetDataReader(1, 0);
            action.Should().NotThrow();

            var reader = new LimitOffsetDataReader(10, 10);
            action = () => reader.SetInnerReader(null);
            action.Should().Throw<ArgumentNullException>("Inner reader cannot be null.");

            reader = new LimitOffsetDataReader(10, 10);
            reader.SetInnerReader(CreateInnerReader());
            action = () => reader.SetInnerReader(CreateInnerReader());
            action.Should().Throw<InvalidOperationException>("Inner reader can be set only once.");
        }

        [Fact]
        public void SkipFirstRecords()
        {
            var limitOffsetReader = new LimitOffsetDataReader(100, 5);
            limitOffsetReader.SetInnerReader(CreateInnerReader());
            CheckReaderData(limitOffsetReader, 6, 7, 8, 9, 10);
        }

        [Fact]
        public void LimitRecords()
        {
            var limitOffsetReader = new LimitOffsetDataReader(4, 0);
            limitOffsetReader.SetInnerReader(CreateInnerReader());
            CheckReaderData(limitOffsetReader, 1, 2, 3, 4);
        }

        [Fact]
        public void SkipFirstRecordsAndLimitOthers()
        {
            var limitOffsetReader = new LimitOffsetDataReader(5, 2);
            limitOffsetReader.SetInnerReader(CreateInnerReader());
            CheckReaderData(limitOffsetReader, 3, 4, 5, 6, 7);
        }

        [Fact]
        public void ReturnNoRowsWhenOffsetIsTooBig()
        {
            var limitOffsetReader = new LimitOffsetDataReader(5, 20);
            limitOffsetReader.SetInnerReader(CreateInnerReader());
            CheckReaderData(limitOffsetReader, new int[] { });
        }

        [Fact]
        public void ReturnFalseImmediatelyWhenOffsetIsTooBig()
        {
            var limitOffsetReader = new LimitOffsetDataReader(10, 20);
            IDataReader innerReader = CreateInnerReader();
            limitOffsetReader.SetInnerReader(innerReader);

            limitOffsetReader.Read().Should().BeFalse();
            innerReader.Received(11).Read();

            innerReader.ClearReceivedCalls();
            limitOffsetReader.Read().Should().BeFalse();
            innerReader.Received(1).Read();
        }

        [Fact]
        public void ReturnAllRemainingRowsWhenLimitIsTooBig()
        {
            var limitOffsetReader = new LimitOffsetDataReader(50, 2);
            limitOffsetReader.SetInnerReader(CreateInnerReader());
            CheckReaderData(limitOffsetReader, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Fact]
        public void ReturnAllRemainingRowsWhenLimitIsZero()
        {
            var limitOffsetReader = new LimitOffsetDataReader(0, 2);
            limitOffsetReader.SetInnerReader(CreateInnerReader());
            CheckReaderData(limitOffsetReader, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Fact]
        public void CloseInnerReader()
        {
            var limitOffsetReader = new LimitOffsetDataReader(0, 2);
            IDataReader innerReader = Substitute.For<IDataReader>();
            limitOffsetReader.SetInnerReader(innerReader);
            limitOffsetReader.Close();

            innerReader.Received().Close();
        }

        #endregion

        #region Helpers

        private IDataReader CreateInnerReader()
        {
            int counter = 0;
            IDataReader reader = Substitute.For<IDataReader>();
            reader.Read().Returns(
                callInfo =>
                {
                    counter++;
                    return counter <= 10;
                });
            reader.GetInt32(0).ReturnsForAnyArgs(callInfo => counter);

            return reader;
        }

        private void CheckReaderData(IDataReader reader, params int[] expectedData)
        {
            var readerData = new List<int>();
            while (reader.Read())
            {
                readerData.Add(reader.GetInt32(0));
            }

            readerData.Should().BeEquivalentTo(expectedData);
        }

        #endregion
    }
}
