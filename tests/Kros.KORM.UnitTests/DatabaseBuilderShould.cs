using FluentAssertions;
using Kros.KORM.Materializer;
using Kros.KORM.UnitTests.Integration;
using Kros.UnitTests;
using NSubstitute;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using Xunit;
using System.Linq;
using System.Data;
using Kros.KORM.Query;
using System;
using Kros.KORM.Metadata;

namespace Kros.KORM.UnitTests
{
    public class DatabaseBuilderShould : SqlServerDatabaseTestBase
    {
        #region Nested Classes

        private class Foo
        {
            public int Id { get; set; }

            public int Value { get; set; }
        }

        private class Bar
        {
            public int RowId { get; set; }

            public int Age { get; set; }
        }

        #endregion

        #region SQL Scripts

        private static readonly string CreateTable_FooTable =
$@"CREATE TABLE [dbo].[Foo] (
    [Id] [int] NOT NULL,
    [Value] [int]
) ON [PRIMARY];";

        private static readonly string InsertIntoFooScript = $@"INSERT INTO [Foo] VALUES (1, 11);";

        #endregion

        protected override string BaseConnectionString => IntegrationTestConfig.ConnectionString;

        protected override IEnumerable<string> DatabaseInitScripts
        {
            get
            {
                yield return CreateTable_FooTable;
                yield return InsertIntoFooScript;
            }
        }

        [Fact]
        public void BuildInstanceWithConnectionStringSettings()
        {
            var database = Database
                .Builder
                .UseConnection(
                    new ConnectionStringSettings("KORM", ServerHelper.Connection.ConnectionString, "System.Data.SqlClient"))
                .Build();

            DatabaseShouldNotBeNull(database);
            DatabaseShouldHaveItem(database);
        }

        [Fact]
        public void BuildInstanceWithConnectionString()
        {
            var database = Database
                .Builder
                .UseConnection(ServerHelper.Connection.ConnectionString, "System.Data.SqlClient")
                .Build();

            DatabaseShouldNotBeNull(database);
            DatabaseShouldHaveItem(database);
        }

        [Fact]
        public void BuildInstanceWithConnection()
        {
            var database = Database
                .Builder
                .UseConnection(ServerHelper.Connection)
                .Build();

            DatabaseShouldNotBeNull(database);
            DatabaseShouldHaveItem(database);
        }

        [Fact]
        public void BuildInstanceWithCustomModelFactory()
        {
            var modelFactory = Substitute.For<IModelFactory>();

            var database = Database
                .Builder
                .UseConnection(ServerHelper.Connection)
                .UseModelFactory(modelFactory)
                .Build();

            DatabaseShouldNotBeNull(database);

            database.Query<Foo>().FirstOrDefault(f => f.Id == 1);
            modelFactory.Received().GetFactory<Foo>(Arg.Any<IDataReader>());
        }

        [Fact]
        public void BuildInstanceWithConnectionAndCustomQueryProviderFactory()
        {
            var queryProviderFactory = Substitute.For<IQueryProviderFactory>();

            var database = Database
                .Builder
                .UseConnection(ServerHelper.Connection)
                .UseQueryProviderFactory(queryProviderFactory)
                .Build();

            DatabaseShouldNotBeNull(database);

            database.Query<Foo>().FirstOrDefault(f => f.Id == 1);
            queryProviderFactory.Received().Create(ServerHelper.Connection, database.ModelBuilder, Arg.Any<DatabaseMapper>());
        }

        [Fact]
        public void BuildInstanceWithConnectionStringSettingsAndCustomQueryProviderFactory()
        {
            var queryProviderFactory = Substitute.For<IQueryProviderFactory>();

            var database = Database
                .Builder
                .UseConnection(ServerHelper.Connection.ConnectionString, "System.Data.SqlClient")
                .UseQueryProviderFactory(queryProviderFactory)
                .Build();

            DatabaseShouldNotBeNull(database);

            database.Query<Foo>().FirstOrDefault(f => f.Id == 1);
            queryProviderFactory.Received().Create(Arg.Any<ConnectionStringSettings>(), database.ModelBuilder, Arg.Any<DatabaseMapper>());
        }

