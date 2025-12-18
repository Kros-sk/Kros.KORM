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
    internal sealed class Query<T> : IQuery<T>, IFilteredQuery<T>, IGroupedQuery<T>, IOrderedQuery<T>, IQueryBaseInternal
    {
        private const string DefaultQueryFilterParameterNamePrefix = "__Dqf";

        #region Private fields

        private readonly IDatabaseMapper _databaseMapper;
        private readonly IQueryProvider _provider;
        private bool _ignoreQueryFilters = false;

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

            Expression = SelectExpression.Constant(_databaseMapper.GetTableInfo<T>(), this);
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
            Expression = expression;
        }

        #endregion

        /// <inheritdoc />
        public ISelectionQuery<T> IgnoreQueryFilters()
        {
            _ignoreQueryFilters = true;

            return this;
        }

        /// <inheritdoc />
        public IQueryBase<T> Sql(RawSqlString sql, params object[] args)
        {
            Check.NotNullOrWhiteSpace(sql.Format, nameof(sql));

            Expression = new SqlExpression(sql, args);

            return this;
        }

        /// <inheritdoc/>
        public IQueryBase<T> Sql(FormattableString sql) => Sql(sql, sql.GetArguments());

        /// <inheritdoc />
        public IFromQuery<T> Select(string selectPart)
        {
            Check.NotNullOrWhiteSpace(selectPart, nameof(selectPart));

            SelectExpression.SetColumnsExpression(new ColumnsExpression(selectPart));

            return this;
        }

        /// <inheritdoc />
        public IFromQuery<T> Select(params string[] columns)
        {
            Check.NotNull(columns, nameof(columns));

            SelectExpression.SetColumnsExpression(new ColumnsExpression(columns));

            return this;
        }

        /// <inheritdoc />
        public IFromQuery<T> Select<TResult>(Func<T, TResult> selector)
        {
            Check.NotNull(selector, nameof(selector));

            SelectExpression.SetColumnsExpression(ColumnsExpression.Create(selector,
                _databaseMapper.GetTableInfo<T>()));

            return this;
        }

        /// <inheritdoc />
        public IProjectionQuery<T> From(string table)
        {
            Check.NotNullOrWhiteSpace(table, nameof(table));

            SelectExpression.SetTableExpression(new TableExpression(table));

            return this;
        }

        /// <inheritdoc />
        public IFilteredQuery<T> Where(RawSqlString whereCondition, params object[] args)
        {
            Check.NotNullOrWhiteSpace(whereCondition.Format, nameof(whereCondition));

            SelectExpression.SetWhereExpression(new WhereExpression(whereCondition, args));

            return this;
        }

        /// <inheritdoc />
        public IFilteredQuery<T> Where(FormattableString whereCondition)
            => Where(whereCondition, whereCondition.GetArguments());

        /// <inheritdoc />
        public T FirstOrDefault(RawSqlString whereCondition, params object[] args)
        {
            Check.NotNullOrWhiteSpace(whereCondition.Format, nameof(whereCondition));

            SelectExpression.SetWhereExpression(new WhereExpression(whereCondition, args));

            return this.AsEnumerable().FirstOrDefault();
        }

        /// <inheritdoc />
        public T FirstOrDefault(FormattableString whereCondition)
            => FirstOrDefault(whereCondition, whereCondition.GetArguments());

        /// <inheritdoc />
        public bool Any(RawSqlString whereCondition, params object[] args)
        {
            Check.NotNullOrWhiteSpace(whereCondition.Format, nameof(whereCondition));
            const string top = "TOP 1 1";

            SelectExpression.SetColumnsExpression(new ColumnsExpression(top));
            SelectExpression.SetWhereExpression(new WhereExpression(whereCondition, args));

            return _provider.ExecuteScalar<T>(this) != null;
        }

        /// <inheritdoc />
        public bool Any(FormattableString whereCondition) => Any(whereCondition, whereCondition.GetArguments());

        /// <inheritdoc />
        public IOrderedQuery<T> OrderBy(string orderBy)
        {
            Check.NotNullOrWhiteSpace(orderBy, nameof(orderBy));

            SelectExpression.SetOrderByExpression(new OrderByExpression(orderBy));

            return this;
        }

        /// <inheritdoc />
        public IGroupedQuery<T> GroupBy(string groupBy)
        {
            Check.NotNullOrWhiteSpace(groupBy, nameof(groupBy));

            SelectExpression.SetGroupByExpression(new GroupByExpression(groupBy));

            return this;
        }

        private SelectExpression SelectExpression
            => (Expression as SelectExpression) ?? (Expression as MethodCallExpression).FindSelectExpression();

        #region IQueryBase

        /// <inheritdoc />
        public Expression Expression { get; private set; }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator() => _provider.Execute<T>(this).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public object ExecuteScalar() => _provider.ExecuteScalar(this);

        /// <inheritdoc />
        public string ExecuteStringScalar()
        {
            var value = ExecuteScalar();

            if (value is DBNull || value == null)
            {
                return null;
            }
            else
            {
                return value.ToString();
            }
        }

        /// <inheritdoc />
        public TRet? ExecuteScalar<TRet>() where TRet : struct
        {
            var value = ExecuteScalar();

            return !(value is DBNull || value == null) ? (TRet)value : new TRet?();
        }

        /// <inheritdoc />
        public IDbSet<T> AsDbSet() =>
            new DbSet<T>(
                new CommandGenerator<T>(_databaseMapper.GetTableInfo<T>(), _provider, this),
                _provider, this, _databaseMapper.GetTableInfo<T>());

        void IQueryBaseInternal.ApplyQueryFilter(IDatabaseMapper databaseMapper, ISqlExpressionVisitor expressionVisitor)
        {
            SelectExpression select = SelectExpression;
            IQueryBaseInternal query = select?.OriginalQuery;

            if (query != null && !query.IgnoreQueryFilters)
            {
                TableInfo tableInfo = databaseMapper.GetTableInfo(select.EntityType);
                if (tableInfo.QueryFilter != null)
                {
                    WhereExpression queryFilter =
                        expressionVisitor.GenerateWhereCondition(tableInfo.QueryFilter, DefaultQueryFilterParameterNamePrefix);
                    select.SetWhereExpression(queryFilter);
                }
            }
        }

        bool IQueryBaseInternal.IgnoreQueryFilters => _ignoreQueryFilters;

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
