using FluentAssertions;
using Kros.Data.BulkActions;
using Kros.Data.Schema;
using Kros.KORM.Helper;
using Kros.KORM.Materializer;
using Kros.KORM.Metadata;
using Kros.KORM.Metadata.Attribute;
using Kros.KORM.Query;
using Kros.KORM.Query.Expressions;
using Kros.KORM.Query.Providers;
using Kros.KORM.Query.Sql;
using Microsoft.Data.SqlClient;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;

namespace Kros.KORM.UnitTests.Query.Sql
{
    /// <summary>
    /// Base class for Linq translation tests
    /// </summary>
    public abstract class LinqTranslatorTestBase
    {
        #region Nested classes

        public interface IModel
        {
            int Id { get; set; }
        }

        [Alias("People")]
        public class Person : IModel
        {
            public int Id { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }

            [Alias("PostAddress")]
            public string Address { get; set; }
        }

        #endregion

        private DatabaseMapper _databaseMapper;

        /// <summary>
        /// Create query for testing.
        /// </summary>
        /// <typeparam name="T">Model type</typeparam>
        /// <returns>Query for testing.</returns>
        public virtual IQuery<T> Query<T>()
            => CreateQuery<T>(Delimiters.Empty);

        /// <summary>
        /// Create query for testing.
        /// </summary>
        /// <typeparam name="T">Model type</typeparam>
        /// <returns>Query for testing.</returns>
        public virtual IQuery<T> QueryWithQuotas<T>()
            => CreateQuery<T>(Delimiters.SquareBrackets);

        private IQuery<T> CreateQuery<T>(Delimiters quota)
        {
            var modelMapper = new ConventionModelMapper();
            (modelMapper as IModelMapperInternal).UseIdentifierDelimiters(quota);
            _databaseMapper = new DatabaseMapper(modelMapper);

            return Database
                .Builder
                .UseConnection(new SqlConnection())
                .UseQueryProviderFactory(new FakeQueryProviderFactory())
                .UseDatabaseConfiguration(new DatabaseConfiguration(quota))
                .Build().Query<T>();
        }

        /// <summary>
        /// Create visitor for translate query to SQL.
        /// </summary>
        protected virtual ISqlExpressionVisitor CreateVisitor()
            => new DefaultQuerySqlGenerator(_databaseMapper ?? Database.DatabaseMapper);

        /// <summary>
        /// Query should be equal to <paramref name="expectedSql"/>.
        /// </summary>
        /// <typeparam name="T">Model type.</typeparam>
        /// <param name="value">Testing query.</param>
        /// <param name="expectedSql">Expected sql query.</param>
        protected void AreSame<T>(IQueryable<T> value, string expectedSql, params object[] parameters)
            => AreSame(value.Expression, new QueryInfo(expectedSql), parameters);

        protected void AreSame<T>(IQueryable<T> value, QueryInfo sql, params object[] parameters)
            => AreSame(value.Expression, sql, parameters);

        private void AreSame(Expression expression, QueryInfo expectedQuery, object[] parameters)
        {
            var visitor = CreateVisitor();
            QueryInfo sql = visitor.GenerateSql(expression);

            sql.Query.Should().Be(expectedQuery.Query);
            CompareLimitOffsetDataReaders(sql.Reader as LimitOffsetDataReader, expectedQuery.Reader as LimitOffsetDataReader)
                .Should().BeTrue();
            parameters?.Should().BeEquivalentTo(ParameterExtractor.ExtractParameters(expression), o => o.WithStrictOrdering());
        }

        /// <summary>
        /// Was generated the same SQL.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">The value.</param>
        /// <param name="expectedSql">The expected SQL.</param>
        /// <param name="parameters">The parameters.</param>
        protected virtual void WasGeneratedSameSql<T>(IQuery<T> value, string expectedSql, params object[] parameters)
        {
            var provider = value.Provider as FakeQueryProvider;

            AreSame(provider.LastExpression.Expression, new QueryInfo(expectedSql), parameters);
        }