        [Fact]
        public void ThrowExceptionWhenCallUseConnectionMoreTime()
        {
            Action build = () => Database
                .Builder
                .UseConnection(ServerHelper.Connection.ConnectionString, "System.Data.SqlClient")
                .UseConnection(ServerHelper.Connection)
                .Build();

            build.Should().Throw<InvalidOperationException>();

            build = () => Database
                .Builder
                .UseConnection(new ConnectionStringSettings(ServerHelper.Connection.ConnectionString, "System.Data.SqlClient"))
                .UseConnection(ServerHelper.Connection)
                .Build();

            build.Should().Throw<InvalidOperationException>();

            build = () => Database
                .Builder
                .UseConnection(new ConnectionStringSettings(ServerHelper.Connection.ConnectionString, "System.Data.SqlClient"))
                .UseConnection(ServerHelper.Connection.ConnectionString, "System.Data.SqlClient")
                .Build();

            build.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ThrowExceptionWhenUseExceptionWasNotCall()
        {
            Action build = () => Database
                .Builder
                .Build();

            build.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void UseDatabaseConfigurationForBuildingModel()
        {
            var database = Database
               .Builder
               .UseConnection(
                   new ConnectionStringSettings("KORM", ServerHelper.Connection.ConnectionString, "System.Data.SqlClient"))
               .UseDatabaseConfiguration<DatabaseConfiguration>()
               .Build();

            database.Query<Bar>()
                .FirstOrDefault(b => b.RowId == 1)
                .Age.Should().Be(11);
        }

        [Fact]
        public void BuildInstanceMoreTimes()
        {
            var builder = Database
                .Builder
                .UseConnection(ServerHelper.Connection)
                .UseDatabaseConfiguration<DatabaseConfiguration>();

            IDatabase database = builder.Build();
            database.Query<Bar>()
                .FirstOrDefault(b => b.RowId == 1)
                .Age.Should().Be(11);

            IDatabase database2 = builder.Build();
            database2.Query<Bar>()
                .FirstOrDefault(b => b.RowId == 1)
                .Age.Should().Be(11);
        }

        [Fact]
        public void ThrowExceptionWhenTryConfigureAfterBuild()
        {
            IDatabaseBuilder builder = Database
                .Builder
                .UseConnection(ServerHelper.Connection);

            builder.Build();

            IModelFactory modelFactory = Substitute.For<IModelFactory>();
            IQueryProviderFactory queryProviderFactory = Substitute.For<IQueryProviderFactory>();

            void ShouldThrowException(Action action)
            {
                action
                    .Should()
                    .Throw<InvalidOperationException>(
                    "The configuration is not allowed if the Build method has already been called.");
            }

            ShouldThrowException(() => builder.UseConnection(ServerHelper.Connection));
            ShouldThrowException(() => builder.UseConnection(
                new ConnectionStringSettings("KORM", ServerHelper.Connection.ConnectionString, "System.Data.SqlClient")));
            ShouldThrowException(() => builder.UseConnection(
                ServerHelper.Connection.ConnectionString, "System.Data.SqlClient"));
            ShouldThrowException(() => builder.UseDatabaseConfiguration<DatabaseConfiguration>());
            ShouldThrowException(() => builder.UseDatabaseConfiguration(new DatabaseConfiguration()));
            ShouldThrowException(() => builder.UseModelFactory(modelFactory));
            ShouldThrowException(() => builder.UseQueryProviderFactory(queryProviderFactory));
        }

        private static void DatabaseShouldNotBeNull(IDatabase database)
        {
            database.Should().NotBeNull();
            database.ModelBuilder.Should().NotBeNull();
            database.DbProviderFactory.Should().NotBeNull();
        }

        private static void DatabaseShouldHaveItem(IDatabase database)
        {
            database.Query<Foo>()
                .FirstOrDefault(f => f.Id == 1)
                .Value.Should().Be(11);
        }

        public class DatabaseConfiguration: DatabaseConfigurationBase
        {
            public override void OnModelCreating(ModelConfigurationBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Entity<Bar>()
                    .HasTableName("Foo")
                    .HasPrimaryKey(p => p.RowId)
                    .Property(p => p.RowId).HasColumnName("Id")
                    .Property(p => p.Age).HasColumnName("Value");
            }
        }
    }
}
