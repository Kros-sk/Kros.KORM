using FluentAssertions;
using Kros.KORM.Converter;
using Kros.KORM.Metadata;
using Kros.KORM.UnitTests.Base;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Kros.KORM.UnitTests.Converter
{
    public class UseConvertForNullValues : DatabaseTestBase
    {
        #region Database schema

        private const string TableName = "WeatherForecast";

        private static readonly string CreateTableScript =
$@"CREATE TABLE [dbo].[{TableName}] (
    [Id] [int] NOT NULL,
    [Data] [nvarchar](250) NULL,
) ON [PRIMARY];";

        private static readonly string InsertDataScript =
$@"INSERT INTO [{TableName}] VALUES (1, '[]');
INSERT INTO [{TableName}] VALUES (2, '[""lorem""]');
INSERT INTO [{TableName}] VALUES (3, '[""lorem"", ""ipsum""]');
INSERT INTO [{TableName}] VALUES (4, NULL);
INSERT INTO [{TableName}] VALUES (5, '');";

        #endregion

        #region Helpers

        private class DataItem
        {
            public int Id { get; set; }
            public List<string> Data { get; set; }
        }

        private class JsonToListConverter<T> : IConverter
        {
            public object Convert(object value)
            {
                string json = (string)value;
                System.Console.WriteLine($"*** {json}");
                return string.IsNullOrEmpty(json)
                    ? new List<T>()
                    : JsonConvert.DeserializeObject<List<T>>(json);
            }

            public object ConvertBack(object value) => JsonConvert.SerializeObject(value);
        }

        private class DatabaseConfiguration : DatabaseConfigurationBase
        {
            public override void OnModelCreating(ModelConfigurationBuilder modelBuilder)
            {
                modelBuilder.Entity<DataItem>()
                    .HasTableName(TableName)
                    .UseConverterForProperties<string>(NullAndTrimStringConverter.ConvertNullAndTrimString)
                    .Property(u => u.Data).UseConverter<JsonToListConverter<string>>();
            }
        }

        private static IDatabase CreateDatabaseWithConfiguration(TestDatabase sourceDb)
        {
            IDatabase db = Database.Builder
                .UseConnection(sourceDb.ConnectionString)
                .UseDatabaseConfiguration<DatabaseConfiguration>()
                .Build();
            return db;
        }

        #endregion

        public static IEnumerable<object[]> UseConverterForNullValue_Data()
        {
            yield return new object[] { 1, new List<string>() };
            yield return new object[] { 2, new List<string>(new[] { "lorem" }) };
            yield return new object[] { 3, new List<string>(new[] { "lorem", "ipsum" }) };
            yield return new object[] { 4, new List<string>() };
            yield return new object[] { 5, new List<string>() };
        }

        [Theory]
        [MemberData(nameof(UseConverterForNullValue_Data))]
        public void UseConverterForNullValue(int id, List<string> expectedData)
        {
            using TestDatabase testDb = CreateDatabase(new[] { CreateTableScript, InsertDataScript });
            using IDatabase db = CreateDatabaseWithConfiguration(testDb);

            DataItem expectedItem = new()
            {
                Id = id,
                Data = expectedData
            };
            DataItem actualItem = db.Query<DataItem>().Where(item => item.Id == id).ToList()[0];

            actualItem.Should().BeEquivalentTo(expectedItem);
        }
    }
}
