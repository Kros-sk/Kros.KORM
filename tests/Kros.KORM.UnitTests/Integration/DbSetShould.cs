using FluentAssertions;
using Kros.Data.SqlServer;
using Kros.KORM.Converter;
using Kros.KORM.Metadata;
using Kros.KORM.Metadata.Attribute;
using Kros.KORM.Query;
using Kros.KORM.UnitTests.Base;
using Kros.KORM.UnitTests.Properties;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Kros.KORM.UnitTests.Integration
{
    public class DbSetShould : DatabaseTestBase
    {
        #region Nested Classes

        [Alias("LimitOffsetTest")]
        private class LimitOffsetTestData
        {
            public int Id { get; set; }
            public string Value { get; set; }
        }

        [Alias("DataTypesTest")]
        private class DataTypesData
        {
            public int Id { get; set; }
            public string ColNote { get; set; }
            public byte ColByte { get; set; }
            public int ColInt32 { get; set; }
            public long ColInt64 { get; set; }
            public float ColSingle { get; set; }
            public double ColDouble { get; set; }
            public decimal ColDecimal { get; set; }
            public decimal ColCurrency { get; set; }
            public DateTime ColDate { get; set; }
            public DateTime ColDateTime { get; set; }
            public DateTime ColDateTime2 { get; set; }
            public DateTimeOffset ColDateTimeOffset { get; set; }
            public DateTime ColSmallDateTime { get; set; }
            public Guid ColGuid { get; set; }
            public bool ColBool { get; set; }
            public string ColShortText { get; set; }
            public string ColLongText { get; set; }
            public string ColNVarcharMax { get; set; }
        }

        [Alias(Table_TestTable)]
        public class Person
        {
            [Key(AutoIncrementMethodType.Custom)]
            public int Id { get; set; }

            public int Age { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }

            [Converter(typeof(AddressConverter))]
            public List<string> Address { get; set; }

            public bool IsDeleted { get; set; }

            public string TestLongText { get; set; }
        }

        [Alias(Table_TestTable)]
        private class Foo
        {
            [Key(AutoIncrementMethodType.None)]
            public int Id { get; set; }
        }

        private class AddressConverter : IConverter
        {
            public object Convert(object value) =>
                value != null ? value.ToString().Split('#').ToList() : new List<string>();

            public object ConvertBack(object value) =>
                value is List<string> address && address.Count > 0 ? string.Join("#", address) : null;
        }

        #endregion

        #region SQL Scripts

        private const string Table_DataTypes = "DataTypesTest";

        private static readonly string CreateTable_DataTypes =
$@"CREATE TABLE[dbo].[{Table_DataTypes}] (
    [Id] [int] NOT NULL,
    [ColNote] [nvarchar](255) NULL,
    [ColByte] [tinyint] NULL,
    [ColInt32] [int] NULL,
    [ColInt64] [bigint] NULL,
    [ColSingle] [real] NULL,
    [ColDouble] [float] NULL,
    [ColDecimal] [decimal](18, 5) NULL,
    [ColCurrency] [money] NULL,
    [ColDate] [date] NULL,
    [ColDateTime] [datetime] NULL,
    [ColDateTime2] [datetime2](7) NULL,
    [ColDateTimeOffset] [datetimeoffset] NULL,
    [ColSmallDateTime] [smalldatetime] NULL,
    [ColGuid] [uniqueidentifier] NULL,
    [ColBool] [bit] NULL,
    [ColShortText] [nvarchar](20) NULL,
    [ColLongText] [ntext] NULL,
    [ColNVarcharMax] [nvarchar](max) NULL,

    CONSTRAINT [PK_TestTable] PRIMARY KEY CLUSTERED ([Id] ASC) ON [PRIMARY]

) ON [PRIMARY];";

        private const string Table_TestTable = "People";

        private static readonly string CreateTable_TestTable =
$@"CREATE TABLE [dbo].[{Table_TestTable}] (
    [Id] [int] NOT NULL,
    [Age] [int] NULL,
    [FirstName] [nvarchar](50) NULL,
    [LastName] [nvarchar](50) NULL,
    [Address] [nvarchar](50) NULL,
    [IsDeleted] [bit] NOT NULL DEFAULT 0,
    [TestLongText] [nvarchar](max) NULL
) ON [PRIMARY];";

        private static readonly string InsertDataScript =
