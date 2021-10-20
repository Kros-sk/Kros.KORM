# Kros.KORM [![Build Status](https://dev.azure.com/krossk/DevShared/_apis/build/status/Kros.KORM/Kros.KORM%20-%20CI?branchName=master)](https://dev.azure.com/krossk/DevShared/_build/latest?definitionId=56&branchName=master)

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
* [Configure model mapping by fluent api](#Configure-model-mapping-by-fluent-api)
* [Global query filter](#Global-query-filter)
* [Reserved words as names for columns or table](#Reserved-words-as-names-for-columns-or-table)
* [Converters](#converters)
* [Value generators](#value-generators)
* [OnAfterMaterialize](#onaftermaterialize)
* [Property injection](#property-injection)
* [Model builder](#model-builder)
* [Committing of changes](#committing-of-changes)
* [SQL commands executing](#sql-commands-executing)
* [Record types](#record-types)
* [Logging](#logging)
* [Supported database types](#supported-database-types)
* [ASP.NET Core extensions](#aspnet-core-extensions)
* [Unit and performance tests](#unit-and-performance-tests)

### Query

You can use Kros.KORM for creating queries and their materialization. Kros.KORM helps you put together desired query, that can return instances of objects populated from database by using foreach or linq.

#### Query for obtaining data

```CSharp
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

```CSharp
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

```CSharp
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

```CSharp
[NoMap]
public int Computed { get; set; }
```

### Convention model mapper

If you have different conventions for naming properties in POCO classes and fields in database, you can redefine behaviour of ModelMapper, which serves mapping POCO classes to database tables and vice versa.

#### Redefining mapping conventions example

```CSharp
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

```CSharp
Database.DefaultModelMapper = new CustomModelMapper();
```

If your POCO class is defined in external library, you can redefine mapper, so it can map properties of the model to desired database names.

##### External class mapping example

```CSharp
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

```CSharp
Database.DefaultModelMapper.SetColumnName<Person, string>(p => p.Name, "FirstName");
```

### Configure model mapping by fluent api

Configuration by data annotation attributes is OK in many scenarios. However, there are scenarios where we want to have a model definition and mapping it to a database separate.
For example, if you want to have entities in domain layer and mapping in infrastructure layer.

For these scenarios you can derive database configuration from `DatabaseConfigurationBase`.

```CSharp
public class User
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName => FirstName + " " + LastName;
    public Address Address { get; set; }
    public IEmailService EmailService { get; set; }
}

public class DatabaseConfiguration : DatabaseConfigurationBase
{
    public override void OnModelCreating(ModelConfigurationBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasTableName("Users")
            .HasPrimaryKey(entity => entity.Id).AutoIncrement(AutoIncrementMethodType.Custom)
            .UseConverterForProperties<string, NullToEmptyStringConverter>()
            .Property(entity => entity.Title).IgnoreConverter()
            .Property(entity => entity.FirstName).HasColumnName("Name")
            .Property(entity => entity.FullName).NoMap()
            .Property(entity => entity.Addresses).UseConverter<AddressConverter>()
            .Property(entity => entity.EmailService).InjectValue(() => new EmailService())
            .Property(entity => entity.IsGenerated).UseValueGeneratorOnInsert<RandomGenerator>();
    }
}
```

And use `IDatabaseBuilder` for creating KORM instance.

```CSharp
var database = Database
    .Builder
    .UseConnection(connection)
    .UseDatabaseConfiguration<DatabaseConfiguration>()
    .Build();
```

If converter is used for property type (`UseConverterForProperties`) and also for specific property of that type (`UseConverter`),
the latter one has precedence.

### Global query filter

In many cases, we want to define a global filter to apply to each query. For example: `ParentId = 1`, `UserId = ActiveUser.Id`, etc.

You can configurate query filter in `DatabaseConfiguration` class.

```CSharp
public class DatabaseConfiguration : DatabaseConfigurationBase
{
    public override void OnModelCreating(ModelConfigurationBuilder modelBuilder)
    {
        modelBuilder.Table("Document")
            .UseQueryFilter<Document>(entity => entity.UserId == ActiveUser.Id && entity.ParentId == 1);
    }
}
```

KORM will automatically add a condition `((UserId = @__Dqf1) AND (ParentId = @__Dqf2))` when calling any query using `Query<Document>()`.
> Except for direct sql calls like `_database.Query<Document>().Sql("SELECT * FROM DOCUMENT")`.

> :warning: Configuration `modelBuilder.Table("Documents")` is applied for all entities mapped to table `Documents` (for example `Document`, `DocumentDto`, `DocumentInfo`, ...).

#### Ignoring global filters

If I need to call a query without these conditions, I must explicitly say:

```Csharp
_database.Query<Document>()
    .IgnoreQueryFilters()
    .ToList();
```

### Reserved words as names for columns or table

If you need to name your column or table with a name that the server has among the reserved words (for example, `Order`), you must ensure that these names are quoted in queries.
If the queries are generated by KORM, then you must explicitly specify delimiters in the `DatabaseConfiguration`.

```csharp
public class DatabaseConfiguration : DatabaseConfigurationBase
{
    public override void OnModelCreating(ModelConfigurationBuilder modelBuilder)
    {
        modelBuilder.UseIdentifierDelimiters(Delimiters.SquareBrackets);
    }
}
```

### Converters

Data type of column in database and data type of property in your POCO class may differ. Some of these differences are automatically solved by Kros.KORM, for example `double` in database is converted to `int` in your model, same as `int` in database to `enum` in model, etc.

For more complicated conversion Kros.KORM offers possibility similar to data binding in WPF, where `IValueConverter` is used.

Imagine you store a list of addresses separated by some special character (for example #) in one long text column, but the property in your POCO class is list of strings.

Let's define a converter that can convert string to list of strings.

```CSharp
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
        return  string.Join("#", (value as List<string>));
    }
}
```

And now you can set this converter for your property using attribute or [fluent configuration](#Configure-model-mapping-by-fluent-api).

```CSharp
[Converter(typeof(AddressesConverter))]
public List<string> Addresses { get; set; }
```

### Value generators

Value generators are used to generate column values. KORM contains some predefined generators but you can create your own.

For this purpose exists `IValueGenerator` interface which your class must implement.

```c#
public interface IValueGenerator
{
    object GetValue();
}
```

Here is an example of custom value generator:

```c#
private class AutoIncrementValueGenerator : IValueGenerator
{
      public object GetValue() => 123;
}
```

For using value generators you can use these three methods in `DatabaseConfiguration`:

- `.UseValueGeneratorOnInsert<YourGenerator>()` - values will be generated on insert to the database.

- `.UseValueGeneratorOnUpdate<YourGenerator>()` - values will be generated on update to the database.
- `.UseValueGeneratorOnInsertOrUpdate<YourGenerator>()`  - values will be generated on insert and update to the database.


#### Currently predefined value generators:

- __CurrentTimeValueGenerator__ - Generator generates date and time that are set to the current Coordinated Universal Time (UTC).

### OnAfterMaterialize

If you want to do some special action right after materialisation is done (for example to do some calculations) or you want to get some other values from source reader, that can not by processed automatically, your class should implement interface [IMaterialize](https://kros-sk.github.io/Kros.Libs.Documentation/api/Kros.KORM.Materializer.IMaterialize.html).

You can do whatever you need in method ```OnAfterMaterialize```.

For example, if you have three int columns for date in database (Year, Month and Day) but in your POCO class you have only one date property, you can solve it as follows:

```CSharp
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

```CSharp
[NoMap]
public ICalculationService CalculationService { get; set; }

[NoMap]
public ILogger Logger { get; set; }
```

And that is how you can configure them.

```CSharp
Database.DefaultModelMapper
    .InjectionConfigurator<Person>()
        .FillProperty(p => p.CalculationService, () => new CalculationService())
        .FillProperty(p => p.Logger, () => ServiceContainer.Instance.Resolve<ILogger>());
```

### Model builder

For materialisation Kros.KORM uses `IModelFactory`, that creates factory for creating and filling your POCO objects.

By default `DynamicMethodModelFactory` is implemented, which uses dynamic method for creating delegates.

If you want to try some other implementation (for example based on reflexion) you can redefine property `Database.DefaultModelFactory`.

```CSharp
Database.DefaultModelFactory = new ReflectionModelfactory();
```

### Committing of changes

You can use Kros.KORM also for editing, adding or deleting records from database. [IdDbSet](https://kros-sk.github.io/Kros.Libs.Documentation/api/Kros.KORM/Kros.KORM.Query.IDbSet-1.html) is designed for that.

Records to edit or delete are identified by the primary key. You can set primary key to your POCO class by using `Key` attribute.

```CSharp
[Key()]
public int Id { get; set; }

public string FirstName { get; set; }

public string LastName { get; set; }
```

#### Inserting records to database

```CSharp
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

```CSharp
var people = database.Query<Person>().AsDbSet();

foreach (var person in dataForImport)
{
    people.Add(person);
}

people.BulkInsert();
```

Kros.KORM supports also bulk update of records, you can use ```BulkUpdate``` method.

```CSharp
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

KORM supports 'Int32' and 'Int64' generators. Primary key property in POCO class must be decorated by `Key` attribute
and its property `AutoIncrementMethodType` must be set to `Custom`.

```CSharp
public class User
{
    [Key(autoIncrementMethodType: AutoIncrementMethodType.Custom)]
    public int Id { get; set; }
}
```

Kros.KORM generates primary key for every inserted record, that does not have value for primary key property.
For generating primary keys implementations of [IIdGenerator](https://kros-sk.github.io/Kros.Libs.Documentation/api/Kros.Utils/Kros.Data.IIdGenerator.html)
are used.

The names of internal generators are the same as table names, for which the values are generated. But this can be explicitly
set to some other name. It can be used for example when one generated sequence of numbers need to be used in two tables.

```CSharp
public class User
{
    [Key(autoIncrementMethodType: AutoIncrementMethodType.Custom, generatorName: "CustomGeneratorName")]
    public int Id { get; set; }
}

// Or using fluent database configuration.

public class DatabaseConfiguration : DatabaseConfigurationBase
{
    public override void OnModelCreating(ModelConfigurationBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasPrimaryKey(entity => entity.Id).AutoIncrement(AutoIncrementMethodType.Custom, "CustomGeneratorName");
    }
}
```


2. Identity

When you set  `AutoIncrementMethodType` to `Identity`, Kros.KORM use `MsSql Identity` for generating primary key and fill generated keys into entity.

```sql
CREATE TABLE [dbo].[Users](
    [Id] [bigint] IDENTITY(1,1) NOT NULL,
    [FIrstName] [nvarchar](50) NULL,
CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED
(
    [Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
```

```CSharp
[Key(autoIncrementMethodType: AutoIncrementMethodType.Identity)]
public long Id { get; set; }
```

When you call `dbSet.CommitChanges()`, Kros.KORM fill generated keys into entity. Unfortunately, doesn't know it when you call a method `dbSet.BulkInsert()`.


#### Editing records in database

```CSharp
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

#### Deleting records from database

```CSharp
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

##### Deleting records by Ids or condition

```CSharp
public void Delete()
{
    using (var database = new Database(_connection))
    {
        var people = database.Query<Person>().AsDbSet();

        people.Delete(1);
        people.Delete(p => p.ParentId == 10);

        people.CommitChangesAsync();
    }
}
```

#### Upsert record by it's primary key

Kros.KORM supports upserting records based on primary key match.
This can be handy when you know the primary key (or composite primary key) of the record but you can not be sure if it already exists in database.

Given (CompanyId, UserId) is composite primary key for UserRole table:
```CSharp
var admin = new UserRole { CompanyId = 1, UserId = 11, Role = "Admin" };
var owner = new UserRole { CompanyId = 1, UserId = 11, Role = "Owner" };
var user = new UserRole { CompanyId = 2, UserId = 22, Role = "User" };

using (var database = new Database(_connection))
{
    var userRoles = database.Query<UserRole>().AsDbSet();

    userRoles.Add(admin);
    userRoles.CommitChanges();

    var userRoles = database.Query<UserRole>().AsDbSet();

    userRoles.Upsert(owner); // this will update admins UserRole to owner
    userRoles.Upsert(user); // this will insert user
    userRoles.CommitChanges();
}
```

#### Upsert record by custom columns match

It is possible to upsert records by match of non PK columns.
!!! Use this with caution. This updates all records with matching provided columns !!!

```CSharp
var admin1 = new UserRole { Id = 1, InternalUserNo = 11, Role = "Admin" };
var admin2 = new UserRole { Id = 2, InternalUserNo = 12, Role = "Admin" };
var owner1 = new UserRole { Id = 3, InternalUserNo = 11, Role = "Owner" };

using (var database = new Database(_connection))
{
    var userRoles = database.Query<UserRole>().AsDbSet();

    userRoles.Add(admin1);
    userRoles.CommitChanges();

    var userRoles = database.Query<UserRole>().AsDbSet()
        .WithCustomUpsertConditionColumns(nameof(UserRole.InternalUserNo));

    userRoles.Upsert(admin2); // this will insert new admin with internalUserNo = 12
    userRoles.Upsert(owner1); // this will update user with internalUserNo = 11 to Owner
    userRoles.CommitChanges();
}
```

#### Explicit transactions

By default, changes of a `DbSet` are committed to database in a transaction. If committing of one record fails, rollback of transaction is executed.

Sometimes you might come to situation, when such implicit transaction would not meet your requirements. For example you need to commit changes to two tables as an atomic operation. When saving changes to first of tables is not successful, you want to discard changes to the other table. Solution of that task is easy with explicit transactions supported by Kros.KORM. See the documentation of [BeginTransaction](https://kros-sk.github.io/Kros.Libs.Documentation/api/Kros.KORM/Kros.KORM.IDatabase.html#Kros_KORM_IDatabase_BeginTransaction).

```CSharp
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

#### Simplify Adding, Deleting and Editing records

For simplifying calling methods (`Add`, `Edit`, `Delete`) use extension methods from `IDatabaseExtensions` class.

```CSharp
await database.AddAsync(person);
await database.AddAsync(people);
await database.BulkAddAsync(people);
await database.DeleteAsync(person);
await database.DeleteAsync(people);
await database.DeleteAsync<Person>(2);
await database.DeleteAsync<Person>(p => p.Id == 2);
await database.DeleteAsync<Person>("Id = @1", 2);
await database.EditAsync(person);
await database.EditAsync(person, "Id", "Age");
await database.EditAsync(people);
await database.BulkEditAsync(people);
await database.UpsertAsync(person);
await database.UpsertAsync(people);

```


#### Execute with temp table

Kros.KORM offers special execute commands for SQL databases, that inserts provided simple data into temp table and
then executes some specified action using those temporary data.
You can find these extension methods in `IDatabaseExtensions` class.

```CSharp
database.ExecuteWithTempTable<TValue>(IEnumerable<TValue> values, action);
await database.ExecuteWithTempTableAsync<TValue>(IEnumerable<TValue> values, function);

database.ExecuteWithTempTable<TKey, TValue>(IDictionary<TKey, TValue> values, action);
await database.ExecuteWithTempTable<TKey, TValue>(IDictionary<TKey, TValue> values, function);

T database.ExecuteWithTempTable<T, TValue> (IEnumerable<TValue> values, action);
await T database.ExecuteWithTempTable<T, TValue> (IEnumerable<TValue> values, function);

T database.ExecuteWithTempTable<T, TKey, TValue> (IDictionary<TKey, TValue> values, action);
await T database.ExecuteWithTempTable<T,TKey, TValue> (IDictionary<TKey, TValue> values, function);

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

var ids = new List<int>(){ 0, 1, 2, 3 };

_database.ExecuteWithTempTable(ids, (database, tableName)
    => database.Query<Person>()
        .From($"PERSON AS P INNER JOIN {tableName} AS T ON (P.Id = T.Value)")
        .ToList());
        
public class IdDto
{
    public IdDto(int id)
    {
        Id = id;
    }
    
    public int Id { get; set; }
}

var ids = new List<IdDto>(){ new IdDto(0), new IdDto(1), new IdDto(2), new IdDto(3) };

_database.ExecuteWithTempTable(ids, (database, tableName)
    => database.Query<Person>()
        .Select("P.*")
        .From($"PERSON AS P INNER JOIN {tableName} AS T ON (P.Id = T.Id)")
        .ToList());
```

### SQL commands executing

Kros.KORM supports SQL commands execution. There are three types of commands:

* ```ExecuteNonQuery``` for commands that do not return value (DELETE, UPDATE, ...)

  ```CSharp
  private Database _database = new Database(new SqlConnection("connection string"));

  // to work with command parameters you can use CommandParameterCollection
  var parameters = new CommandParameterCollection();
  parameters.Add("@value", "value");
  parameters.Add("@id", 10);
  parameters.Add("@type", "DateTime");

  _database.ExecuteNonQuery("UPDATE Column = @value WHERE Id = @id AND Type = @type", parameters);

  // or you can send them directly via params array
  _database.ExecuteNonQuery("UPDATE Column = @value WHERE Id = @id AND Type = @type", "value", 10, "DateTime");
  ```

* ```ExecuteScalar``` for commands that return only one value (SELECT)

* ```ExecuteStoredProcedure``` for executing of stored procedures. Stored procedure may return scalar value or list of values or it can return data in output parameters.

#### Execution of stored procedure example

```CSharp
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

```CSharp
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

### Record types

KORM supports a new `record` type for model definition.

```csharp
public record Person(int Id, string FirstName, string LastName);

using var database = new Database(new SqlConnection("connection string"));

foreach (Person person = database.Query<Person>())
{
    Console.WriteLine($"{person.Id}: {person.FirstName} - {person.LastName}");
}
```

The same features as for "standard" `class`-es are supported. Converters, name mapping and value injection. It is possible to use [fluent notation](#Configure-model-mapping-by-fluent-api), but also using attributes.

To use attribute notation, you must use syntax with `property:` keyword.

```csharp
public record Person(int Id, [property: Alias("FirstName")]string Name);
```

Materializing `record` types is a bit faster than with property-filled classes.

1000 rows of `InMemoryDbReader`:

| Method      | Mean      | Error   | StdDev  |
| ----------- | --------- | ------- | ------- |
| RecordTypes | 301.50 μs | 5.07 μs | 7.11 μs |
| ClassTypes  | 458.10 μs | 7.13 μs | 6.68 μs |

### Logging

Kros.KORM offers the ability to log each generated and executed query. All you have to do is add this line to your source code.

```CSharp
Database.Log = Console.WriteLine;
```

### Supported database types

Kros.KORM uses its own [QueryProvider](https://kros-sk.github.io/Kros.Libs.Documentation/api/Kros.KORM/Kros.KORM.Query.QueryProvider.html) to execute query in a database. [ISqlExpressionVisitor](https://kros-sk.github.io/Kros.Libs.Documentation/api/Kros.KORM/Kros.KORM.Query.Sql.ISqlExpressionVisitor.html) transforms IQuery to SELECT command specific for each supported database engine.

MsAccess is suported from version 2.4 in Kros.KORM.MsAccess library. If you need to work with MS Access database, you have to refer this library in your project and register [MsAccessQueryProviderFactory](https://kros-sk.github.io/Kros.Libs.Documentation/api/Kros.KORM.MsAccess/Kros.KORM.Query.MsAccess.MsAccessQueryProviderFactory.html).

```CSharp
MsAccessQueryProviderFactory.Register();
```

Current version of Kros.KORM suports databases MS ACCESS and MS SQL.

If you want to support a different database engine, you can implement your own [IQueryProvider](https://kros-sk.github.io/Kros.Libs.Documentation/api/Kros.KORM/Kros.KORM.Query.IQueryProvider.html). And register it in [QueryProviderFactories](https://kros-sk.github.io/Kros.Libs.Documentation/api/Kros.KORM/Kros.KORM.Query.QueryProviderFactories.html).

```CSharp
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

``` c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddKorm(Configuration);
}
```

The configuration file *(typically `appsettings.json`)* must contain a section `ConnectionStrings`.

``` json
  "ConnectionStrings": {
    "DefaultConnection": "Server=servername\\instancename;Initial Catalog=database;Persist Security Info=False;"
  }
```

If you need to initialize the database for [IIdGenerator](https://kros-sk.github.io/Kros.Libs.Documentation/api/Kros.Utils/Kros.Data.IIdGenerator.html) then you can call `InitDatabaseForIdGenerator`.

``` c#
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
