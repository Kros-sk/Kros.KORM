using FluentAssertions;
using Kros.Data.BulkActions;
using Kros.Data.Schema;
using Kros.KORM.Helper;
using Kros.KORM.Materializer;
using Kros.KORM.Metadata;
using Kros.KORM.Query;
using Kros.KORM.Query.Providers;
using Kros.KORM.Query.Sql;
using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Providers
{
    public class QueryProviderFactoriesShould : IDisposable
    {
        [Fact]
        public void GetFactoryByProviderName()
        {
            CustomQueryProviderFactory.Register();

            QueryProviderFactories.GetFactory("System.Data.CustomDb").Should().BeOfType<CustomQueryProviderFactory>();
        }

        [Fact]
        public void GetFactoryByConnection()
        {
            CustomQueryProviderFactory.Register();

            QueryProviderFactories.GetFactory(new SqlConnection()).Should().BeOfType<CustomQueryProviderFactory>();
        }

        [Fact]
        public void ShouldThrowInvalidProgramExceptionWhenProviderNameIsNotRegistered()
        {
            CustomQueryProviderFactory.Register();

            Action action = () => QueryProviderFactories.GetFactory(new CustomConnection());

            action.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ShouldThrowInvalidProgramExceptionWhenConnectionIsNotRegistered()
        {
            CustomQueryProviderFactory.Register();

            Action action = () => QueryProviderFactories.GetFactory("System.Odbc");

            action.Should().Throw<InvalidOperationException>();
        }

        public void Dispose()
        {
            QueryProviderFactories.UnRegisterAll();
            SqlServerQueryProviderFactory.Register();
        }

        public class CustomQueryProvider : QueryProvider
        {
            public CustomQueryProvider(ConnectionStringSettings connectionString,
                ISqlExpressionVisitorFactory sqlGeneratorFactory,
                IModelBuilder modelBuilder,
                ILogger logger)
                : base(connectionString, sqlGeneratorFactory, modelBuilder, logger)
            {
            }

            public CustomQueryProvider(DbConnection connection,
                ISqlExpressionVisitorFactory sqlGeneratorFactory,
                IModelBuilder modelBuilder,
                ILogger logger)
                : base(connection, sqlGeneratorFactory, modelBuilder, logger)
            {
            }

            public override DbProviderFactory DbProviderFactory => SqlClientFactory.Instance;

            public override IBulkInsert CreateBulkInsert() => throw new NotImplementedException();

            public override IBulkUpdate CreateBulkUpdate() => throw new NotImplementedException();

            protected override IDatabaseSchemaLoader GetSchemaLoader()
            {
                throw new NotImplementedException();
            }
        }

        public class CustomQueryProviderFactory : IQueryProviderFactory
        {
            public CustomQueryProviderFactory()
            {
            }

            public IQueryProvider Create(DbConnection connection, IModelBuilder modelBuilder, IDatabaseMapper databaseMapper)
                => new CustomQueryProvider(
                    connection, new SqlServerSqlExpressionVisitorFactory(databaseMapper), modelBuilder, new Logger());

            public IQueryProvider Create(
                ConnectionStringSettings connectionString,
                IModelBuilder modelBuilder,
                IDatabaseMapper databaseMapper)
                => new CustomQueryProvider(
                    connectionString, new SqlServerSqlExpressionVisitorFactory(databaseMapper), modelBuilder, new Logger());

            internal static void Register()
                => QueryProviderFactories.Register<SqlConnection>("System.Data.CustomDb", new CustomQueryProviderFactory());
        }

        public class CustomConnection : DbConnection
        {
            public override string ConnectionString { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override string Database => throw new NotImplementedException();

            public override string DataSource => throw new NotImplementedException();

            public override string ServerVersion => throw new NotImplementedException();

            public override ConnectionState State => throw new NotImplementedException();

            public override void ChangeDatabase(string databaseName) => throw new NotImplementedException();

            public override void Close() => throw new NotImplementedException();

            public override void Open() => throw new NotImplementedException();

            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotImplementedException();

            protected override DbCommand CreateDbCommand() => throw new NotImplementedException();
        }
    }
}
