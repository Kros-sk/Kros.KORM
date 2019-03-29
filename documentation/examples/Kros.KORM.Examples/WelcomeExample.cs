using Kros.Data.BulkActions;
using Kros.Data.Schema;
using Kros.KORM.Converter;
using Kros.KORM.Helper;
using Kros.KORM.Injection;
using Kros.KORM.Materializer;
using Kros.KORM.Metadata;
using Kros.KORM.Metadata.Attribute;
using Kros.KORM.Query;
using Kros.KORM.Query.MsAccess;
using Kros.KORM.Query.Sql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;

namespace Kros.KORM.Tests.Performance.Doc
{
    internal class WelcomeExample
    {
        private void RegisterMsAccessFactory()
        {
            #region MsAccessQueryProviderFactory
            MsAccessQueryProviderFactory.Register();
            #endregion
        }

        private void MainExample()
        {
            DataTable dataTable = null;
            var connection = new SqlConnection();

            #region MainExample
            using (var database = new Database(connection))
            {
                var people = database.ModelBuilder.Materialize<Person>(dataTable);

                foreach (var person in people)
                {
                    Console.WriteLine(person.FirstName);
                }
            }
            #endregion
        }

        private SqlConnection _connection = new SqlConnection();
        private SqlCommand _command = null;

        #region AttributeExample
        [Alias("Workers")]
        private class Staff
        {
            [Alias("PK")]
            public int Id { get; set; }

            [Alias("Name")]
            public string FirstName { get; set; }

            [Alias("SecondName")]
            public string LastName { get; set; }
        }

        private void StaffExample()
        {
            using (var database = new Database(_connection))
            {
                _command.CommandText = "SELECT PK, Name, SecondName from Workers";

                using (var reader = _command.ExecuteReader())
                {
                    var staff = database.ModelBuilder.Materialize<Staff>(reader);
                }
            }
        }
        #endregion

        private void Convention()
        {
            #region Convention
            Database.DefaultModelMapper.MapColumnName = (colInfo, modelType) =>
            {
                return string.Format("COL_{0}", colInfo.PropertyInfo.Name.ToUpper());
            };

            Database.DefaultModelMapper.MapTableName = (tInfo, type) =>
            {
                return string.Format("TABLE_{0}", type.Name.ToUpper());
            };

            using (var database = new Database(_connection))
            {
                _command.CommandText = "SELECT COL_ID, COL_FIRSTNAME from TABLE_WORKERS";

                using (var reader = _command.ExecuteReader())
                {
                    var people = database.ModelBuilder.Materialize<Person>(reader);

                    foreach (var person in people)
                    {
                        Console.WriteLine(person.FirstName);
                    }
                }
            }
            #endregion

            #region CustomModelMapper
            Database.DefaultModelMapper = new CustomModelMapper();
            #endregion

            #region ReflectionModelBuilder
            Database.DefaultModelFactory = new ReflectionModelfactory();
            #endregion
        }

        private void MapExternalClass()
        {
            #region MapExternalClass
            var externalPersonMap = new Dictionary<string, string>() {
                { PropertyName<ExternalPerson>.GetPropertyName(p => p.oId), "Id" },
                { PropertyName<ExternalPerson>.GetPropertyName(p => p.Name), "FirstName" },
                { PropertyName<ExternalPerson>.GetPropertyName(p => p.SecondName), "LastName" }
            };

            Database.DefaultModelMapper.MapColumnName = (colInfo, modelType) =>
            {
                if (modelType == typeof(ExternalPerson))
                {
                    return externalPersonMap[colInfo.PropertyInfo.Name];
                }
                else
                {
                    return colInfo.PropertyInfo.Name;
                }
            };

            using (var database = new Database(_connection))
            {
                var people = database.Query<ExternalPerson>();

                foreach (var person in people)
                {
                    Console.WriteLine($"{person.oId} : {person.Name}-{person.SecondName}");
                }
            }
            #endregion
        }

        private class ReflectionModelfactory : IModelFactory
        {
            public Func<IDataReader, T> GetFactory<T>(IDataReader reader)
            {
                throw new NotImplementedException();
            }
        }

        private class CustomModelMapper : IModelMapper
        {
            public Func<ColumnInfo, Type, string> MapColumnName
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public Func<TableInfo, IEnumerable<ColumnInfo>> MapPrimaryKey
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public Func<TableInfo, Type, string> MapTableName
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public IInjector GetInjector<T>()
            {
                throw new NotImplementedException();
            }

            public TableInfo GetTableInfo<T>()
            {
                throw new NotImplementedException();
            }

            public TableInfo GetTableInfo(Type modelType)
            {
                throw new NotImplementedException();
            }

