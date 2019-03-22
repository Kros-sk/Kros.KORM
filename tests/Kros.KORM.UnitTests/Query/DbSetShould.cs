using FluentAssertions;
using Kros.Data;
using Kros.Data.BulkActions;
using Kros.KORM.CommandGenerator;
using Kros.KORM.Data;
using Kros.KORM.Exceptions;
using Kros.KORM.Metadata;
using Kros.KORM.Query;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
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

            dbSet.AddedItems.Should().HaveCount(4);
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

            dbSet.AddedItems.Should().HaveCount(4);
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

            dbSet.EditedItems.Should().HaveCount(4);
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

            dbSet.EditedItems.Should().HaveCount(4);
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

            dbSet.DeletedItems.Should().HaveCount(4);
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

            dbSet.DeletedItems.Should().HaveCount(4);
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
            action.Should().Throw<AlreadyInCollectionException>();
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
            action.Should().Throw<AlreadyInCollectionException>();
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

            action.Should().Throw<AlreadyInCollectionException>();
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

            dbSet.AddedItems.Should().BeEmpty();
            dbSet.EditedItems.Should().BeEmpty();
            dbSet.DeletedItems.Should().BeEmpty();
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

            dbSet.AddedItems.Should().BeEmpty();
            dbSet.EditedItems.Should().BeEmpty();
            dbSet.DeletedItems.Should().BeEmpty();
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
            public DbProviderFactory DbProviderFactory => throw new NotImplementedException();

            public IBulkInsert CreateBulkInsert()
            {
                throw new NotImplementedException();
            }

            public IBulkUpdate CreateBulkUpdate() => throw new NotImplementedException();

            public IEnumerable<T> Execute<T>(IQuery<T> query)
            {
                throw new NotImplementedException();
            }

            public int ExecuteNonQuery(string query)
            {
                throw new NotImplementedException();
            }

            public int ExecuteNonQuery(string query, CommandParameterCollection parameters)
            {
                throw new NotImplementedException();
            }

            public int ExecuteNonQueryCommand(IDbCommand command)
            {
                return command.ExecuteNonQuery();
            }

            public object ExecuteScalar<T>(IQuery<T> query)
            {
                throw new NotImplementedException();
            }

            public System.Linq.IQueryable CreateQuery(Expression expression)
            {
                throw new NotImplementedException();
            }

            public System.Linq.IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            {
                throw new NotImplementedException();
            }

            public object Execute(Expression expression)
            {
                throw new NotImplementedException();
            }

            public TResult Execute<TResult>(Expression expression)
            {
                throw new NotImplementedException();
            }

            public DbConnection CreateConnection()
            {
                throw new NotImplementedException();
            }

            public TResult ExecuteStoredProcedure<TResult>(string storedProcedureNam)
            {
                throw new NotImplementedException();
            }

            public TResult ExecuteStoredProcedure<TResult>(string storedProcedureName, CommandParameterCollection parameters)
            {
                throw new NotImplementedException();
            }

            public DbCommand GetCommandForCurrentTransaction()
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
            }

            public void ExecuteInTransaction(Action action)
            {
                action();
            }

            public ITransaction BeginTransaction(IsolationLevel isolationLevel)
            {
                throw new NotImplementedException();
            }

            public void SetParameterDbType(DbParameter parameter, string tableName, string columnName)
            {
                throw new NotImplementedException();
            }

            public IIdGenerator CreateIdGenerator(string tableName, int batchSize)
            {
                throw new NotImplementedException();
            }

            public async Task ExecuteInTransactionAsync(Func<Task> action) => await action();

            public Task<int> ExecuteNonQueryCommandAsync(DbCommand command)
            {
                throw new NotImplementedException();
            }

            public bool SupportsPrepareCommand() => true;

            public Task<int> ExecuteNonQueryAsync(string query)
            {
                throw new NotImplementedException();
            }

            public Task<int> ExecuteNonQueryAsync(string query, CommandParameterCollection parameters)
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}