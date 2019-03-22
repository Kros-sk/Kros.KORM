using Kros.KORM.CommandGenerator;
using Kros.KORM.Metadata;
using Kros.KORM.Query.Expressions;
using Kros.KORM.Query.Sql;
using Kros.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Kros.KORM.Query
{
    /// <summary>
    /// Query builder.
    /// </summary>
    /// <typeparam name="T">Class of model.</typeparam>
    /// <seealso cref="Kros.KORM.Query.IQuery{T}" />
    internal sealed class Query<T> : IQuery<T>, IFilteredQuery<T>, IGroupedQuery<T>, IOrderedQuery<T>
    {
        #region Private fields

        private IDatabaseMapper _databaseMapper;
        private IQueryProvider _provider;
        private Expression _expression;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Query{T}"/> class.
        /// </summary>
        /// <param name="databaseMapper">The database mapper.</param>
        /// <param name="provider">The provider for executing query.</param>
        internal Query(IDatabaseMapper databaseMapper, IQueryProvider provider)
        {
            Check.NotNull(provider, nameof(provider));
            Check.NotNull(databaseMapper, nameof(databaseMapper));

            _databaseMapper = databaseMapper;
            _provider = provider;

            _expression = SelectExpression.Constant(_databaseMapper.GetTableInfo<T>(), this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Query{T}"/> class.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="expression">The expression.</param>
        internal Query(IQueryProvider provider, Expression expression)
        {
            Check.NotNull(provider, nameof(provider));

            _provider = provider;
            _expression = expression;
        }

        #endregion

        /// <summary>
        /// Create query from sql statement.
        /// </summary>
        /// <param name="sql">The SQL for executing in server.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>
        /// Query from sql.
        /// </returns>
        /// <remarks>
        /// Sql must be server specific. Because no translation is provide.
        /// </remarks>
        /// <exception cref="ArgumentNullException">if <c>sql</c> is null or white string.</exception>
        public IQueryBase<T> Sql(RawSqlString sql, params object[] args)
        {
            Check.NotNullOrWhiteSpace(sql.Format, nameof(sql));

            this.Expression = new SqlExpression(sql, args);

            return this;
        }

        /// <inheritdoc/>
        public IQueryBase<T> Sql(FormattableString sql) => Sql(sql, sql.GetArguments());

        /// <summary>
        /// Create query from sql statement.
        /// </summary>
        /// <param name="selectPart"></param>
        /// <returns>
        /// Query from sql.
        /// </returns>
        /// <exception cref="ArgumentNullException">if <c>sql</c> is null or white string.</exception>
        /// <remarks>
        /// Sql must be server specific. Because no translation is provide.
        /// </remarks>
        public IQuery<T> Select(string selectPart)
        {
            Check.NotNullOrWhiteSpace(selectPart, nameof(selectPart));

            this.SelectExpression.SetColumnsExpression(new ColumnsExpression(selectPart));

            return this;
        }

        /// <summary>
        /// Add columns to sql.
        /// </summary>
        /// <param name="columns">The columns for select clausule.</param>
        /// <returns>
        /// Query for enumerable models.
        /// </returns>
        /// <remarks>
        ///  When Select method is not call, query take columns by T model.
        /// </remarks>
        public IQuery<T> Select(params string[] columns)
        {
            Check.NotNull(columns, nameof(columns));

            this.SelectExpression.SetColumnsExpression(new ColumnsExpression(columns));

            return this;
        }

        /// <summary>
        /// Add select part to sql.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="selector">The selector.</param>
        /// <returns>
        /// Query for enumerable models.
        /// </returns>
        /// <exception cref="ArgumentNullException">if <c>sqlPart</c> is null or white string.</exception>
        /// <remarks>
        /// When <c>Select</c> method is not call, query take columns by T model.
        /// </remarks>
        public IQuery<T> Select<TResult>(Func<T, TResult> selector)
        {
            Check.NotNull(selector, nameof(selector));

            this.SelectExpression.SetColumnsExpression(ColumnsExpression.Create(selector,
                _databaseMapper.GetTableInfo<T>()));

            return this;
        }

        /// <summary>
        /// Add FROM part to sql query.
        /// </summary>
        /// <param name="table">Table name or join.</param>
        /// <returns>
        /// Query for enumerable models.
        /// </returns>
        /// <remarks>
        /// When <c>From</c> method is not call, query take table by T model type.
        /// </remarks>
        /// <exception cref="ArgumentNullException">if <c>table</c> is null or white string.</exception>
        public IProjectionQuery<T> From(string table)
        {
            Check.NotNullOrWhiteSpace(table, nameof(table));

            this.SelectExpression.SetTableExpression(new TableExpression(table));

            return this;
        }

        /// <summary>
        /// Add where condition to sql.
        /// </summary>
        /// <param name="whereCondition">The where condition.</param>
        /// <param name="args">The arguments for where.</param>
        /// <returns>
        /// Query for enumerable models.
        /// </returns>
        /// <exception cref="ArgumentNullException">if <c>whereCondition</c> is null or white string.</exception>
        public IFilteredQuery<T> Where(RawSqlString whereCondition, params object[] args)
        {
            Check.NotNullOrWhiteSpace(whereCondition.Format, nameof(whereCondition));

            this.SelectExpression.SetWhereExpression(new WhereExpression(whereCondition, args));

            return this;
        }

        /// <inheritdoc />
        public IFilteredQuery<T> Where(FormattableString whereCondition) =>
            Where(whereCondition, whereCondition.GetArguments());

        /// <summary>
        /// Returns the first item of which match where condition, or a default value if item doesn't exist.
        /// </summary>
        /// <param name="whereCondition">The where condition.</param>
        /// <param name="args">The arguments for where.</param>
        /// <returns>
        /// <see langword="null"/> if item doesn't exist; otherwise, the first item which match the condition.
        /// </returns>
        /// <exception cref="ArgumentNullException">if <c>whereCondition</c> is null or white string.</exception>
        public T FirstOrDefault(RawSqlString whereCondition, params object[] args)
        {
            Check.NotNullOrWhiteSpace(whereCondition.Format, nameof(whereCondition));

            this.SelectExpression.SetWhereExpression(new WhereExpression(whereCondition, args));

            return this.AsEnumerable().FirstOrDefault();
        }

        /// <inheritdoc />
        public T FirstOrDefault(FormattableString whereCondition) =>
            FirstOrDefault(whereCondition, whereCondition.GetArguments());

        /// <summary>
        /// Check if exist elements in the table which match condition; otherwise, false.
        /// </summary>
        /// <param name="whereCondition">The where condition.</param>
        /// <param name="args">The arguments for where.</param>
        /// <returns>
        /// <see langword="true"/> if exist elements in the table which match condition; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">if <c>whereCondition</c> is null or white string.</exception>
        /// <example>
        ///   <code source="..\..\..\..\Documentation\Examples\Kros.KORM.Examples\IQueryExample.cs" title="Any" region="Any" language="cs" />
        /// </example>
        public bool Any(RawSqlString whereCondition, params object[] args)
        {
            Check.NotNullOrWhiteSpace(whereCondition.Format, nameof(whereCondition));
            const string top = "TOP 1 1";

            this.SelectExpression.SetColumnsExpression(new ColumnsExpression(top));
            this.SelectExpression.SetWhereExpression(new WhereExpression(whereCondition, args));

            return _provider.ExecuteScalar<T>(this) != null;
        }

        /// <inheritdoc />
        public bool Any(FormattableString whereCondition) =>
            Any(whereCondition, whereCondition.GetArguments());

        /// <summary>
        /// Add order by statement to sql.
        /// </summary>
        /// <param name="orderBy">The order by statement.</param>
        /// <returns>
        /// Query for enumerable models.
        /// </returns>
        /// <exception cref="ArgumentNullException">if <c>orderBy</c> is null or white string.</exception>
        public IOrderedQuery<T> OrderBy(string orderBy)
        {
            Check.NotNullOrWhiteSpace(orderBy, nameof(orderBy));

            this.SelectExpression.SetOrderByExpression(new OrderByExpression(orderBy));

            return this;
        }

        /// <summary>
        /// Add group by statement to sql query.
        /// </summary>
        /// <param name="groupBy">The group by statement.</param>
        /// <returns>
        /// Query for enumerable models.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IGroupedQuery<T> GroupBy(string groupBy)
        {
            Check.NotNullOrWhiteSpace(groupBy, nameof(groupBy));

            this.SelectExpression.SetGroupByExpression(new GroupByExpression(groupBy));

            return this;
        }

        private SelectExpression SelectExpression => this.Expression as SelectExpression;

        #region IQueryBase

        /// <summary>
        /// Property represent expression for this query.
        /// </summary>
        /// <remarks>
        /// This property is used for genereting sql query by IQueryProvider.
        /// </remarks>
        public Expression Expression
        {
            get
            {
                return _expression;
            }
            private set
            {
                _expression = value;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<T> GetEnumerator() => _provider.Execute<T>(this).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result set returned by the query.
        /// Additional columns or rows are ignored.
        /// </summary>
        /// <returns>
        /// The first column of the first row in the result set, or <see langword="null"/>
        /// if the result set is empty. Returns a maximum of 2033 characters.
        /// </returns>
        /// <example>
        /// <code
        ///   source="..\..\..\..\Documentation\Examples\Kros.KORM.Examples\IQueryExample.cs"
        ///   title="Execute string scalar"
        ///   region="ExecuteScalar"
        ///   language="cs" />
        /// </example>
        public object ExecuteScalar() => _provider.ExecuteScalar(this);

        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result set returned by the query.
        /// Additional columns or rows are ignored.
        /// </summary>
        /// <returns>
        /// The first column of the first row in the result set as string, or <see langword="null"/>
        /// if the result set is empty. Returns a maximum of 2033 characters.
        /// </returns>
        /// <example>
        /// <code
        ///   source="..\..\..\..\Documentation\Examples\Kros.KORM.Examples\IQueryExample.cs"
        ///   title="Execute string scalar"
        ///   region="ExecuteStringScalar"
        ///   language="cs" />
        /// </example>
        public string ExecuteStringScalar()
        {
            var value = this.ExecuteScalar();

            if (value is DBNull || value == null)
            {
                return null;
            }
            else
            {
                return value.ToString();
            }
        }

        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result set returned by the query.
        /// Additional columns or rows are ignored.
        /// </summary>
        /// <typeparam name="TRet">Return type.</typeparam>
        /// <returns>
        /// The first column of the first row in the result set as nullable type of TRet. If the result set is empty,
        /// then HasValue is false.
        /// Returns a maximum of 2033 characters.
        /// </returns>
        /// <example>
        /// <code
        ///   source="..\..\..\..\Documentation\Examples\Kros.KORM.Examples\IQueryExample.cs"
        ///   title="Execute generic scalar"
        ///   region="ExecuteScalarGeneric"
        ///   language="cs" />
        /// </example>
        public TRet? ExecuteScalar<TRet>() where TRet : struct
        {
            var value = this.ExecuteScalar();

            return !(value is DBNull || value == null) ? (TRet)value : new TRet?();
        }

        /// <summary>
        /// Returns the collection of all entities that can be queried from the database.
        /// </summary>
        /// <returns><seealso cref="DbSet{T}"/>.</returns>
        public IDbSet<T> AsDbSet() =>
            new DbSet<T>(
                new CommandGenerator<T>(_databaseMapper.GetTableInfo<T>(), _provider, this),
                _provider, this, _databaseMapper.GetTableInfo<T>());

        #endregion

        #region Linq

        /// <summary>
        /// Gets the type of the element(s) that are returned when the expression tree associated
        /// with this instance of <see cref="System.Linq.IQueryable"/> is executed.
        /// </summary>
        public Type ElementType => typeof(T);

        /// <summary>
        /// Gets the query provider that is associated with this data source.
        /// </summary>
        public System.Linq.IQueryProvider Provider => _provider;

        #endregion
    }
}