using Kros.KORM.Converter;
using Kros.KORM.Metadata;
using Kros.KORM.Properties;
using Kros.KORM.Query;
using Kros.KORM.Query.Expressions;
using Kros.Utils;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace Kros.KORM.CommandGenerator
{
    /// <summary>
    /// Generates single-table commands that are used to commit changes made to a DbSet with the associated database.
    /// </summary>
    /// <typeparam name="T">Type class of model.</typeparam>
    internal class CommandGenerator<T> : ICommandGenerator<T>
    {
        #region Constants

        private const string INSERT_QUERY_BASE = "INSERT INTO [{0}] ({1}) VALUES ({2})";
        private const string UPDATE_QUERY_BASE = "UPDATE [{0}] SET {1} WHERE {2}";
        private const string DELETE_QUERY_BASE = "DELETE FROM [{0}] WHERE {1}";
        private const string DELETE_QUERY_BASE_IN = "DELETE FROM [{0}] WHERE [{1}] IN (";
        private const int DEFAULT_MAX_PARAMETERS_FOR_DELETE_COMMANDS_IN_PART = 100;

        #endregion

        #region Private Fields

        private TableInfo _tableInfo;
        private KORM.Query.IQueryProvider _provider;
        private IQueryBase<T> _query;
        private List<ColumnInfo> _columnsInfo = null;
        private int _maxParametersForDeleteCommandsInPart = DEFAULT_MAX_PARAMETERS_FOR_DELETE_COMMANDS_IN_PART;

        #endregion

        #region Public Fields

        /// <summary>
        /// Maximum parameters for delete command in IN part.
        /// </summary>
        public int MaxParametersForDeleteCommandsInPart
        {
            get
            {
                return _maxParametersForDeleteCommandsInPart;
            }
            set
            {
                _maxParametersForDeleteCommandsInPart = value < 1 ? 1 : value;
            }
        }

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandGenerator{T}" /> class.
        /// </summary>
        /// <param name="tableInfo">Information about table from database.</param>
        /// <param name="provider">Provider, that can execute queries.</param>
        /// <param name="query">Executing query.</param>
        public CommandGenerator(TableInfo tableInfo, KORM.Query.IQueryProvider provider, IQueryBase<T> query)
        {
            Check.NotNull(tableInfo, nameof(tableInfo));
            Check.NotNull(provider, nameof(provider));
            Check.NotNull(query, nameof(query));

            _tableInfo = tableInfo;
            _provider = provider;
            _query = query;
        }

        #endregion

        #region ICommandGenerator

        /// <summary>
        /// Gets the automatically generated DbCommand object required to perform insertions on the database.
        /// </summary>
        /// <returns>Insert command.</returns>
        public DbCommand GetInsertCommand()
        {
            var columns = GetQueryColumns();
            var cmd = _provider.GetCommandForCurrentTransaction();
            AddParametersToCommand(cmd, columns);
            cmd.CommandText = GetInsertCommandText(columns);
            return cmd;
        }

        /// <summary>
        /// Gets the automatically generated DbCommand object required to perform updates on the database
        /// </summary>
        /// <exception cref="Exceptions.MissingPrimaryKeyException">Table does not have primary key.</exception>
        /// <returns>Update command.</returns>
        public DbCommand GetUpdateCommand()
        {
            CheckPrimaryKeyExist(string.Format(Resources.MethodNotSupportedWhenNoPrimaryKey, nameof(GetUpdateCommand)));

            var columns = GetQueryColumns();
            var cmd = _provider.GetCommandForCurrentTransaction();
            AddParametersToCommand(cmd, columns.Where(x => !x.IsPrimaryKey));
            AddParametersToCommand(cmd, columns.Where(x => x.IsPrimaryKey));
            cmd.CommandText = GetUpdateCommandText(columns);
            return cmd;
        }

        /// <summary>
        /// Gets the automatically generated DbCommand object required to perform deletions on the database.
        /// </summary>
        /// <exception cref="Exceptions.MissingPrimaryKeyException">Table does not have primary key.</exception>
        /// <returns>Delete command.</returns>
        public DbCommand GetDeleteCommand()
        {
            CheckPrimaryKeyExist(string.Format(Resources.MethodNotSupportedWhenNoPrimaryKey, nameof(GetDeleteCommand)));

            var columns = _tableInfo.PrimaryKey;
            var cmd = _provider.GetCommandForCurrentTransaction();
            AddParametersToCommand(cmd, columns);
            cmd.CommandText = GetDeleteCommandText(columns);
            return cmd;
        }

        /// <summary>
        /// Gets the automatically generated DbCommands object required to perform deletions on the database.
        /// </summary>
        /// <param name="items">Type class of model collection.</param>
        /// <exception cref="Exceptions.MissingPrimaryKeyException">Table does not have primary key.</exception>
        /// <exception cref="Exceptions.CompositePrimaryKeyException">Table has composite primary key.</exception>
        /// <returns>Delete command collection.</returns>
        public IEnumerable<DbCommand> GetDeleteCommands(IEnumerable<T> items)
        {
            CheckPrimaryKeyExist(string.Format(Resources.MethodNotSupportedWhenNoPrimaryKey, nameof(GetDeleteCommands)));

            if (_tableInfo.PrimaryKey.Count() > 1)
            {
                throw new Exceptions.CompositePrimaryKeyException(
                    string.Format(Resources.MethodNotSupportedForCompositePrimaryKey, nameof(GetDeleteCommands)),
                    _tableInfo.Name);
            }

            List<DbCommand> retVal = new List<DbCommand>();
            ColumnInfo colInfo = _tableInfo.PrimaryKey.First();
            DbCommand cmd = null;
            StringBuilder deleteQueryText = new StringBuilder();
            int iterationCount = 0;

            foreach (T item in items)
            {
                if (iterationCount == 0)
                {
                    cmd = _provider.GetCommandForCurrentTransaction();
                    deleteQueryText.Clear();
                    deleteQueryText.AppendFormat(DELETE_QUERY_BASE_IN, _tableInfo.Name, colInfo.Name);
                }

                iterationCount++;
                string paramterName = $"@P{iterationCount}";
                AddDeleteCommandParameter(cmd, paramterName, GetColumnValue(colInfo, item));
                if (iterationCount > 1)
                {
                    deleteQueryText.Append(",");
                }
                deleteQueryText.Append(paramterName);

                if (iterationCount == MaxParametersForDeleteCommandsInPart)
                {
                    retVal.Add(FinishDeleteCommand(cmd, deleteQueryText));
                    iterationCount = 0;
                }
            }

            if (iterationCount > 0)
            {
                retVal.Add(FinishDeleteCommand(cmd, deleteQueryText));
            }

            return retVal;
        }

        /// <summary>
        /// Fills command's parameters with values from <paramref name="item" />.
        /// </summary>
        /// <param name="command">Command which parameters are filled.</param>
        /// <param name="item">Item, from which command is filled.</param>
        /// <exception cref="System.ArgumentNullException">Either <paramref name="command" /> or <paramref name="item" />
        /// is <see langword="null"/>.</exception>
        public void FillCommand(DbCommand command, T item)
        {
            Check.NotNull(command, nameof(command));
            Check.NotNull(item, nameof(item));

            foreach (ColumnInfo colInfo in this.GetQueryColumns())
            {
                string paramName = $"@{colInfo.Name}";
                if (command.Parameters.Contains(paramName))
                {
                    DbParameter parameter = command.Parameters[paramName];
                    var val = GetColumnValue(colInfo, item);
                    parameter.Value = val ?? System.DBNull.Value;
                }
            }
        }

        private void CheckPrimaryKeyExist(string message)
        {
            if (_tableInfo.PrimaryKey.Count() == 0)
            {
                throw new Exceptions.MissingPrimaryKeyException(message, _tableInfo.Name);
            }
        }

        private void AddDeleteCommandParameter(DbCommand cmd, string parameterName, object value)
        {
            DbParameter newParameter = cmd.CreateParameter();
            newParameter.ParameterName = parameterName;
            newParameter.Value = value;
            cmd.Parameters.Add(newParameter);
        }

        private DbCommand FinishDeleteCommand(DbCommand cmd, StringBuilder deleteQueryText)
        {
            deleteQueryText.Append(")");
            cmd.CommandText = deleteQueryText.ToString();
            return cmd;
        }

        #endregion

        #region Private Helpers

        /// <inheritdoc/>
        public IEnumerable<ColumnInfo> GetQueryColumns()
        {
            if (_columnsInfo == null)
            {
                _columnsInfo = new List<ColumnInfo>();
                var expression = (_query.Expression as SelectExpression);
                var columns = expression.ColumnsExpression.ColumnsPart.Split(',');

                foreach (var column in columns)
                {
                    var columnInfo = _tableInfo.Columns.Where(p => p.Name == column.Trim()).FirstOrDefault();

                    if (columnInfo != null)
                    {
                        _columnsInfo.Add(columnInfo);
                    }
                }
            }

            return _columnsInfo;
        }

        private void AddParametersToCommand(DbCommand cmd, IEnumerable<ColumnInfo> columns)
        {
            foreach (ColumnInfo colInfo in columns)
            {
                var par = cmd.CreateParameter();
                par.ParameterName = $"@{colInfo.Name}";
                _provider.SetParameterDbType(par, _tableInfo.Name, colInfo.Name);
                cmd.Parameters.Add(par);
            }
        }

        /// <inheritdoc/>
        public object GetColumnValue(ColumnInfo columnInfo, T item)
        {
            var value = columnInfo.PropertyInfo.GetValue(item, null);
            if (value != null)
            {
                var converter = ConverterHelper.GetConverter(columnInfo, value.GetType());
                if (converter != null)
                {
                    value = converter.ConvertBack(value);
                }
            }

            return value;
        }

        private string GetInsertCommandText(IEnumerable<ColumnInfo> columns)
        {
            var paramNames = new StringBuilder();
            var paramValues = new StringBuilder();

            foreach (var column in columns)
            {
                if (paramNames.Length > 0)
                {
                    paramNames.Append(", ");
                }
                paramNames.AppendFormat("[{0}]", column.Name);

                if (paramValues.Length > 0)
                {
                    paramValues.Append(", ");
                }
                paramValues.AppendFormat("@{0}", column.Name);
            }

            return string.Format(INSERT_QUERY_BASE, _tableInfo.Name, paramNames.ToString(), paramValues.ToString());
        }

        private string GetUpdateCommandText(IEnumerable<ColumnInfo> columns)
        {
            var paramSetPart = new StringBuilder();

            foreach (var column in columns.Where(p => !p.IsPrimaryKey))
            {
                if (paramSetPart.Length > 0)
                {
                    paramSetPart.Append(", ");
                }
                paramSetPart.AppendFormat("[{0}] = @{0}", column.Name);
            }

            var paramWherePart = new StringBuilder();

            foreach (var col in _tableInfo.PrimaryKey)
            {
                if (paramWherePart.Length > 0)
                {
                    paramWherePart.Append(" AND ");
                }
                paramWherePart.AppendFormat("([{0}] = @{0})", col.Name);
            }

            return string.Format(UPDATE_QUERY_BASE, _tableInfo.Name, paramSetPart.ToString(), paramWherePart.ToString());
        }

        private string GetDeleteCommandText(IEnumerable<ColumnInfo> columns)
        {
            var paramWherePart = new StringBuilder();

            foreach (ColumnInfo column in columns)
            {
                if (paramWherePart.Length > 0)
                {
                    paramWherePart.Append(" AND ");
                }
                paramWherePart.AppendFormat("([{0}] = @{0})", column.Name);
            }

            return string.Format(DELETE_QUERY_BASE, _tableInfo.Name, paramWherePart.ToString());
        }

        #endregion
    }
}
