using Kros.KORM.Metadata;
using Kros.KORM.Query;
using System.Data.SqlClient;
using Xunit;
using System.Linq;

namespace Kros.KORM.UnitTests.Query.Sql
{
    public class QueryFilterShould : LinqTranslatorTestBase
    {
        private IDatabase _database;

        public QueryFilterShould()
        {
            _database = Database.Builder
                .UseConnection(new SqlConnection())
                .UseDatabaseConfiguration<DatabaseConfiguration>()
                .UseQueryProviderFactory(new FakeQueryProviderFactory())
                .Build();
        }

        [Fact]
        public void AddWhereConditionToSql()
        {
            var query = Query<Foo>();

            WasGeneratedSameSql(query, "SELECT Id, IsDeleted, Value FROM Foo WHERE ((Value > @Dqf1))", 2);
        }

        [Fact]
        public void DoNotAddWhereConditionIfIgnoreIsCalled()
        {
            var query = Query<Foo>().IgnoreQueryFilters();

            WasGeneratedSameSql2(query, "SELECT Id, IsDeleted, Value FROM Foo");
        }

        [Fact]
        public void AddWhereConditionToSqlWhenEntityHasConfiguredTableName()
        {
            var query = Query<Bar>();

            WasGeneratedSameSql(query, "SELECT Id, Value FROM Bars WHERE ((Value LIKE @Dqf1 + '%'))", "Slov");
        }

        [Fact]
        public void AddWhereConditionToSqlWhenQueryHasDefinedWhereCondition()
        {
            var query = Query<Foo>().Where("Id > @1", 22);

            WasGeneratedSameSql2(query, "SELECT Id, IsDeleted, Value FROM Foo WHERE ((Id > @1) AND ((Value > @Dqf1)))", 22, 2);
        }

        [Fact]
        public void AddWhereConditionToSqlWhenQueryHasDefinedLinqWhereCondition()
        {
            var query = Query<Foo>().Where(f => f.Id > 22);

            WasGeneratedSameSql2(query, "SELECT Id, IsDeleted, Value FROM Foo WHERE (((Value > @Dqf1)) AND ((Id > @1)))", 22, 2);
        }

        [Fact]
        public void AddWhereConditionToSqlWhenFilterIsDefinedOverAnotherEntity()
        {
            var query = Query<Foo2>();

            WasGeneratedSameSql(query, "SELECT Id FROM Foo WHERE ((Value > @Dqf1))", 2);
        }

        [Fact]
        public void AddWhereConditionToSqlWhenCallFirstOrDefault()
        {
            var query = Query<Foo>();
            query.FirstOrDefault();

            WasGeneratedSameSql(query, "SELECT TOP 1 Id, IsDeleted, Value FROM Foo WHERE ((Value > @Dqf1))", 2);
        }

        [Fact]
        public void RetrievingLambdaDataRepeatedly()
        {
            var query = Query<FooBar>();

            WasGeneratedSameSql2(query, "SELECT Value FROM FooBar WHERE ((Value > @Dqf1))", 0);

            query = Query<FooBar>();
            WasGeneratedSameSql2(query, "SELECT Value FROM FooBar WHERE ((Value > @Dqf1))", 1);
        }

        [Fact]
        public void AddComplexWhereCondition()
        {
            var query = Query<ComplexCondition>()
                .Select("Id")
                .OrderBy("Id")
                .Where(f => f.Id < 10000);

            WasGeneratedSameSql2(
                query,
                "SELECT Id FROM ComplexCondition WHERE (((((Value > @Dqf1) AND (Value < @Dqf2)) OR (Id > @Dqf3))) AND ((Id < @1))) ORDER BY Id",
                1, 1000, 0, 10000);
        }

        protected override void WasGeneratedSameSql<T>(IQuery<T> query, string expectedSql, params object[] parameters)
        {
            (query.Provider as FakeQueryProvider).Execute(query);

            base.WasGeneratedSameSql(query, expectedSql, parameters);

            (query.Provider as FakeQueryProvider).LastExpression = null;
        }

        private void WasGeneratedSameSql2<T>(IQueryable<T> query, string expectedSql, params object[] parameters)
            => WasGeneratedSameSql<T>(query as Query<T>, expectedSql, parameters);

        public override IQuery<T> Query<T>()
            => _database.Query<T>();

        public class Foo
        {
            public int Id { get; set; }

            public bool IsDeleted { get; set; }

            public int Value { get; set; }
        }

        public class Foo2
        {
            public int Id { get; set; }
        }

        public class Bar
        {
            public int Id { get; set; }

            public string Value { get; set; }
        }

        public class FooBar
        {
            public int Value { get; set; }
        }

        public class ComplexCondition
        {
            public int Id { get; set; }

            public int Value { get; set; }
        }

        public class DatabaseConfiguration : DatabaseConfigurationBase
        {
            public override void OnModelCreating(ModelConfigurationBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Entity<Foo>()
                    .UseQueryFilter(f => f.Value > 2);

                modelBuilder.Entity<Foo2>()
                    .HasTableName("Foo");

                modelBuilder.Entity<Bar>()
                    .HasTableName("Bars")
                    .UseQueryFilter(b => b.Value.StartsWith("Slov"));

                modelBuilder.Entity<FooBar>()
                    .UseQueryFilter(f => f.Value > GetValue());

                modelBuilder.Entity<ComplexCondition>()
                    .UseQueryFilter(f => (f.Value > 1 && f.Value < 1000) || f.Id > 0);
            }

            private int _value = 0;
            private int GetValue() => _value++;
        }
    }
}
