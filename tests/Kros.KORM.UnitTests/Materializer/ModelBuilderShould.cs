using Kros.KORM.Materializer;
using Kros.KORM.Metadata;
using Kros.KORM.Metadata.Attribute;
using Kros.KORM.Query.Providers;
using Kros.KORM.UnitTests.Helper;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kros.KORM.UnitTests.Materializer
{
    public class ModelBuilderShould
    {
        #region Tests

        [Fact]
        public void MaterializeDataFromReader()
        {
            using IDataReader reader = new InMemoryDataReader(CreateDataForReader());

            ModelBuilder builder = CreateBuilder();

            var data = builder.Materialize<Foo>(reader).ToList();

            Assert.Equal(2, data.Count());
            Assert.Equal(1, data[0].Id);
            Assert.Equal(new Guid("ddc995d7-4dda-41ca-abab-7f45e651784a"), data[0].PropertyGuid);

            Assert.Equal("Kitty", data[1].PropertyString);
            Assert.Equal(new DateTime(1984, 4, 20), data[1].PropertyDateTime);
        }

        [Fact]
        public void GetEmptyDataFromEmptyReader()
        {
            var sourceData = CreateDataForReader();
            sourceData.Clear();
            using IDataReader reader = new InMemoryDataReader(sourceData);

            ModelBuilder builder = CreateBuilder();

            var data = builder.Materialize<Foo>(reader).ToList();

            Assert.Empty(data);
        }

        [Fact]
        public void MaterializeDataFromDataTable()
        {
            var dataTable = CreateDataTable();
            ModelBuilder builder = CreateBuilder();

            var data = builder.Materialize<Foo>(dataTable).ToList();

            Assert.Equal(3, data.Count());
            Assert.Equal(1, data[0].Id);
            Assert.Equal(new DateTime(1990, 1, 1), data[0].PropertyDateTime);

            Assert.Equal(TestEnum.Value3, data[1].PropertyEnum);
            Assert.Equal(new DateTime(1975, 10, 11), data[1].PropertyDateTime);

            Assert.Equal(12, data[2].Id);
            Assert.Null(data[2].PropertyString);
        }

        [Fact]
        public void CallDisposeOnInternalReaderAndCloseConnection()
        {
            IDbConnection connection = Substitute.For<IDbConnection>();
            IDataReader internalReader = Substitute.For<IDataReader>();
            IDbCommand command = Substitute.For<IDbCommand>();
            command.Connection = connection;
            command.ExecuteReader().Returns(internalReader);

            ModelBuilder.QueryDataReader reader = new ModelBuilder.QueryDataReader(command, null, true);
            ModelBuilder builder = CreateBuilder();
            var data = builder.Materialize<Foo>(reader).ToList();

            internalReader.Received().Dispose();
            connection.Received().Close();
        }

        [Fact]
        public void NotCallDisposeMethodOnReader()
        {
            IDataReader reader = Substitute.For<IDataReader>();

            ModelBuilder builder = CreateBuilder();
            var data = builder.Materialize<Foo>(reader).ToList();

            reader.DidNotReceive().Dispose();
        }

        [Fact]
        public async Task MaterializeDataFromReaderAsync()
        {
            using var reader = new InMemoryDbDataReader(CreateDataForReader());

            ModelBuilder builder = CreateBuilder();

            var data = await builder.MaterializeAsync<Foo>(reader).ToListAsync(TestContext.Current.CancellationToken);

            Assert.Equal(2, data.Count);
            Assert.Equal(1, data[0].Id);
            Assert.Equal(new Guid("ddc995d7-4dda-41ca-abab-7f45e651784a"), data[0].PropertyGuid);

            Assert.Equal("Kitty", data[1].PropertyString);
            Assert.Equal(new DateTime(1984, 4, 20), data[1].PropertyDateTime);
            Assert.Equal(3, reader.ReadAsyncCallCount);
        }

        [Fact]
        public async Task MaterializeAsyncFallsBackToSyncReadForPlainDataReader()
        {
            using IDataReader reader = new InMemoryDataReader(CreateDataForReader());

            ModelBuilder builder = CreateBuilder();

            var data = await builder.MaterializeAsync<Foo>(reader).ToListAsync(TestContext.Current.CancellationToken);

            Assert.Equal(2, data.Count);
            Assert.Equal(1, data[0].Id);
            Assert.Equal(new Guid("ddc995d7-4dda-41ca-abab-7f45e651784a"), data[0].PropertyGuid);

            Assert.Equal("Kitty", data[1].PropertyString);
            Assert.Equal(new DateTime(1984, 4, 20), data[1].PropertyDateTime);
        }

        [Fact]
        public async Task MaterializeAsyncThrowsWhenTokenIsAlreadyCancelled()
        {
            using var reader = new InMemoryDbDataReader(CreateDataForReader());
            ModelBuilder builder = CreateBuilder();
            await using IAsyncEnumerator<Foo> enumerator = builder
                .MaterializeAsync<Foo>(reader)
                .GetAsyncEnumerator(new CancellationToken(canceled: true));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await enumerator.MoveNextAsync());

            Assert.Equal(0, reader.ReadAsyncCallCount);
        }

        [Fact]
        public async Task MaterializeAsyncThrowsOnSecondEnumeration()
        {
            using var reader = new InMemoryDbDataReader(CreateDataForReader());
            ModelBuilder builder = CreateBuilder();
            IAsyncEnumerable<Foo> data = builder.MaterializeAsync<Foo>(reader);

            await data.ToListAsync(TestContext.Current.CancellationToken);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await data.ToListAsync(TestContext.Current.CancellationToken));
            Assert.Equal("The asynchronous sequence can be enumerated only once.", ex.Message);
        }

        [Fact]
        public async Task CallDisposeOnInternalReaderAndCloseConnectionAsync()
        {
            DbConnection connection = Substitute.For<DbConnection>();
            var internalReader = new InMemoryDbDataReader(CreateDataForReader());
            var command = new FakeDbCommand(connection, internalReader);

            ModelBuilder.QueryDataReader reader = await ModelBuilder.QueryDataReader.CreateAsync(
                command, null, true, TestContext.Current.CancellationToken);
            ModelBuilder builder = CreateBuilder();
            var data = await builder.MaterializeAsync<Foo>(reader).ToListAsync(TestContext.Current.CancellationToken);

            Assert.Equal(2, data.Count);
            Assert.True(command.ExecuteReaderAsyncCalled);
            Assert.True(internalReader.IsDisposed);
            connection.Received().Close();
        }

        [Fact]
        public async Task CreateAsyncReadsThroughEnvelope()
        {
            DbConnection connection = Substitute.For<DbConnection>();
            var internalReader = new InMemoryDbDataReader(CreateDataForReader());
            var command = new FakeDbCommand(connection, internalReader);

            ModelBuilder.QueryDataReader reader = await ModelBuilder.QueryDataReader.CreateAsync(
                command, new LimitOffsetDataReader(1, 1), true, TestContext.Current.CancellationToken);
            ModelBuilder builder = CreateBuilder();
            var data = await builder.MaterializeAsync<Foo>(reader).ToListAsync(TestContext.Current.CancellationToken);

            Foo item = Assert.Single(data);
            Assert.Equal(2, item.Id);
            Assert.Equal("Kitty", item.PropertyString);
            Assert.Equal(2, internalReader.ReadAsyncCallCount);
        }

        [Fact]
        public async Task NotCallDisposeMethodOnReaderAsync()
        {
            DbDataReader reader = Substitute.For<DbDataReader>();

            ModelBuilder builder = CreateBuilder();
            await builder.MaterializeAsync<Foo>(reader).ToListAsync(TestContext.Current.CancellationToken);

            reader.DidNotReceive().Dispose();
        }

        [Fact]
        public void MaterializeDataFromDataRow()
        {
            var dataTable = CreateDataTable();
            ModelBuilder builder = CreateBuilder();

            var data = builder.Materialize<Foo>(dataTable.Rows[0]);

            Assert.Equal(1, data.Id);
            Assert.Equal(new DateTime(1990, 1, 1), data.PropertyDateTime);
        }

        #endregion

        private static ModelBuilder CreateBuilder()
        {
            return new ModelBuilder(new DynamicMethodModelFactory(new DatabaseMapper(new ConventionModelMapper())));
        }

        #region Creating reader data

        private List<Dictionary<string, object>> CreateDataForReader()
        {
            List<Dictionary<string, object>> ret = new List<Dictionary<string, object>>();

            AddRow(ret, 1, "Hello", 45.78, (decimal)785.78, new DateTime(1980, 7, 24),
                true, new Guid("ddc995d7-4dda-41ca-abab-7f45e651784a"));
            AddRow(ret, 2, "Kitty", 47.98, (decimal)75.8, new DateTime(1984, 4, 20),
                true, new Guid("ddc995d7-4dda-41ca-abab-7f45e6517844"));

            return ret;
        }

        private static void AddRow(List<Dictionary<string, object>> ret,
                                                                int id,
                                                             string firstName,
                                                             double something,
                                                            decimal salary,
                                                           DateTime birthday,
                                                               bool iS,
                                                               Guid guid)
        {
            Dictionary<string, object> row = new Dictionary<string, object>() { { "Id", id },
                                                                                { "FirstName", firstName },
                                                                                { "Something", something},
                                                                                { "Salary",salary},
                                                                                { "Birthday", birthday},
                                                                                { "Is", iS},
                                                                                { "PropertyGuid", guid}};

            ret.Add(row);
        }

        #endregion

        #region Creating test datatable

        private DataTable CreateDataTable()
        {
            var ret = new DataTable();

            ret.Columns.Add("Id", typeof(int));
            ret.Columns.Add("FirstName", typeof(string));
            ret.Columns.Add("Birthday", typeof(DateTime));
            ret.Columns.Add("PropertyEnum", typeof(TestEnum));

            AddRow(ret, 1, "Michael", new DateTime(1990, 1, 1), TestEnum.Value2);
            AddRow(ret, 4, "Peter", new DateTime(1975, 10, 11), TestEnum.Value3);
            AddRow(ret, 12, DBNull.Value, new DateTime(1987, 9, 9), TestEnum.Value1);

            return ret;
        }

        private void AddRow(DataTable table, int id, object firstName, DateTime birthday, TestEnum propertyEnum)
        {
            var row = table.NewRow();

            row["Id"] = id;
            row["FirstName"] = firstName;
            row["birthday"] = birthday;
            row["PropertyEnum"] = propertyEnum;

            table.Rows.Add(row);
        }

        #endregion

        #region Test classes

        private class Foo
        {
            public int Id { get; set; }

            [Alias("FirstName")]
            public string PropertyString { get; set; }

            [Alias("Something")]
            public double PropertyDouble { get; set; }

            [Alias("Salary")]
            public decimal PropertyDecimal { get; set; }

            [Alias("Birthday")]
            public DateTime PropertyDateTime { get; set; }

            public bool Is { get; set; }

            [NoMap]
            public int Bar { get; set; }

            public Guid PropertyGuid { get; set; }

            public TestEnum PropertyEnum { get; set; }
        }

        private enum TestEnum
        {
            Value1 = 1,
            Value2 = 2,
            Value3 = 3,
        }

        #endregion
    }
}