$@"INSERT INTO {Table_TestTable} VALUES (1, 18, 'John', 'Smith', 'London', 0, 'Lorem ipsum dolor sit amet 1.');
INSERT INTO {Table_TestTable} VALUES (2, 22, 'Kilie', 'Bistrol', 'London', 1, 'Lorem ipsum dolor sit amet 2.');";

        private const string Table_LimitOffsetTest = "LimitOffsetTest";

        private static readonly string CreateTable_LimitOffsetTest =
$@"CREATE TABLE [dbo].[{Table_LimitOffsetTest}] (
    [Id] [int] NOT NULL,
    [Value] [nvarchar](50) NULL
) ON [PRIMARY];";

        private static readonly string InsertLimitOffsetDataScript =
$@"INSERT INTO [{Table_LimitOffsetTest}] VALUES (1, 'one');
INSERT INTO [{Table_LimitOffsetTest}] VALUES (2, 'two');
INSERT INTO [{Table_LimitOffsetTest}] VALUES (3, 'three');
INSERT INTO [{Table_LimitOffsetTest}] VALUES (4, 'four');
INSERT INTO [{Table_LimitOffsetTest}] VALUES (5, 'fice');
INSERT INTO [{Table_LimitOffsetTest}] VALUES (6, 'six');
INSERT INTO [{Table_LimitOffsetTest}] VALUES (7, 'seven');
INSERT INTO [{Table_LimitOffsetTest}] VALUES (8, 'eight');
INSERT INTO [{Table_LimitOffsetTest}] VALUES (9, 'nine');
INSERT INTO [{Table_LimitOffsetTest}] VALUES (10, 'ten');
INSERT INTO [{Table_LimitOffsetTest}] VALUES (11, 'eleven');
INSERT INTO [{Table_LimitOffsetTest}] VALUES (12, 'twelve');
INSERT INTO [{Table_LimitOffsetTest}] VALUES (13, 'thirteen');
INSERT INTO [{Table_LimitOffsetTest}] VALUES (14, 'fourteen');
INSERT INTO [{Table_LimitOffsetTest}] VALUES (15, 'fifteen');
INSERT INTO [{Table_LimitOffsetTest}] VALUES (16, 'sixteen');
INSERT INTO [{Table_LimitOffsetTest}] VALUES (17, 'seventeen');
INSERT INTO [{Table_LimitOffsetTest}] VALUES (18, 'eighteen');
INSERT INTO [{Table_LimitOffsetTest}] VALUES (19, 'nineteen');
INSERT INTO [{Table_LimitOffsetTest}] VALUES (20, 'twenty');";

        #endregion

        #region Insert Data

        [Fact]
        public void InsertData()
        {
            InsertDataCore();
        }

        [Fact]
        public void InsertDataSynchronouslyWithoutDeadLock()
        {
            AsyncContext.Run(() =>
            {
                InsertDataCore();
            });
        }

        [Fact]
        public async Task InsertDataAsync()
        {
            using (var korm = CreateDatabase(CreateTable_DataTypes))
            {
                IDbSet<DataTypesData> dbSet = GetDbSetForCommitInsert(korm);
                await dbSet.CommitChangesAsync();
                AssertDataTypesData(korm);
            }
        }

        private void InsertDataCore()
        {
            using (var korm = CreateDatabase(CreateTable_DataTypes))
            {
                IDbSet<DataTypesData> dbSet = GetDbSetForCommitInsert(korm);
                dbSet.CommitChanges();
                AssertDataTypesData(korm);
            }
        }

        private static IDbSet<DataTypesData> GetDbSetForCommitInsert(IDatabase korm)
        {
            var dbSet = korm.Query<DataTypesData>().AsDbSet();

            dbSet.Add(GetDataTypesData());
            return dbSet;
        }

        #endregion

        #region Update data

        [Fact]
        public void UpdateData()
        {
            UpdateDataCore();
        }

        [Fact]
        public void UpdateDataSynchronouslyWithoutDeadLock()
        {
            AsyncContext.Run(() =>
            {
                UpdateDataCore();
            });
        }

        [Fact]
        public async Task UpdateDataAsync()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var dbSet = GetDbSetForUpdate(korm);

                await dbSet.CommitChangesAsync();

                AssertData(korm);
            }
        }

        private void UpdateDataCore()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var dbSet = GetDbSetForUpdate(korm);

                dbSet.CommitChanges();

                AssertData(korm);
            }
        }

        private static IDbSet<Person> GetDbSetForUpdate(IDatabase korm)
        {
            var dbSet = korm.Query<Person>().AsDbSet();

            dbSet.Edit(GetPersonData());
            return dbSet;
        }

        #endregion

        #region Delete Data

        private class DeleteItem
        {
            public int Value { get; set; }
            public int GetId() => 1;
            public DeleteSubItem Sub1 { get; } = new DeleteSubItem();
        }

        private class DeleteSubItem
        {
            public DeleteSubSubItem Sub2 { get; } = new DeleteSubSubItem();
        }

        private class DeleteSubSubItem
        {
            public int Value { get; set; }
            public int GetId() => 1;
            public int GetId(int value1, int value2) => value1 + value2;
        }

        private int DeleteGetId() => 1;

        private int DeleteGetId(int p1, int p2) => p1 + p2;

        [Fact]
        public void DeleteData()
        {
            DeleteDataCore();
        }

        [Fact]
        public void DeleteDataSynchronouslyWithoutDeadLock()
        {
            AsyncContext.Run(() =>
            {
                DeleteDataCore();
            });
        }

        [Fact]
        public async Task DeleteDataAsync()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var dbSet = korm.Query<Person>().AsDbSet();
                dbSet.Delete(new List<Person>() {
                    new Person() { Id = 1 },
                    new Person() { Id = 2 } });

                await dbSet.CommitChangesAsync();

                korm.Query<Person>().Count().Should().Be(0);
            }
        }

        [Fact]
        public async Task DeleteDataByIdAsync()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var dbSet = korm.Query<Person>().AsDbSet();
                dbSet.Delete(1);

                await dbSet.CommitChangesAsync();

                korm.Query<Person>()
                    .Should()
                    .NotContain(p => p.Id == 1);
            }
        }

        [Fact]
        public async Task DeleteDataByIdUsingVariableAsync()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var dbSet = korm.Query<Person>().AsDbSet();
                int tmp = 1;
                dbSet.Delete(tmp);

                await dbSet.CommitChangesAsync();

                korm.Query<Person>()
                    .Should()
                    .NotContain(p => p.Id == 1);
            }
        }

        [Fact]
        public async Task DeleteDataByIdsAsync()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var dbSet = korm.Query<Person>().AsDbSet();
                dbSet.Delete(1);
                dbSet.Delete(2);

                await dbSet.CommitChangesAsync();

                korm.Query<Person>().Should().BeEmpty();
            }
        }

        [Fact]
        public async Task DeleteDataByLinqConditionAsync()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var dbSet = korm.Query<Person>().AsDbSet();
                dbSet.Delete(p => p.Id == 1);

                await dbSet.CommitChangesAsync();

                korm.Query<Person>()
                    .Should()
                    .NotContain(p => p.Id == 1);
            }
        }

        [Fact]
        public async Task DeleteDataByLinqConditionUsingVariableAsync()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var dbSet = korm.Query<Person>().AsDbSet();
                int tmp = 1;
                dbSet.Delete(p => p.Id == tmp);

                await dbSet.CommitChangesAsync();

                korm.Query<Person>()
                    .Should()
                    .NotContain(p => p.Id == 1);
            }
        }

        [Fact]
        public async Task DeleteDataByLinqConditionUsingPropertyAsync()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var dbSet = korm.Query<Person>().AsDbSet();
                var item = new DeleteItem
                {
                    Value = 1
                };
                dbSet.Delete(p => p.Id == item.Value);

                await dbSet.CommitChangesAsync();

                korm.Query<Person>()
                    .Should()
                    .NotContain(p => p.Id == 1);
            }
        }

        [Fact]
        public async Task DeleteDataByLinqConditionUsingNestedPropertyAsync()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var dbSet = korm.Query<Person>().AsDbSet();
                var item = new DeleteItem();
                item.Sub1.Sub2.Value = 1;
                dbSet.Delete(p => p.Id == item.Sub1.Sub2.Value);

                await dbSet.CommitChangesAsync();

                korm.Query<Person>()
                    .Should()
                    .NotContain(p => p.Id == 1);
            }
        }

        [Fact]
        public async Task DeleteDataByLinqConditionUsingMethodAsync()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var dbSet = korm.Query<Person>().AsDbSet();
                dbSet.Delete(p => p.Id == DeleteGetId(0, DeleteGetId()));

                await dbSet.CommitChangesAsync();

                korm.Query<Person>()
                    .Should()
                    .NotContain(p => p.Id == 1);
            }
        }

        [Fact]
        public async Task DeleteDataByLinqConditionUsingMethodInAnotherClassAsync()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var dbSet = korm.Query<Person>().AsDbSet();
                var item = new DeleteItem();
                dbSet.Delete(p => p.Id == item.GetId());

                await dbSet.CommitChangesAsync();

                korm.Query<Person>()
                    .Should()
                    .NotContain(p => p.Id == 1);
            }
        }

        [Fact]
        public async Task DeleteDataByLinqConditionUsingSubMethodInAnotherClassAsync()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var dbSet = korm.Query<Person>().AsDbSet();
                var item = new DeleteItem();
                dbSet.Delete(p => p.Id == item.Sub1.Sub2.GetId(0, item.Sub1.Sub2.GetId()));

                await dbSet.CommitChangesAsync();

                korm.Query<Person>()
                    .Should()
                    .NotContain(p => p.Id == 1);
            }
        }

        [Fact]
        public async Task DeleteDataByComplexLinqConditionAsync()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var dbSet = korm.Query<Person>().AsDbSet();
                dbSet.Delete(p => p.Age >= 18 && p.Age < 20);

                await dbSet.CommitChangesAsync();

                korm.Query<Person>()
                    .Should()
                    .NotContain(p => p.Id == 1);
            }
        }

        [Fact]
        public async Task DeleteDataByConditionAsync()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var dbSet = korm.Query<Person>().AsDbSet();
                dbSet.Delete("Id = @1", 2);

                await dbSet.CommitChangesAsync();

                korm.Query<Person>()
                    .Should()
                    .NotContain(p => p.Id == 2);
            }
        }

        [Theory]
        [MemberData(nameof(DeleteDataByBooleanData))]
        public async Task DeleteDataByBooleanAsync(Expression<Func<Person, bool>> predicate, int deletedId)
        {
            using (var korm = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var dbSet = korm.Query<Person>().AsDbSet();
                dbSet.Delete(predicate);
                await dbSet.CommitChangesAsync();

                korm.Query<Person>()
                    .Should()
                    .HaveCount(1)
                    .And.NotContain(p => p.Id == deletedId);
            }
        }

        public static IEnumerable<object[]> DeleteDataByBooleanData()
        {
            yield return new object[] { (Expression<Func<Person, bool>>)(p => p.IsDeleted), 2 };
            yield return new object[] { (Expression<Func<Person, bool>>)(p => !p.IsDeleted), 1 };
            yield return new object[] { (Expression<Func<Person, bool>>)(p => (p.IsDeleted == true) && p.IsDeleted), 2 };
            yield return new object[] { (Expression<Func<Person, bool>>)(p => (p.IsDeleted == false) || !p.IsDeleted), 1 };
            yield return new object[] { (Expression<Func<Person, bool>>)(p => p.IsDeleted || (p.IsDeleted == true)), 2 };
            yield return new object[] { (Expression<Func<Person, bool>>)(p => !p.IsDeleted && (p.IsDeleted == false)), 1 };
            yield return new object[] { (Expression<Func<Person, bool>>)(p => (p.IsDeleted == false) && !p.IsDeleted), 1 };
            yield return new object[] { (Expression<Func<Person, bool>>)(p => (p.IsDeleted == true) && p.IsDeleted), 2 };
        }

        private void DeleteDataCore()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var dbSet = korm.Query<Person>().AsDbSet();
                dbSet.Delete(new List<Person>() {
                    new Person() { Id = 1 },
                    new Person() { Id = 2 } });

                dbSet.CommitChanges();

                korm.Query<Person>().Should().BeEmpty();
            }
        }

        #endregion

        #region Bulk Insert

        [Fact]
        public async Task BulkInsertDataAsync()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable))
            {
                IDbSet<Person> dbSet = korm.Query<Person>().AsDbSet();
                dbSet.Add(GetPersonData());

                await dbSet.BulkInsertAsync();

                AssertData(korm);
            }
        }

        [Fact]
        public async Task BulkInsertDataDirectlyAsync()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable))
            {
                IDbSet<Person> dbSet = korm.Query<Person>().AsDbSet();

                await dbSet.BulkInsertAsync(GetPersonData());

                AssertData(korm);
            }
        }

        [Fact]
        public void BulkInsertDataSynchronouslyWithoutDeadLock()
        {
            AsyncContext.Run(() =>
            {
                using (var korm = CreateDatabase(CreateTable_TestTable))
                {
                    IDbSet<Person> dbSet = korm.Query<Person>().AsDbSet();
                    dbSet.Add(GetPersonData());

                    dbSet.BulkInsert();

                    AssertData(korm);
                }
            });
        }

        [Fact]
        public void BulkInsertDataDirectlySynchronouslyWithoutDeadLock()
        {
            AsyncContext.Run(() =>
            {
                using (var korm = CreateDatabase(CreateTable_TestTable))
                {
                    IDbSet<Person> dbSet = korm.Query<Person>().AsDbSet();

                    dbSet.BulkInsert(GetPersonData());

                    AssertData(korm);
                }
            });
        }

        #endregion

        #region Bulk Update

        [Fact]
        public void BulkUpdateDataSynchronouslyWithoutDeadLock()
        {
            AsyncContext.Run(() =>
            {
                using (var korm = CreateDatabase(CreateTable_TestTable, InsertDataScript))
                {
                    var dbSet = korm.Query<Person>().AsDbSet();

                    dbSet.Edit(GetPersonData());

                    dbSet.BulkUpdate();

                    AssertData(korm);
                }
            });
        }

        [Fact]
        public async Task BulkUpdateDataAsync()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var dbSet = korm.Query<Person>().AsDbSet();

                dbSet.Edit(GetPersonData());

                await dbSet.BulkUpdateAsync();

                AssertData(korm);
            }
        }

        [Fact]
        public void BulkUpdateDataWithActionSynchronouslyWithoutDeadLock()
        {
            AsyncContext.Run(() =>
            {
                using (var korm = CreateDatabase(CreateTable_TestTable, InsertDataScript))
                {
                    var dbSet = korm.Query<Person>().AsDbSet();

                    dbSet.Edit(GetPersonData());

                    dbSet.BulkUpdate((c, t, s) => { });

                    AssertData(korm);
                }
            });
        }

        [Fact]
        public async Task BulkUpdateDataWithActionAsync()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var dbSet = korm.Query<Person>().AsDbSet();

                dbSet.Edit(GetPersonData());

                await dbSet.BulkUpdateAsync((c, t, s) => { });

                AssertData(korm);
            }
        }

        [Fact]
        public void BulkUpdateDataDirectlySynchronouslyWithoutDeadLock()
        {
            AsyncContext.Run(() =>
            {
                using (var korm = CreateDatabase(CreateTable_TestTable, InsertDataScript))
                {
                    var dbSet = korm.Query<Person>().AsDbSet();

                    dbSet.BulkUpdate(GetPersonData());

                    AssertData(korm);
                }
            });
        }

        [Fact]
        public async Task BulkUpdateDataDirectlyAsync()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var dbSet = korm.Query<Person>().AsDbSet();

                await dbSet.BulkUpdateAsync(GetPersonData());

                AssertData(korm);
            }
        }

        [Fact]
        public void BulkUpdateDataWithActionDirectlySynchronouslyWithoutDeadLock()
        {
            AsyncContext.Run(() =>
            {
                using (var korm = CreateDatabase(CreateTable_TestTable, InsertDataScript))
                {
                    var dbSet = korm.Query<Person>().AsDbSet();

                    dbSet.BulkUpdate(GetPersonData(), (c, t, s) => { });

                    AssertData(korm);
                }
            });
        }

        [Fact]
        public async Task BulkUpdateDataWithActionDirectlyAsync()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var dbSet = korm.Query<Person>().AsDbSet();

                await dbSet.BulkUpdateAsync(GetPersonData(), (c, t, s) => { });

                AssertData(korm);
            }
        }

        #endregion

        #region Limit/Offset

        [Fact]
        public void ReturnOnlyFirstNRows()
        {
            using (IDatabase korm = CreateDatabase(CreateTable_LimitOffsetTest, InsertLimitOffsetDataScript))
            {
                var expectedData = new List<LimitOffsetTestData>(new[] {
                    new LimitOffsetTestData() { Id = 1, Value = "one" },
                    new LimitOffsetTestData() { Id = 2, Value = "two" },
                    new LimitOffsetTestData() { Id = 3, Value = "three" }
                });

                List<LimitOffsetTestData> data = korm.Query<LimitOffsetTestData>()
                    .OrderBy(item => item.Id)
                    .Take(3)
                    .ToList();

                data.Should().BeEquivalentTo(expectedData);
            }
        }

        [Fact]
        public void SkipFirstNRows()
        {
            using (IDatabase korm = CreateDatabase(CreateTable_LimitOffsetTest, InsertLimitOffsetDataScript))
            {
                var expectedData = new List<LimitOffsetTestData>(new[] {
                    new LimitOffsetTestData() { Id = 18, Value = "eighteen" },
                    new LimitOffsetTestData() { Id = 19, Value = "nineteen" },
                    new LimitOffsetTestData() { Id = 20, Value = "twenty" }
                });

                List<LimitOffsetTestData> data = korm.Query<LimitOffsetTestData>()
                    .OrderBy(item => item.Id)
                    .Skip(17)
                    .ToList();

                data.Should().BeEquivalentTo(expectedData);
            }
        }

        [Fact]
        public void SkipFirstNRowsAndReturnNextMRows()
        {
            using (IDatabase korm = CreateDatabase(CreateTable_LimitOffsetTest, InsertLimitOffsetDataScript))
            {
                var expectedData = new List<LimitOffsetTestData>(new[] {
                    new LimitOffsetTestData() { Id = 6, Value = "six" },
                    new LimitOffsetTestData() { Id = 7, Value = "seven" },
                    new LimitOffsetTestData() { Id = 8, Value = "eight" }
                });

                List<LimitOffsetTestData> data = korm.Query<LimitOffsetTestData>()
                    .OrderBy(item => item.Id)
                    .Skip(5)
                    .Take(3)
                    .ToList();

                data.Should().BeEquivalentTo(expectedData);
            }
        }

        [Fact]
        public void ReturnNoRowsWhenSkipIsTooBig()
        {
            using (IDatabase korm = CreateDatabase(CreateTable_LimitOffsetTest, InsertLimitOffsetDataScript))
            {
                var expectedData = new List<LimitOffsetTestData>();

                List<LimitOffsetTestData> data = korm.Query<LimitOffsetTestData>()
                    .OrderBy(item => item.Id)
                    .Skip(100)
                    .ToList();

                data.Should().BeEquivalentTo(expectedData);
            }
        }

        [Fact]
        public void ReturnAllRemainigRowsWhenTakeIsTooBig()
        {
            using (IDatabase korm = CreateDatabase(CreateTable_LimitOffsetTest, InsertLimitOffsetDataScript))
            {
                var expectedData = new List<LimitOffsetTestData>(new[] {
                    new LimitOffsetTestData() { Id = 19, Value = "nineteen" },
                    new LimitOffsetTestData() { Id = 20, Value = "twenty" },
                });

                List<LimitOffsetTestData> data = korm.Query<LimitOffsetTestData>()
                    .OrderBy(item => item.Id)
                    .Skip(18)
                    .Take(100)
                    .ToList();

                data.Should().BeEquivalentTo(expectedData);
            }
        }

        #endregion

        #region Helpers

        private TestDatabase CreateTestDatabase()
        {
            var (_, _, tableScript, procedureScript) = SqlServerIntIdGenerator.GetSqlInfo();
            return CreateDatabase(new[] { CreateTable_TestTable, tableScript, procedureScript });
        }

        private static IEnumerable<DataTypesData> GetDataTypesData()
        {
            yield return CreateRecord(1);
            yield return CreateRecord(2);
            yield return CreateRecord(3);
            yield return CreateRecord(4);
            yield return CreateRecord(5);
        }

        private static DataTypesData CreateRecord(int id)
        {
            return new DataTypesData()
            {
                Id = id,
                ColNote = "Record " + id.ToString(),
                ColByte = (byte)id,
                ColInt32 = id * 100,
                ColInt64 = id * 100000000000,
                ColSingle = id * (float)100000000000.12345,
                ColDouble = id * 100000000000.12345,
                ColDecimal = id * (decimal)100000000000.12345,
                ColCurrency = id * (decimal)100000000000.12345,
                ColDate = new DateTime(1978, 12, id),
                ColDateTime = new DateTime(1978, 12, id, 10, 11, 22),
                ColDateTime2 = new DateTime(1978, 12, id, 10, 11, 22),
                ColDateTimeOffset = new DateTimeOffset(1978, 12, id, 10, 11, 22, 123, TimeSpan.FromHours(id)),
                ColSmallDateTime = new DateTime(1978, 12, id, 10, 11, 0),
                ColGuid = Guid.Parse($"{id}0000000-0000-0000-0000-000000000000"),
                ColBool = (id % 2) == 0,
                ColShortText = "Short text " + id.ToString(),
                ColLongText = "Long text " + id.ToString() + " " + Resources.BigTextData,
                ColNVarcharMax = "NVarcharMax text " + id.ToString() + " " + Resources.BigTextData
            };
        }

        private static void AssertDataTypesData(IDatabase korm)
        {
            List<DataTypesData> actualData = korm.Query<DataTypesData>().OrderBy(item => item.Id).ToList();
            actualData.Should().BeEquivalentTo(GetDataTypesData());
        }

        private static IEnumerable<Person> GetPersonData()
        {
            var data = new List<Person>();

            data.Add(new Person()
            {
                Id = 1,
                FirstName = "Milan",
                LastName = "Martiniak",
                Age = 32,
                Address = new List<string>() { "Petzvalova", "Pekna", "Zelena" },
                TestLongText = "Lorem ipsum dolor sit amet 1."
            });

            data.Add(new Person()
            {
                Id = 2,
                FirstName = "Peter",
                LastName = "Juráček",
                Age = 14,
                Address = new List<string>() { "Novozámocká" },
                TestLongText = "Lorem ipsum dolor sit amet 2."
            });

            return data;
        }

        private static void AssertData(IDatabase korm)
        {
            var person = korm.Query<Person>().FirstOrDefault(p => p.Id == 1);

            person.Should().NotBeNull();
            person.Id.Should().Be(1);
            person.Age.Should().Be(32);
            person.FirstName.Should().Be("Milan");
            person.LastName.Should().Be("Martiniak");
            person.Address.Should().BeEquivalentTo(new List<string>() { "Petzvalova", "Pekna", "Zelena" });
            person.TestLongText.Should().Be("Lorem ipsum dolor sit amet 1.");
        }

        #endregion
    }
}