            public IInjectionConfigurator<T> Injection<T>()
            {
                throw new NotImplementedException();
            }

            public IInjectionConfigurator<T> InjectionConfigurator<T>()
            {
                throw new NotImplementedException();
            }

            public void SetColumnName<TModel, TValue>(Expression<Func<TModel, TValue>> modelProperty, string columnName) where TModel : class
            {
                throw new NotImplementedException();
            }
        }

        private class Person : IMaterialize
        {
            [Alias("PK")]
            public int Id { get; set; }

            [Alias("Name")]
            public string FirstName { get; set; }

            [Alias("SecondName")]
            public string LastName { get; set; }

            #region Converter
            [Converter(typeof(AddressesConverter))]
            public List<string> Addresses { get; set; }
            #endregion

            #region NoMap
            [NoMap]
            public int Computed { get; set; }
            #endregion

            #region OnAfterMaterialize
            [NoMap]
            public DateTime Date { get; set; }

            public void OnAfterMaterialize(IDataRecord source)
            {
                var year = source.GetInt32(source.GetOrdinal("Year"));
                var month = source.GetInt32(source.GetOrdinal("Month"));
                var day = source.GetInt32(source.GetOrdinal("Day"));

                this.Date = new DateTime(year, month, day);
            }
            #endregion

            #region InjectionProperty

            [NoMap]
            public ICalculationService CalculationService { get; set; }

            [NoMap]
            public ILogger Logger { get; set; }

            #endregion
        }

        private class ExternalPerson
        {
            public int oId { get; set; }

            public string Name { get; set; }

            [Alias("SecondName")]
            public string SecondName { get; set; }
        }

        #region AddressConverter
        public class AddressesConverter : IConverter
        {
            public object Convert(object value)
            {
                var ret = new List<string>();
                if (value != null)
                {
                    var address = value.ToString();
                    var addresses = address.Split('#');

                    ret.AddRange(addresses);
                }

                return ret;
            }

            public object ConvertBack(object value)
            {
                var addresses = string.Join("#", (value as List<string>));

                return addresses;
            }
        }
        #endregion

        #region InsertDbSet

        public void Insert()
        {
            using (var database = new Database(_connection))
            {
                var people = database.Query<Person>().AsDbSet();

                people.Add(new Person() { Id = 1, FirstName = "Jean Claude", LastName = "Van Damme" });
                people.Add(new Person() { Id = 2, FirstName = "Sylvester", LastName = "Stallone" });

                people.CommitChanges();
            }
        }

        #endregion

        #region IdGenerator

        private class Foo
        {
            #region AutoIncrement
            [Key(autoIncrementMethodType: AutoIncrementMethodType.Custom)]
            public int Id { get; set; }
            #endregion
        }

        #endregion

        #region EditDbSet

        public void Edit()
        {
            using (var database = new Database(_connection))
            {
                var people = database.Query<Person>().AsDbSet();

                foreach (var person in people)
                {
                    person.LastName += "ová";
                    people.Edit(person);
                }

                people.CommitChanges();
            }
        }

        #endregion

        #region DeleteDbSet

        public void Delete()
        {
            using (var database = new Database(_connection))
            {
                var people = database.Query<Person>().AsDbSet();

                people.Delete(people.FirstOrDefault(x => x.Id == 1));
                people.Delete(people.FirstOrDefault(x => x.Id == 2));

                people.CommitChanges();
            }
        }

        #endregion

        private void Logging()
        {
            #region Logging
            Database.Log = Console.WriteLine;
            #endregion
        }

        private void BulkInsert()
        {
            Database database = null;
            var dataForImport = new List<Person>();
            #region BulkInsert
            var people = database.Query<Person>().AsDbSet();

            foreach (var person in dataForImport)
            {
                people.Add(person);
            }

            people.BulkInsert();
            #endregion
        }

        private void BulkUpdate()
        {
            Database database = null;
            var dataForUpdate = new List<Person>();
            #region BulkUpdate
            var people = database.Query<Person>().AsDbSet();

            foreach (var person in dataForUpdate)
            {
                people.Edit(person);
            }

            people.BulkUpdate();
            #endregion
        }

        #region Injections

        private interface ICalculationService
        {
        }

        public interface ILogger { }

        public class ServiceContainer
        {
            public static ServiceContainer Instance { get; set; }

            public T Resolve<T>() => throw new NotImplementedException();
        }

        public class CalculationService : ICalculationService { }

        private void Injections()
        {
            #region InectionConfiguration
            Database.DefaultModelMapper
                .InjectionConfigurator<Person>()
                    .FillProperty(p => p.CalculationService, () => new CalculationService())
                    .FillProperty(p => p.Logger, () => ServiceContainer.Instance.Resolve<ILogger>());
            #endregion
        }

