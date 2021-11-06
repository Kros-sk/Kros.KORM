using FluentAssertions;
using Kros.KORM.Metadata.Attribute;
using Kros.KORM.UnitTests.Base;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Kros.KORM.UnitTests.Integration
{
    public class QueryNullableFieldsTests : DatabaseTestBase
    {
        #region Helpers

        private const string Table_TestTable = "People";

        private static readonly string CreateTable_TestTable =
$@"CREATE TABLE [dbo].[{Table_TestTable}] (
    [Id] [int] NOT NULL,
    [Data] [nvarchar](50) NOT NULL,
    [NullableString] [nvarchar](50) NULL,
    [NullableInt] [int] NULL
) ON [PRIMARY];";

        private static readonly string InsertTestData =
$@"INSERT INTO {Table_TestTable} ([Id], [Data], [NullableString], [NullableInt]) VALUES
(1, 'Not null', 'Lorem', 100),
(2, 'String null', NULL, 100),
(3, 'Int null', 'Lorem', NULL),
(4, 'All null', NULL, NULL)";

        public int Fact { get; private set; }

        [Alias(Table_TestTable)]
        public class PersonClass
        {
            [Key]
            public int Id { get; set; }
            public string Data { get; set; }
            public string NullableString { get; set; }
            public int? NullableInt { get; set; }
        }

        [Alias(Table_TestTable)]
        public record PersonRecord
        {
            [Key]
            public int Id { get; set; }
            public string Data { get; set; }
            public string NullableString { get; set; }
            public int? NullableInt { get; set; }
        }

        private TestDatabase CreateTestDatabase() => CreateDatabase(new[] {
            CreateTable_TestTable,
            InsertTestData
        });

        #endregion

        [Fact]
        public void MapNullDataToClass()
        {
            var expectedData = new List<PersonClass>{
                new PersonClass
                {
                    Id = 1,
                    Data = "Not null",
                    NullableString = "Lorem",
                    NullableInt = 100
                },
                new PersonClass
                {
                    Id = 2,
                    Data = "String null",
                    NullableString = null,
                    NullableInt = 100
                },
                new PersonClass
                {
                    Id = 3,
                    Data = "Int null",
                    NullableString = "Lorem",
                    NullableInt = null
                },
                new PersonClass
                {
                    Id = 4,
                    Data = "All null",
                    NullableString = null,
                    NullableInt = null
                },
            };

            using TestDatabase korm = CreateTestDatabase();
            var data = korm.Query<PersonClass>().OrderBy(item => item.Id).ToList();

            data.Should().BeEquivalentTo(expectedData, config =>
            {
                config.WithStrictOrdering();
                return config;
            });
        }

        [Fact]
        public void MapNullDataToRecord()
        {
            var expectedData = new List<PersonRecord>{
                new PersonRecord
                {
                    Id = 1,
                    Data = "Not null",
                    NullableString = "Lorem",
                    NullableInt = 100
                },
                new PersonRecord
                {
                    Id = 2,
                    Data = "String null",
                    NullableString = null,
                    NullableInt = 100
                },
                new PersonRecord
                {
                    Id = 3,
                    Data = "Int null",
                    NullableString = "Lorem",
                    NullableInt = null
                },
                new PersonRecord
                {
                    Id = 4,
                    Data = "All null",
                    NullableString = null,
                    NullableInt = null
                },
            };

            using TestDatabase korm = CreateTestDatabase();
            var data = korm.Query<PersonRecord>().OrderBy(item => item.Id).ToList();

            data.Should().BeEquivalentTo(expectedData, config =>
            {
                config.WithStrictOrdering();
                return config;
            });
        }
    }
}
