using FluentAssertions;
using Kros.Data.SqlServer;
using Kros.KORM.Materializer;
using Kros.KORM.Metadata;
using Kros.KORM.Query;
using System.Configuration;
using System.Data.SqlClient;
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

            provider.Should().NotBeNull();
        }

        [Fact]
        public void CreateSqlProviderConnection()
        {
            var factory = CreateFactory();

            var provider = factory.Create(new SqlConnection(), CreateModelBuilder(), DatabaseMapper);

            provider.Should().NotBeNull();
        }

        private static DatabaseMapper DatabaseMapper => new DatabaseMapper(new ConventionModelMapper());

        [Fact]
        public void CreateOleDbProviderBySettings()
        {
            var factory = CreateFactory();
            var connectionString = new ConnectionStringSettings("Default", "", "System.Data.OleDb");
            var provider = factory.Create(connectionString, CreateModelBuilder(), DatabaseMapper);

            provider.Should().NotBeNull();
        }

        [Fact]
        public void CreateSqlProviderBySettings()
        {
            var factory = CreateFactory();
            var connectionString = new ConnectionStringSettings("Default", "", SqlServerDataHelper.ClientId);

            var provider = factory.Create(connectionString, CreateModelBuilder(), DatabaseMapper);

            provider.Should().NotBeNull();
        }

        [Fact]
        public void CreateSqlProviderBySettingsCaseInsensitive()
        {
            var factory = CreateFactory();
            var connectionString = new ConnectionStringSettings("Default", "", SqlServerDataHelper.ClientId);

            var provider = factory.Create(connectionString, CreateModelBuilder(), DatabaseMapper);

            provider.Should().NotBeNull();
        }

        private static ModelBuilder CreateModelBuilder()
        {
            return new ModelBuilder(new DynamicMethodModelFactory(DatabaseMapper));
        }

        private static IQueryProviderFactory CreateFactory() =>
            new SqlServerQueryProviderFactory();
    }
}
