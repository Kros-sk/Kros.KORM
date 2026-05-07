using Kros.Data;
using Kros.Data.BulkActions;
using Kros.KORM.CommandGenerator;
using Kros.KORM.Data;
using Kros.KORM.Exceptions;
using Kros.KORM.Metadata;
using Kros.KORM.Query;
using Kros.KORM.Query.Sql;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kros.KORM.UnitTests
{
    /// <summary>
    /// Summary description for DbSetShould
    /// </summary>
    public class DbSetShould
    {
        #region Tests

        private static int GetCount<T>(IEnumerable<T> values) => System.Linq.Enumerable.Count(values);

        [Fact]
        public void AddItemToPendingChanges()
        {
            IDbSet<Person> dbSet = new DbSet<Person>(Substitute.For<ICommandGenerator<Person>>(),
                                                     Substitute.For<IQueryProvider>(),
                                                     Substitute.For<IQuery<Person>>(),
                                                     CreateTableInfo())
            {
                new Person() { Id = 1, Name = "A", Age = 1 },
                new Person() { Id = 2, Name = "B", Age = 2 },
                new Person() { Id = 3, Name = "C", Age = 3 },
                new Person() { Id = 4, Name = "D", Age = 4 }
            };

            Assert.Equal(4, GetCount(dbSet.AddedItems));
        }

        private static TableInfo CreateTableInfo() =>
            new TableInfo(new List<ColumnInfo>(), new List<PropertyInfo>(), null);

        [Fact]
        public void AddItemsToPendingChanges()
        {
            IDbSet<Person> dbSet = new DbSet<Person>(Substitute.For<ICommandGenerator<Person>>(),
                                                     Substitute.For<IQueryProvider>(),
                                                     Substitute.For<IQuery<Person>>(),
                                                     CreateTableInfo());
            var people = new List<Person>
            {
                new Person() { Id = 1, Name = "A", Age = 1 },
                new Person() { Id = 2, Name = "B", Age = 2 },
                new Person() { Id = 3, Name = "C", Age = 3 },
                new Person() { Id = 4, Name = "D", Age = 4 }
            };

            dbSet.Add(people);

            Assert.Equal(4, GetCount(dbSet.AddedItems));
        }

        [Fact]
        public void EditItemToPendingChanges()
        {
            IDbSet<Person> dbSet = new DbSet<Person>(Substitute.For<ICommandGenerator<Person>>(),
                                                     Substitute.For<IQueryProvider>(),
                                                     Substitute.For<IQuery<Person>>(),
                                                     CreateTableInfo());

            dbSet.Edit(new Person() { Id = 1, Name = "A", Age = 1 });
            dbSet.Edit(new Person() { Id = 2, Name = "B", Age = 2 });
            dbSet.Edit(new Person() { Id = 3, Name = "C", Age = 3 });
            dbSet.Edit(new Person() { Id = 4, Name = "D", Age = 4 });

            Assert.Equal(4, GetCount(dbSet.EditedItems));
        }

        [Fact]
        public void EditItemsToPendingChanges()
        {
            IDbSet<Person> dbSet = new DbSet<Person>(Substitute.For<ICommandGenerator<Person>>(),
                                                     Substitute.For<IQueryProvider>(),
                                                     Substitute.For<IQuery<Person>>(),
                                                     CreateTableInfo());

            var people = new List<Person>
            {
                new Person() { Id = 1, Name = "A", Age = 1 },
                new Person() { Id = 2, Name = "B", Age = 2 },
                new Person() { Id = 3, Name = "C", Age = 3 },
                new Person() { Id = 4, Name = "D", Age = 4 }
            };

            dbSet.Edit(people);

            Assert.Equal(4, GetCount(dbSet.EditedItems));
        }

        [Fact]
        public void DeleteItemToPendingChanges()
        {
            IDbSet<Person> dbSet = new DbSet<Person>(Substitute.For<ICommandGenerator<Person>>(),
                                                     Substitute.For<IQueryProvider>(),
                                                     Substitute.For<IQuery<Person>>(),
                                                     CreateTableInfo());

            dbSet.Delete(new Person() { Id = 1, Name = "A", Age = 1 });
            dbSet.Delete(new Person() { Id = 2, Name = "B", Age = 2 });
            dbSet.Delete(new Person() { Id = 3, Name = "C", Age = 3 });
            dbSet.Delete(new Person() { Id = 4, Name = "D", Age = 4 });

            Assert.Equal(4, GetCount(dbSet.DeletedItems));
        }

        [Fact]
        public void DeleteItemsToPendingChanges()
        {
            IDbSet<Person> dbSet = new DbSet<Person>(Substitute.For<ICommandGenerator<Person>>(),
                                                     Substitute.For<IQueryProvider>(),
                                                     Substitute.For<IQuery<Person>>(),
                                                     CreateTableInfo());

            var people = new List<Person>
            {
                new Person() { Id = 1, Name = "A", Age = 1 },
                new Person() { Id = 2, Name = "B", Age = 2 },
                new Person() { Id = 3, Name = "C", Age = 3 },
                new Person() { Id = 4, Name = "D", Age = 4 }
            };

            dbSet.Delete(people);

            Assert.Equal(4, GetCount(dbSet.DeletedItems));
        }

        [Fact]
        public void ThrowAlreadyInCollectionExceptionWhenAddExistingItem()
        {
            IDbSet<Person> dbSet = new DbSet<Person>(Substitute.For<ICommandGenerator<Person>>(),
                                                     Substitute.For<IQueryProvider>(),
                                                     Substitute.For<IQuery<Person>>(),
                                                     CreateTableInfo());

            Person newPerson = new Person { Id = 1, Name = "A", Age = 1 };

            dbSet.Delete(newPerson);
            Action action = () => dbSet.Add(newPerson);
            Assert.Throws<AlreadyInCollectionException>(action);
        }

        [Fact]
        public void ThrowAlreadyInCollectionExceptionWhenEditingExistingItem()
        {
            IDbSet<Person> dbSet = new DbSet<Person>(Substitute.For<ICommandGenerator<Person>>(),
                                                     Substitute.For<IQueryProvider>(),
                                                     Substitute.For<IQuery<Person>>(),
                                                     CreateTableInfo());

            Person newPerson = new Person { Id = 1, Name = "A", Age = 1 };

            dbSet.Add(newPerson);
            Action action = () => dbSet.Edit(newPerson);
            Assert.Throws<AlreadyInCollectionException>(action);
        }

        [Fact]
        public void ThrowAlreadyInCollectionExceptionWhenDeletingExistingItem()
        {
            IDbSet<Person> dbSet = new DbSet<Person>(Substitute.For<ICommandGenerator<Person>>(),
                                                     Substitute.For<IQueryProvider>(),
                                                     Substitute.For<IQuery<Person>>(),
                                                     CreateTableInfo());

            Person newPerson = new Person { Id = 1, Name = "A", Age = 1 };

            dbSet.Edit(newPerson);
            Action action = () => dbSet.Delete(newPerson);

            Assert.Throws<AlreadyInCollectionException>(action);
        }

        [Fact]
        public void ClearPendingChanges()
        {
            IDbSet<Person> dbSet = new DbSet<Person>(Substitute.For<ICommandGenerator<Person>>(),
                                                     Substitute.For<IQueryProvider>(),
                                                     Substitute.For<IQuery<Person>>(),
                                                     CreateTableInfo())
            {
                new Person() { Id = 1, Name = "A", Age = 1 },
                new Person() { Id = 2, Name = "B", Age = 2 }
            };
            dbSet.Edit(new Person() { Id = 3, Name = "C", Age = 3 });
            dbSet.Edit(new Person() { Id = 4, Name = "D", Age = 4 });
            dbSet.Delete(new Person() { Id = 5, Name = "E", Age = 5 });
            dbSet.Delete(new Person() { Id = 6, Name = "F", Age = 6 });

            dbSet.Clear();

            Assert.Empty(dbSet.AddedItems);
            Assert.Empty(dbSet.EditedItems);
            Assert.Empty(dbSet.DeletedItems);
        }

        [Fact]
        public void CommitPendingChanges()
        {
            var commandGenerator = Substitute.For<ICommandGenerator<Person>>();
            var command = Substitute.For<DbCommand>();
            commandGenerator.GetInsertCommand().Returns(command);
            commandGenerator.GetUpdateCommand().Returns(command);
            commandGenerator.GetDeleteCommand().Returns(command);

            IDbSet<Person> dbSet = new DbSet<Person>(commandGenerator,
                                                     new FakeProvider(),
                                                     Substitute.For<IQuery<Person>>(),
                                                     CreateTableInfo())
            {
                new Person() { Id = 1, Name = "A", Age = 1 },
                new Person() { Id = 2, Name = "B", Age = 2 }
            };
            dbSet.Edit(new Person() { Id = 3, Name = "C", Age = 3 });
            dbSet.Edit(new Person() { Id = 4, Name = "D", Age = 4 });
            dbSet.Delete(new Person() { Id = 5, Name = "E", Age = 5 });
            dbSet.Delete(new Person() { Id = 6, Name = "F", Age = 6 });

            dbSet.CommitChanges();

            Assert.Empty(dbSet.AddedItems);
            Assert.Empty(dbSet.EditedItems);
            Assert.Empty(dbSet.DeletedItems);
        }

        [Fact]
        public void ThrowExceptionWhenProviderDoesNotSupportIdnetity()
        {
            var tableInfo = new TableInfo(new List<ColumnInfo>()
            {
                new ColumnInfo(){
                    Name = "Id",
                    IsPrimaryKey = true,
                    AutoIncrementMethodType = AutoIncrementMethodType.Identity
                }
            }, new List<PropertyInfo>(), null);

            IDbSet<Person> dbSet = new DbSet<Person>(Substitute.For<ICommandGenerator<Person>>(),
                                                     new FakeProvider(),
                                                     Substitute.For<IQuery<Person>>(),
                                                     tableInfo);

            dbSet.Add(new Person() { Name = "A" });

            Action action = () => dbSet.CommitChanges();

            var ex = Assert.Throws<InvalidOperationException>(action);
            Assert.Contains("FakeProvider", ex.Message);
            Assert.Contains("Person", ex.Message);
        }

        [Theory]
        [MemberData(nameof(GetDataForDeleteByIds))]
        public void ThrowExceptionWhenTryDeleteByIdsWithIncorrectType(IEnumerable<object> ids, bool throwException)
        {
            var tableInfo = new TableInfo(new List<ColumnInfo>()
            {
                new ColumnInfo(){
                    Name = "Id",
                    IsPrimaryKey = true,
                    PropertyInfo = typeof(Person).GetProperty(nameof(Person.Id))
                }
            }, new List<PropertyInfo>(), null);

            var dbSet = new DbSet<Person>(
                Substitute.For<ICommandGenerator<Person>>(),
                Substitute.For<IQueryProvider>(),
                Substitute.For<IQuery<Person>>(),
                tableInfo);

            Action action = () =>
            {
                foreach (object id in ids)
                {
                    dbSet.Delete(id);
                }
            };

            if (throwException)
            {
                var ex = Assert.Throws<ArgumentException>(action);
                Assert.Contains("System.Int32", ex.Message);
            }
            else
            {
                action();
            }
        }

        public static IEnumerable<object[]> GetDataForDeleteByIds()
        {
            yield return new object[] { new List<object>() { "1", 1 }, true };
            yield return new object[] { new List<object>() { 1, 2, 3, true }, true };
            yield return new object[] { new List<object>() { 1, 2, 3, 4 }, false };
        }

        #endregion

        #region Test classes

        public class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
        }

        private class FakeProvider : IQueryProvider
        {
            async Task IQueryProvider.ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken) => await action(cancellationToken);
            int IQueryProvider.ExecuteNonQueryCommand(IDbCommand command) => command.ExecuteNonQuery();
            bool IQueryProvider.SupportsIdentity() => false;
            bool IQueryProvider.SupportsPrepareCommand() => true;
            void IDisposable.Dispose() => throw new NotImplementedException();

            DbProviderFactory IQueryProvider.DbProviderFactory => throw new NotImplementedException();
            ITransaction IQueryProvider.BeginTransaction(IsolationLevel isolationLevel) => throw new NotImplementedException();
            IBulkInsert IQueryProvider.CreateBulkInsert(object options) => throw new NotImplementedException();
            IBulkUpdate IQueryProvider.CreateBulkUpdate() => throw new NotImplementedException();
            IIdGenerator IQueryProvider.CreateIdGenerator(string tableName, int batchSize) => throw new NotImplementedException();
            IIdGenerator IQueryProvider.CreateIdGenerator(Type dataType, string tableName, int batchSize) => throw new NotImplementedException();
            System.Linq.IQueryable System.Linq.IQueryProvider.CreateQuery(Expression expression) => throw new NotImplementedException();
            System.Linq.IQueryable<TElement> System.Linq.IQueryProvider.CreateQuery<TElement>(Expression expression) => throw new NotImplementedException();
            IEnumerable<T> IQueryProvider.Execute<T>(IQuery<T> query) => throw new NotImplementedException();
            object System.Linq.IQueryProvider.Execute(Expression expression) => throw new NotImplementedException();
            TResult System.Linq.IQueryProvider.Execute<TResult>(Expression expression) => throw new NotImplementedException();
            int IQueryProvider.ExecuteNonQuery(string query) => throw new NotImplementedException();
            int IQueryProvider.ExecuteNonQuery(string query, CommandParameterCollection parameters) => throw new NotImplementedException();
            Task<int> IQueryProvider.ExecuteNonQueryAsync(string query, CancellationToken cancellationToken) => throw new NotImplementedException();
            Task<int> IQueryProvider.ExecuteNonQueryAsync(string query, CancellationToken cancellationToken, params object[] paramValues) => throw new NotImplementedException();
            Task<int> IQueryProvider.ExecuteNonQueryAsync(string query, CommandParameterCollection parameters, CancellationToken cancellationToken) => throw new NotImplementedException();
            Task<int> IQueryProvider.ExecuteNonQueryCommandAsync(DbCommand command, CancellationToken cancellationToken) => throw new NotImplementedException();
            object IQueryProvider.ExecuteScalar<T>(IQuery<T> query) => throw new NotImplementedException();
            object IQueryProvider.ExecuteScalarCommand(IDbCommand command) => throw new NotImplementedException();
            Task<object> IQueryProvider.ExecuteScalarCommandAsync(DbCommand command, CancellationToken cancellationToken) => throw new NotImplementedException();
            TResult IQueryProvider.ExecuteStoredProcedure<TResult>(string storedProcedureName) => throw new NotImplementedException();
            TResult IQueryProvider.ExecuteStoredProcedure<TResult>(string storedProcedureName, CommandParameterCollection parameters) => throw new NotImplementedException();
            DbCommand IQueryProvider.GetCommandForCurrentTransaction() => throw new NotImplementedException();
            ISqlExpressionVisitor IQueryProvider.GetExpressionVisitor() => throw new NotImplementedException();
            IIdGeneratorsForDatabaseInit IQueryProvider.GetIdGeneratorsForDatabaseInit() => throw new NotImplementedException();
            void IQueryProvider.SetParameterDbType(DbParameter parameter, string tableName, string columnName) => throw new NotImplementedException();
        }

        #endregion
    }
}
