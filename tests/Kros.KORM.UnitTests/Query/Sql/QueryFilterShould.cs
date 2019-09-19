using Kros.KORM.Metadata;
using Kros.KORM.Query;
using System.Data.SqlClient;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Sql
{
    public class QueryFilterShould : LinqTranslatorTestBase
    {
        [Fact]
        public void AddWhereConditionToSql()
        {
            var query = Query<Foo>();

            WasGeneratedSameSql(query, "SELECT Id, IsDeleted, Value FROM Foo WHERE ((Value > @Dqf1))", 2);
        }

        // Pomenovana tabulka
        // Pridať podmienku do dotazu
        // pridať podmienku do dotazu na scalarú hodnotu
        // pridať podmienku do dotazu keď bol ako querz builder
        //                            -- vztiahnúť ymenenú hodnotu z linqu
        // scalar
        // FirstOrDefault, sum, count, ...
        // ak tam už podmienka je, ale aj keď nie je
        // komplexnejšia podmienka
        // podmienka napísaná na iný tzp entitz. poprípade, že ta  vlastnosť tam ani neexistuje.

        protected override void WasGeneratedSameSql<T>(IQuery<T> query, string expectedSql, params object[] parameters)
        {
            (query.Provider as FakeQueryProvider).Execute(query);

            base.WasGeneratedSameSql(query, expectedSql, parameters);
        }

        public override IQuery<T> Query<T>()
            => Database.Builder
            .UseConnection(new SqlConnection())
            .UseDatabaseConfiguration<DatabaseConfiguration>()
            .UseQueryProviderFactory(new FakeQueryProviderFactory())
            .Build()
            .Query<T>();

        public class Foo
        {
            public int Id { get; set; }

            public bool IsDeleted { get; set; }

            public int Value { get; set; }
        }

        public class DatabaseConfiguration : DatabaseConfigurationBase
        {
            public override void OnModelCreating(ModelConfigurationBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Entity<Foo>()
                    .UseQueryFilter(f => f.Value > 2);
            }
        }
    }
}
