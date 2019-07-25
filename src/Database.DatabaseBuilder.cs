using Kros.KORM.Materializer;
using Kros.KORM.Metadata;
using Kros.KORM.Query;
using Kros.Utils;
using System;
using System.Data.Common;

namespace Kros.KORM
{
    public partial class Database
    {
        private class DatabaseBuilder : IDatabaseBuilder
        {
            private IQueryProviderFactory _queryProviderFactory;
            private KormConnectionSettings _connectionString;
            private DbConnection _connection;
            private IModelFactory _modelFactory;
            private DatabaseConfigurationBase _databaseConfiguration;
            private readonly Lazy<ConventionModelMapper> _conventionModelMapper;
            private readonly Lazy<DatabaseMapper> _databaseMapper;
            private readonly Lazy<ModelBuilder> _modelBuilder;
            private bool _wasBuildCall = false;

            public DatabaseBuilder()
            {
                _conventionModelMapper = new Lazy<ConventionModelMapper>(CreateModelMapper);
                _databaseMapper = new Lazy<DatabaseMapper>(() => new DatabaseMapper(_conventionModelMapper.Value));
                _modelBuilder = new Lazy<ModelBuilder>(CreateModelBuilder);
            }

            private ModelBuilder CreateModelBuilder()
            {
                if (_modelFactory != null)
                {
                    return new ModelBuilder(_modelFactory);
                }
                else
                {
                    return new ModelBuilder(new DynamicMethodModelFactory(_databaseMapper.Value));
                }
            }

            private IQueryProvider CreateQueryProvider()
            {
                IQueryProviderFactory factory;
                if (_connectionString != null)
                {
                    factory = _queryProviderFactory ?? QueryProviderFactories.GetFactory(_connectionString.KormProvider);
                    return factory.Create(_connectionString, _modelBuilder.Value, _databaseMapper.Value);
                }
                else
                {
                    factory = _queryProviderFactory ?? QueryProviderFactories.GetFactory(_connection);
                    return factory.Create(_connection, _modelBuilder.Value, _databaseMapper.Value);
                }
            }

            private ConventionModelMapper CreateModelMapper()
            {
                var modelMapper = new ConventionModelMapper();
                if (_databaseConfiguration != null)
                {
                    var modelBuilder = new ModelConfigurationBuilder();
                    _databaseConfiguration.OnModelCreating(modelBuilder);
                    modelBuilder.Build(modelMapper);
                }

                return modelMapper;
            }

            public IDatabaseBuilder UseConnection(KormConnectionSettings connectionString)
            {
                CheckDuplicateSettingForConnection();
                CheckMultipleConfiguration();

                _connectionString = Check.NotNull(connectionString, nameof(connectionString));

                return this;
            }

            public IDatabaseBuilder UseConnection(string connectionString)
            {
                CheckDuplicateSettingForConnection();
                CheckMultipleConfiguration();
                Check.NotNullOrWhiteSpace(connectionString, nameof(connectionString));

                _connectionString = KormConnectionSettings.Parse(connectionString);

                return this;
            }

            public IDatabaseBuilder UseConnection(DbConnection connection)
            {
                CheckDuplicateSettingForConnection();
                CheckMultipleConfiguration();

                _connection = Check.NotNull(connection, nameof(connection));

                return this;
            }

            public IDatabaseBuilder UseQueryProviderFactory(IQueryProviderFactory queryProviderFactory)
            {
                CheckMultipleConfiguration();
                _queryProviderFactory = Check.NotNull(queryProviderFactory, nameof(queryProviderFactory));

                return this;
            }

            public IDatabaseBuilder UseModelFactory(IModelFactory modelFactory)
            {
                CheckMultipleConfiguration();
                _modelFactory = Check.NotNull(modelFactory, nameof(modelFactory));

                return this;
            }

            public IDatabaseBuilder UseDatabaseConfiguration<TConfiguration>()
                where TConfiguration : DatabaseConfigurationBase, new()
                => UseDatabaseConfiguration(new TConfiguration());

            public IDatabaseBuilder UseDatabaseConfiguration(DatabaseConfigurationBase databaseConfiguration)
            {
                CheckMultipleConfiguration();

                _databaseConfiguration = Check.NotNull(databaseConfiguration, nameof(databaseConfiguration));

                return this;
            }

            public IDatabase Build()
            {
                CheckIfConnectionIsSet();
                _wasBuildCall = true;

                var database = new Database();

                SetDatabaseMapper(database);
                SetModelBuilder(database);
                SetQueryProvider(database);

                return database;
            }

            private void CheckDuplicateSettingForConnection()
            {
                if (_connectionString != null || _connection != null)
                {
                    throw new InvalidOperationException(
                        string.Format(Properties.Resources.UseConnectionCanBeCallOnlyOne, nameof(UseConnection)));
                }
            }

            private void CheckIfConnectionIsSet()
            {
                if (_connectionString is null && _connection is null)
                {
                    throw new InvalidOperationException(
                        string.Format(Properties.Resources.UseConnectionMustBeCall, nameof(UseConnection), nameof(Build)));
                }
            }

            private void CheckMultipleConfiguration()
            {
                if (_wasBuildCall)
                {
                    throw new InvalidOperationException(Properties.Resources.ConfigurationIsNotAllowed);
                }
            }

            private void SetModelBuilder(Database database) => database._modelBuilder = _modelBuilder.Value;

            private void SetQueryProvider(Database database) => database._queryProvider = CreateQueryProvider();

            private void SetDatabaseMapper(Database database) => database._databaseMapper = _databaseMapper.Value;
        }
    }
}