        private bool CompareLimitOffsetDataReaders(LimitOffsetDataReader reader1, LimitOffsetDataReader reader2)
        {
            if (reader1 == null)
            {
                return reader2 == null;
            }
            return (reader1.Limit == reader2.Limit) && (reader1.Offset == reader2.Offset);
        }

        private class ParameterExtractor : ExpressionVisitor
        {
            private List<object> _parameters;

            private ParameterExtractor(List<object> parameters)
            {
                _parameters = parameters;
            }

            public static IEnumerable<object> ExtractParameters(Expression expression)
            {
                var ret = new List<object>();

                (new ParameterExtractor(ret)).Visit(expression);

                return ret;
            }

            public override Expression Visit(Expression node)
            {
                if (node is ArgsExpression expression)
                {
                    VisitArgs(expression);
                    return node;
                }
                else if (node != null)
                {
                    return base.Visit(node.Reduce());
                }
                else
                {
                    return node;
                }
            }

            private void VisitArgs(ArgsExpression argsExpression)
            {
                if (argsExpression.Parameters?.Count() > 0)
                {
                    _parameters.AddRange(argsExpression.Parameters);
                }
            }
        }

        public class FakeQueryProvider : QueryProvider
        {
            private readonly DbConnection _sqlConnection;
            private readonly ISqlExpressionVisitorFactory _visitorFactory;

            public FakeQueryProvider(
                DbConnection sqlConnection,
                ISqlExpressionVisitorFactory visitorFactory,
                IDatabaseMapper databaseMapper)
                : base(sqlConnection,
                     visitorFactory,
                     Substitute.For<IModelBuilder>(),
                     Substitute.For<ILogger>(),
                     databaseMapper)
            {
                _sqlConnection = sqlConnection;
                _visitorFactory = visitorFactory;
            }

            /// <summary>
            /// Gets the last generated SQL.
            /// </summary>
            public IQueryable LastExpression { get; set; }

            public override DbProviderFactory DbProviderFactory => throw new NotImplementedException();

            public override IBulkInsert CreateBulkInsert(object options) => throw new NotImplementedException();

            public override IBulkUpdate CreateBulkUpdate() => throw new NotImplementedException();

            public override IEnumerable<T> Execute<T>(IQuery<T> query)
            {
                if (LastExpression is null)
                {
                    SetQueryFilter(query, _visitorFactory.CreateVisitor(_sqlConnection));
                    LastExpression = query;
                }

                return Enumerable.Empty<T>();
            }

            protected override IDatabaseSchemaLoader GetSchemaLoader()
            {
                throw new NotImplementedException();
            }
        }

        public class FakeQueryProviderFactory : IQueryProviderFactory
        {
            public KORM.Query.IQueryProvider Create(DbConnection connection, IModelBuilder modelBuilder, IDatabaseMapper databaseMapper)
                => new FakeQueryProvider(connection, new FakeSqlServerSqlExpressionVisitorFactory(databaseMapper), databaseMapper);

            public KORM.Query.IQueryProvider Create(KormConnectionSettings connectionString, IModelBuilder modelBuilder, IDatabaseMapper databaseMapper)
            {
                throw new NotImplementedException();
            }
        }

        public class FakeSqlServerSqlExpressionVisitorFactory : ISqlExpressionVisitorFactory
        {
            private readonly IDatabaseMapper _databaseMapper;

            /// <summary>
            /// Creates an instance with specified database mapper <paramref name="databaseMapper"/>.
            /// </summary>
            /// <param name="databaseMapper">Database mapper.</param>
            public FakeSqlServerSqlExpressionVisitorFactory(IDatabaseMapper databaseMapper)
            {
                _databaseMapper = databaseMapper;
            }

            public ISqlExpressionVisitor CreateVisitor(IDbConnection connection)
                => new SqlServer2012SqlGenerator(_databaseMapper);
        }

        public class DatabaseConfiguration : DatabaseConfigurationBase
        {
            private readonly Delimiters _quota;

            public DatabaseConfiguration(Delimiters quota)
            {
                _quota = quota;
            }

            public override void OnModelCreating(ModelConfigurationBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                modelBuilder.UseIdentifierDelimiters(_quota);
            }
        }
    }
}
