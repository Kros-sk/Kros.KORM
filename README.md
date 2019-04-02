# Kros.KORM [![Build status](https://ci.appveyor.com/api/projects/status/xebjpdbakd45mfs4/branch/master?svg=true)](https://ci.appveyor.com/project/Kros/kros-libs-u2wo6/branch/master)

Kros.KORM is simple, fast and easy to use micro-ORM framework for .NETStandard created by Kros a.s. from Slovakia.

## Why to use Kros.KORM

* You can easily create query builder for creating queries returning IEnumerable of your POCO objects
* Linq support
* Saving changes to your data (Insert / Update / Delete)
* Kros.KORM supports bulk operations for fast inserting and updating large amounts of data (BulkInsert, BulkDelete)

### Documentation

For configuration, general information and examples [see the documentation.](https://kros-sk.github.io/docs/Kros.KORM/)

### Download

Kros.KORM is available from:

* Nuget [__Kros.KORM__](https://www.nuget.org/packages/Kros.KORM/)
* Nuget [__Kros.KORM.MsAccess__](https://www.nuget.org/packages/Kros.KORM.MsAccess/)
* Nuget [__Kros.KORM.Extensions.Asp__](https://www.nuget.org/packages/Kros.KORM.Extensions.Asp/)

## Contributing Guide

To contribute with new topics/information or make changes, see [contributing](https://github.com/Kros-sk/Kros.KORM/blob/master/CONTRIBUTING.md) for instructions and guidelines.

## This topic contains following sections

* [Query](#query)
* [Linq to Kros.KORM](#linq-to-kroskorm)
* [DataAnnotation attributes](#dataannotation-attributes)
* [Convention model mapper](#convention-model-mapper)
* [Converters](#converters)
* [OnAfterMaterialize](#onaftermaterialize)
* [Property injection](#property-injection)
* [Model builder](#model-builder)
* [Committing of changes](#committing-of-changes)
* [SQL commands executing](#sql-commands-executing)
* [Logging](#logging)
* [Supported database types](#supported-database-types)
* [ASP.NET Core extensions](#aspnet-core-extensions)
* [Unit and performance tests](#unit-and-performance-tests)

### Query

You can use Kros.KORM for creating queries and their materialization. Kros.KORM helps you put together desired query, that can return instances of objects populated from database by using foreach or linq.

#### Query for obtaining data

```c#
var people = database.Query<Person>()
    .Select("p.Id", "FirstName", "LastName", "PostCode")
    .From("Person JOIN Address ON (Person.AddressId = Address.Id)")
    .Where("Age > @1", 18);

foreach (var person in people)
{
    Console.WriteLine(person.FirstName);
}
```

For more information take a look at definition of [IQuery](https://kros-sk.github.io/Kros.Libs.Documentation/api/Kros.KORM.Query.IQuery-1.html).

### Linq to Kros.KORM

Kros.KORM allows you to use Linq for creating queries. Basic queries are translated to SQL language.

#### Example

```c#
var people = database.Query<Person>()
    .From("Person JOIN Address ON (Person.AddressId = Address.Id)")
    .Where(p => p.LastName.EndsWith("ová"))
    .OrderByDescending(p => p.Id)
    .Take(5);

foreach (var person in people)
{
    Console.WriteLine(person.FirstName);
}
```

Supported Linq methods are ```Where, FirstOrDefault, Take, Sum, Max, Min, OrderBy, OrderByDescending, ThenBy, ThenByDescending, Count, Any, Skip.```

Other methods, such as ```Select, GroupBy, Join``` are not supported at this moment because of their complexity.

You can use also some string functions in Linq queries:

| String function | Example                                               | Translation to T-SQL                          |
| --------------- | ----------------------------------------------------- | --------------------------------------------- |
| StartWith       | Where(p => p.FirstName.StartWith("Mi"))               | WHERE (FirstName LIKE @1 + '%')               |
| EndWith         | Where(p => p.LastName.EndWith("ová"))                 | WHERE (LastName LIKE '%' + @1)                |
| Contains        | Where(p => p.LastName.Contains("ia"))                 | WHERE (LastName LIKE '%' + @1 + '%')          |
| IsNullOrEmpty   | Where(p => String.IsNullOrEmpty(p.LastName))          | WHERE (LastName IS NULL OR LastName = '')     |
| ToUpper         | Where(p => p.LastName.ToUpper() == "Smith")           | WHERE (UPPER(LastName) = @1)                  |
| ToLower         | Where(p => p.LastName.ToLower() == "Smith")           | WHERE (LOWER(LastName) = @1)                  |
| Replace         | Where(p => p.FirstName.Replace("hn", "zo") == "Jozo") | WHERE (REPLACE(FirstName, @1, @2) = @3)       |
| Substring       | Where(p => p.FirstName.Substring(1, 2) == "oh")       | WHERE (SUBSTRING(FirstName, @1 + 1, @2) = @3) |
| Trim            | Where(p => p.FirstName.Trim() == "John")              | WHERE (RTRIM(LTRIM(FirstName)) = @1)          |

Translation is provided by implementation of [ISqlExpressionVisitor](https://kros-sk.github.io/Kros.Libs.Documentation/api/Kros.KORM.Query.Sql.ISqlExpressionVisitor.html).

### DataAnnotation attributes

Properties (not readonly or writeonly properties) are implicitly mapped to database fields with same name. When you want to map property to database field with different name use AliasAttribute. The same works for mapping POCO classes with database tables.

```c#
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
```

When you need to have read-write properties independent of the database use `NoMapAttribute`.

```c#
[NoMap]
public int Computed { get; set; }
```

### Convention model mapper

If you have different conventions for naming properties in POCO classes and fields in database, you can redefine behaviour of ModelMapper, which serves mapping POCO classes to database tables and vice versa.

#### Redefining mapping conventions example

```c#
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
```

Alternatively you can write your own implementation of [IModelMapper](https://kros-sk.github.io/Kros.Libs.Documentation/api/Kros.KORM.Metadata.IModelMapper.html).

##### Custom model mapper

```c#
Database.DefaultModelMapper = new CustomModelMapper();
```

If your POCO class is defined in external library, you can redefine mapper, so it can map properties of the model to desired database names.

##### External class mapping example

```c#
var externalPersonMap = new Dictionary<string, string>() {
    { nameOf(ExternalPerson.oId), "Id" },
    { nameOf(ExternalPerson.Name), "FirstName" },
    { nameOf(ExternalPerson.SecondName), "LastName" }
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
```

For dynamic mapping you can use method [SetColumnName<TModel, TValue>](https://kros-sk.github.io/Kros.Libs.Documentation/api/Kros.KORM.Metadata.IModelMapper.html#Kros_KORM_Metadata_IModelMapper_SetColumnName__2_System_Linq_Expressions_Expression_System_Func___0___1___System_String_)

```c#
Database.DefaultModelMapper.SetColumnName<Person, string>(p => p.Name, "FirstName");
```

### Converters

Data type of column in database and data type of property in your POCO class may differ. Some of these differences are automatically solved by Kros.KORM, for example `double` in database is converted to `int` in your model, same as `int` in database to `enum` in model, etc.

For more complicated conversion Kros.KORM offers possibility similar to data binding in WPF, where `IValueConverter` is used.

Imagine you store a list of addresses separated by some special character (for example #) in one long text column, but the property in your POCO class is list of strings.

Let's define a converter that can convert string to list of strings.

```c#
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
```

And now you can set this converter for your property.

```c#
[Converter(typeof(AddressesConverter))]
public List<string> Addresses { get; set; }
```

### OnAfterMaterialize

If you want to do some special action right after materialisation is done (for example to do some calculations) or you want to get some other values from source reader, that can not by processed automatically, your class should implement interface [IMaterialize](https://kros-sk.github.io/Kros.Libs.Documentation/api/Kros.KORM.Materializer.IMaterialize.html).

You can do whatever you need in method ```OnAfterMaterialize```.

For example, if you have three int columns for date in database (Year, Month and Day) but in your POCO class you have only one date property, you can solve it as follows:

```c#
[NoMap]
public DateTime Date { get; set; }

public void OnAfterMaterialize(IDataRecord source)
{
    var year = source.GetInt32(source.GetOrdinal("Year"));
    var month = source.GetInt32(source.GetOrdinal("Month"));
    var day = source.GetInt32(source.GetOrdinal("Day"));

    this.Date = new DateTime(year, month, day);
}
```

### Property injection

Sometimes you might need to inject some service to your model, for example calculator or logger. For these purposes Kros.KORM offers `IInjectionConfigurator`, that can help you with injection configuration.

Let's have properties in model

```c#
[NoMap]
public ICalculationService CalculationService { get; set; }

[NoMap]
public ILogger Logger { get; set; }
```

And that is how you can configure them.

```c#
Database.DefaultModelMapper
    .InjectionConfigurator<Person>()
        .FillProperty(p => p.CalculationService, () => new CalculationService())
        .FillProperty(p => p.Logger, () => ServiceContainer.Instance.Resolve<ILogger>());
```

### Model builder

For materialisation Kros.KORM uses `IModelFactory`, that creates factory for creating and filling your POCO objects.

By default `DynamicMethodModelFactory` is implemented, which uses dynamic method for creating delegates.

If you want to try some other implementation (for example based on reflexion) you can redefine property `Database.DefaultModelFactory`.

```c#
Database.DefaultModelFactory = new ReflectionModelfactory();
```

### Committing of changes

You can use Kros.KORM also for editing, adding or deleting records from database. [IdDbSet](https://kros-sk.github.io/Kros.Libs.Documentation/api/Kros.KORM/Kros.KORM.Query.IDbSet-1.html) is designed for that.

Records to edit or delete are identified by the primary key. You can set primary key to your POCO class by using `Key` attribute.

```c#
[Key()]
public int Id { get; set; }

public string FirstName { get; set; }

public string LastName { get; set; }
```

#### Inserting records to database

```c#
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
```

Kros.KORM supports bulk inserting, which is one of its best features. You add records to DbSet standardly by method ```Add```, but for committing to database use method ```BulkInsert``` instead of ```CommitChanges```.

```c#
var people = database.Query<Person>().AsDbSet();

foreach (var person in dataForImport)
{
    people.Add(person);
}

people.BulkInsert();
```

Kros.KORM supports also bulk update of records, you can use ```BulkUpdate``` method.

```c#
var people = database.Query<Person>().AsDbSet();

foreach (var person in dataForUpdate)
{
    people.Edit(person);
}

people.BulkUpdate();
```

This bulk way of inserting or updating data is several times faster than standard inserts or updates.

For both of bulk operations you can provide data as an argument of method. The advantage is that if you have a specific enumerator, you do not need to spill data into memory.

#### Primary key generating

Kros.KORM supports generating of primary keys for inserted records.

Support two types of generating:
1. Custom

Primary key must be simple `Int32` column. Primary key property in POCO class must be decorated by `Key` attribute and its property `AutoIncrementMethodType` must be set to `Custom`.

```c#
[Key(autoIncrementMethodType: AutoIncrementMethodType.Custom)]
public int Id { get; set; }
```

Kros.KORM generates primary key for every inserted record, that does not have value for primary key property. For generating primary keys implementations of [IIdGenerator](https://kros-sk.github.io/Kros.Libs.Documentation/api/Kros.Utils/Kros.Data.IIdGenerator.html) are used.

2. Identity

When you set  `AutoIncrementMethodType` to `Identity`, Kros.KORM use `MsSql Identity` for generating primary key and fill generated keys into entity.
```sql
CREATE TABLE [dbo].[Users](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FIrstName] [nvarchar](50) NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
```

```c#
[Key(autoIncrementMethodType: AutoIncrementMethodType.Identity)]
public int Id { get; set; }
```

When you call `dbSet.CommitChanges()`, Kros.KORM fill generated keys into entity. Unfortunately, he doesn't know it when you call a method `dbSet.BulkInsert()`.


#### Editing records in database

```c#
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
```

### Deleting records from database

```c#
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
```

#### Explicit transactions

By default, changes of a `DbSet` are committed to database in a transaction. If committing of one record fails, rollback of transaction is executed.

Sometimes you might come to situation, when such implicit transaction would not meet your requirements. For example you need to commit changes to two tables as an atomic operation. When saving changes to first of tables is not successful, you want to discard changes to the other table. Solution of that task is easy with explicit transactions supported by Kros.KORM. See the documentation of [BeginTransaction](https://kros-sk.github.io/Kros.Libs.Documentation/api/Kros.KORM/Kros.KORM.IDatabase.html#Kros_KORM_IDatabase_BeginTransaction).

```c#
using (var transaction = database.BeginTransaction())
{
    var invoicesDbSet = database.Query<Invoice>().AsDbSet();
    var itemsDbSet = database.Query<Item>().AsDbSet();

    try
    {
        invoicesDbSet.Add(invoices);
        invoicesDbSet.CommitChanges();

        itemsDbSet.Add(items);
        itemsDbSet.CommitChanges();

        transaction.Commit();
    }
    catch
    {
        transaction.Rollback();
    }
}
```

### SQL commands executing

Kros.KORM supports SQL commands execution. There are three types of commands:

* ```ExecuteNonQuery``` for commands that do not return value (DELETE, UPDATE, ...)
* ```ExecuteScalar``` for commands that return only one value (SELECT)
* ```ExecuteStoredProcedure``` for executing of stored procedures. Stored procedure may return scalar value or list of values or it can return data in output parameters.

#### Execution of stored procedure example

```c#
public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime BDay { get; set; }
}

private Database _database = new Database(new SqlConnection("connection string"));

// Stored procedure returns a scalar value.
int intResult = _database.ExecuteStoredProcedure<int>("ProcedureName");
DateTime dateResult = _database.ExecuteStoredProcedure<DateTime>("ProcedureName");

// Stored procedure sets the value of output parameter.
var parameters = new CommandParameterCollection();
parameters.Add("@param1", 10);
parameters.Add("@param2", DateTime.Now);
parameters.Add("@outputParam", null, DbType.String, ParameterDirection.Output);

_database.ExecuteStoredProcedure<string>("ProcedureName", parameters);

Console.WriteLine(parameters["@outputParam"].Value);


// Stored procedure returns complex object.
Person person = _database.ExecuteStoredProcedure<Person>("ProcedureName");


// Stored procedure returns list of complex objects.
IEnumerable<Person> persons = _database.ExecuteStoredProcedure<IEnumerable<Person>>("ProcedureName");
```

#### CommandTimeout support

If you want to execute time-consuming command, you will definitely appreciate `CommandTimeout` property of transaction. See the documentation of [BeginTransaction](https://kros-sk.github.io/Kros.Libs.Documentation/api/Kros.KORM/Kros.KORM.IDatabase.html#Kros_KORM_IDatabase_BeginTransaction).

Warning: You can set `CommandTimeout` only for main transaction, not for nested transactions. In that case CommandTimout of main transaction will be used.

```c#
IEnumerable<Person> persons = null;

using (var transaction = database.BeginTransaction(IsolationLevel.Chaos))
{
    transaction.CommandTimeout = 150;

    try
    {
        persons = database.ExecuteStoredProcedure<IEnumerable<Person>>("LongRunningProcedure_GetPersons");
        transaction.Commit();
    }
    catch
    {
        transaction.Rollback();
    }
}
```

### Logging

Kros.KORM offers the ability to log each generated and executed query. All you have to do is add this line to your source code.

```c#
Database.Log = Console.WriteLine;
```

### Supported database types

Kros.KORM uses its own [QueryProvider](https://kros-sk.github.io/Kros.Libs.Documentation/api/Kros.KORM/Kros.KORM.Query.QueryProvider.html) to execute query in a database. [ISqlExpressionVisitor](https://kros-sk.github.io/Kros.Libs.Documentation/api/Kros.KORM/Kros.KORM.Query.Sql.ISqlExpressionVisitor.html) transforms IQuery to SELECT command specific for each supported database engine.

MsAccess is suported from version 2.4 in Kros.KORM.MsAccess library. If you need to work with MS Access database, you have to refer this library in your project and register [MsAccessQueryProviderFactory](https://kros-sk.github.io/Kros.Libs.Documentation/api/Kros.KORM.MsAccess/Kros.KORM.Query.MsAccess.MsAccessQueryProviderFactory.html).

```c#
MsAccessQueryProviderFactory.Register();
```

Current version of Kros.KORM suports databases MS ACCESS and MS SQL.

If you want to support a different database engine, you can implement your own [IQueryProvider](https://kros-sk.github.io/Kros.Libs.Documentation/api/Kros.KORM/Kros.KORM.Query.IQueryProvider.html). And register it in [QueryProviderFactories](https://kros-sk.github.io/Kros.Libs.Documentation/api/Kros.KORM/Kros.KORM.Query.QueryProviderFactories.html).

```c#
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
```

### ASP.NET Core extensions
For simple integration into ASP.NET Core projects, the [__Kros.KORM.Extensions.Asp__](https://www.nuget.org/packages/Kros.KORM.Extensions.Asp/) package was created.

You can use the `AddKorm` extension method to register `IDatabase` to the DI container.

```
public void ConfigureServices(IServiceCollection services)
{
    services.AddKorm(Configuration);
}
```

The configuration file *(typically `appsettings.json`)* must contain a section `ConnectionString`.
```
  "ConnectionString": {
    "ProviderName": "System.Data.SqlClient",
    "ConnectionString": "Server=servername\\instancename;Initial Catalog=database;Persist Security Info=False;"
  }
```

If you need to initialize the database for [IIdGenerator](https://kros-sk.github.io/Kros.Libs.Documentation/api/Kros.Utils/Kros.Data.IIdGenerator.html) then you can call `InitDatabaseForIdGenerator`.

```
public void ConfigureServices(IServiceCollection services)
{
    services.AddKorm(Configuration)
        .InitDatabaseForIdGenerator();
}
```

### Unit and performance tests

Kros.KORM unit test coverage is more than 87%.
There are also some performance test written for Kros.KORM. Here you can see some of their results:

* Reading of 150 000 records with 25 columns (long strings and guids) from DataTable is finished in about 410 ms.
* Reading of 1 500 records with 25 columns (long strings and guids) from DataTable is finished in about 7 ms.