        #endregion
    }

    #region CustomQueryProvider
    public class CustomQueryProvider : QueryProvider
    {
        public CustomQueryProvider(ConnectionStringSettings connectionString,
           ISqlExpressionVisitor sqlGenerator,
           IModelBuilder modelBuilder,
           ILogger logger)
            : base(connectionString, sqlGenerator, modelBuilder, logger)
        {
        }

        public CustomQueryProvider(DbConnection connection,
            ISqlExpressionVisitor sqlGenerator,
            IModelBuilder modelBuilder,
            ILogger logger)
                : base(connection, sqlGenerator, modelBuilder, logger)
        {
        }

        public override DbProviderFactory DbProviderFactory => CustomDbProviderFactory.Instance;

        public override IBulkInsert CreateBulkInsert()
        {
            if (IsExternalConnection)
            {
                return new CustomBulkInsert(Connection as CustomConnection);
            }
            else
            {
                return new CustomBulkInsert(ConnectionString);
            }
        }

        public override IBulkUpdate CreateBulkUpdate()
        {
            if (IsExternalConnection)
            {
                return new CustomBulkUpdate(Connection as CustomConnection);
            }
            else
            {
                return new CustomBulkUpdate(ConnectionString);
            }
        }

        protected override IDatabaseSchemaLoader GetSchemaLoader()
        {
            throw new NotImplementedException();
        }
    }

    public class CustomQuerySqlGenerator : DefaultQuerySqlGenerator
    {
        public CustomQuerySqlGenerator(IDatabaseMapper databaseMapper)
            : base(databaseMapper)
        { }
    }

    public class CustomQueryProviderFactory : IQueryProviderFactory
    {
        public Query.IQueryProvider Create(DbConnection connection, IModelBuilder modelBuilder, IDatabaseMapper databaseMapper) =>
            new CustomQueryProvider(connection, new CustomQuerySqlGenerator(databaseMapper), modelBuilder, new Logger());

        public Query.IQueryProvider Create(ConnectionStringSettings connectionString, IModelBuilder modelBuilder, IDatabaseMapper databaseMapper) =>
            new CustomQueryProvider(connectionString, new CustomQuerySqlGenerator(databaseMapper), modelBuilder, new Logger());

        public static void Register()
        {
            QueryProviderFactories.Register<CustomConnection>("System.Data.CustomDb", new CustomQueryProviderFactory());
        }
    }
    #endregion

    internal class CustomBulkInsert : IBulkInsert
    {
        private CustomConnection _customConnection;

        public CustomBulkInsert(CustomConnection customConnection)
        {
            _customConnection = customConnection;
        }

        public CustomBulkInsert(string connectionString)
        {
        }

        public int BatchSize { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int BulkInsertTimeout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string DestinationTableName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Insert(IBulkActionDataReader reader)
        {
            throw new NotImplementedException();
        }

        public void Insert(IDataReader reader)
        {
            throw new NotImplementedException();
        }

        public void Insert(DataTable table)
        {
            throw new NotImplementedException();
        }
    }

    internal class CustomBulkUpdate : IBulkUpdate
    {
        private CustomConnection _customConnection;

        public CustomBulkUpdate(CustomConnection customConnection)
        {
            _customConnection = customConnection;
        }

        public CustomBulkUpdate(string connectionString)
        {
        }

        public string DestinationTableName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Action<IDbConnection, IDbTransaction, string> TempTableAction { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string PrimaryKeyColumn { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Update(IBulkActionDataReader reader)
        {
            throw new NotImplementedException();
        }

        public void Update(IDataReader reader)
        {
            throw new NotImplementedException();
        }

        public void Update(DataTable table)
        {
            throw new NotImplementedException();
        }
    }

    internal class CustomConnection : DbConnection
    {
        public override string ConnectionString { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override string Database => throw new NotImplementedException();

        public override string DataSource => throw new NotImplementedException();

        public override string ServerVersion => throw new NotImplementedException();

        public override ConnectionState State => throw new NotImplementedException();

        public override void ChangeDatabase(string databaseName)
        {
            throw new NotImplementedException();
        }

        public override void Close()
        {
            throw new NotImplementedException();
        }

        public override void Open()
        {
            throw new NotImplementedException();
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            throw new NotImplementedException();
        }

        protected override DbCommand CreateDbCommand()
        {
            throw new NotImplementedException();
        }
    }

    internal class CustomDbProviderFactory : DbProviderFactory
    {
        public static CustomDbProviderFactory Instance = new CustomDbProviderFactory();
    }
}