using Kros.Data.SqlServer;
using Kros.KORM.Materializer;
using Kros.KORM.Metadata;
using Kros.KORM.Query;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Kros.KORM.UnitTests.Query
{
    public class QueryProviderFactoryShould
    {
        [Fact]
        public void CreateOleDbProviderByConnection()
        {
            var factory = CreateFactory();

            var provider = factory.Create(new SqlConnection(), CreateModelBuilder(), DatabaseMapper);

            Assert.NotNull(provider);
        }

        [Fact]
        public void CreateSqlProviderConnection()
        {
            var factory = CreateFactory();

            var provider = factory.Create(new SqlConnection(), CreateModelBuilder(), DatabaseMapper);

            Assert.NotNull(provider);
        }

        private static DatabaseMapper DatabaseMapper => new DatabaseMapper(new ConventionModelMapper());

        [Fact]
        public void CreateOleDbProviderBySettings()
        {
            var factory = CreateFactory();
            var connectionString = new KormConnectionSettings() { ConnectionString = "", KormProvider = "System.Data.OleDb" };
            var provider = factory.Create(connectionString, CreateModelBuilder(), DatabaseMapper);

            Assert.NotNull(provider);
        }

        [Fact]
        public void CreateSqlProviderBySettings()
        {
            var factory = CreateFactory();
            var connectionString = new KormConnectionSettings() { ConnectionString = "", KormProvider = SqlServerDataHelper.ClientId };

            var provider = factory.Create(connectionString, CreateModelBuilder(), DatabaseMapper);

            Assert.NotNull(provider);
        }

        [Fact]
        public void CreateSqlProviderBySettingsCaseInsensitive()
        {
            var factory = CreateFactory();
            var connectionString = new KormConnectionSettings() { ConnectionString = "", KormProvider = SqlServerDataHelper.ClientId };

            var provider = factory.Create(connectionString, CreateModelBuilder(), DatabaseMapper);

            Assert.NotNull(provider);
        }

        private static ModelBuilder CreateModelBuilder()
        {
            return new ModelBuilder(new DynamicMethodModelFactory(DatabaseMapper));
        }

        private static IQueryProviderFactory CreateFactory() =>
            new SqlServerQueryProviderFactory();
    }
}
