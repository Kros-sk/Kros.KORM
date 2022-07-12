using Kros.KORM.CommandGenerator;
using Kros.KORM.Data;
using Kros.KORM.Exceptions;
using Kros.KORM.Metadata;
using Kros.KORM.Properties;
using Kros.KORM.Query.Expressions;
using Kros.KORM.Query.Sql;
using Kros.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Kros.KORM.Query
{
    /// <summary>
    /// Represents the collection of all items that can be saved to the database, of a given type.
    /// </summary>
    /// <typeparam name="T">The type that defines the set.</typeparam>
    public class DbSet<T> : IDbSet<T>
    {
        #region Private fields

        private ICommandGenerator<T> _commandGenerator;
        private IQueryProvider _provider;
        private IQueryBase<T> _query;
        private HashSet<T> _addedItems = new HashSet<T>();
        private HashSet<T> _editedItems = new HashSet<T>();
        private HashSet<T> _deletedItems = new HashSet<T>();
        private HashSet<T> _upsertedItems = new HashSet<T>();
        private HashSet<object> _deletedItemsIds = new HashSet<object>();
        private List<WhereExpression> _deleteExpressions = new List<WhereExpression>();
        private readonly TableInfo _tableInfo;
        private Lazy<Type> _primaryKeyPropertyType;
        private IEnumerable<string> _upsertConditionColumnNames;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DbSet{T}" /> class.
        /// </summary>
        /// <param name="commandGenerator">Generator to create commands.</param>
        /// <param name="provider">Provider to executing commands.</param>
        /// <param name="query">Query.</param>
        /// <param name="tableInfo">Information about table from database.</param>
        public DbSet(
            ICommandGenerator<T> commandGenerator,
            IQueryProvider provider,
            IQueryBase<T> query,
            TableInfo tableInfo)
        {
            _commandGenerator = Check.NotNull(commandGenerator, nameof(commandGenerator));
            _provider = Check.NotNull(provider, nameof(provider));
            _query = Check.NotNull(query, nameof(query));
            _tableInfo = Check.NotNull(tableInfo, nameof(tableInfo));
            _primaryKeyPropertyType = new Lazy<Type>(GetPrimaryKeyType);
        }

        #endregion

        #region IDbSet Members

        /// <summary>
        /// Adds the item to the context underlying the set in the Added state such that it will be inserted
        /// into the database when CommitChanges is called.
        /// </summary>
        /// <param name="entity">Item to add.</param>
        /// <exception cref="AlreadyInCollectionException">Adding item already exists in list of items.</exception>
        public void Add(T entity)
        {
            CheckItemInCollection(entity, _editedItems, Resources.ExistingItemCannotBeAdded, nameof(EditedItems));
            CheckItemInCollection(entity, _deletedItems, Resources.ExistingItemCannotBeAdded, nameof(DeletedItems));
            CheckItemInCollection(entity, _upsertedItems, Resources.ExistingItemCannotBeAdded, nameof(UpsertedItems));

            _addedItems.Add(entity);
        }

        /// <summary>
        /// Adds the item to the context underlying the set in the Edited state such that it will be updated
        /// in the database when CommitChanges is called.
        /// </summary>
        /// <param name="entity">Item to add.</param>
        /// <exception cref="AlreadyInCollectionException">Adding item already exists in list of items.</exception>
        public void Edit(T entity)
        {
            CheckItemInCollection(entity, _addedItems, Resources.ExistingItemCannotBeEdited, nameof(AddedItems));
            CheckItemInCollection(entity, _deletedItems, Resources.ExistingItemCannotBeEdited, nameof(DeletedItems));
            CheckItemInCollection(entity, _upsertedItems, Resources.ExistingItemCannotBeEdited, nameof(UpsertedItems));

            _editedItems.Add(entity);
        }

        /// <summary>
        /// Adds the item to the context underlying the set in the Upserted state such that it will be updated or
        /// inserted in the database when CommitChanges is called.
        /// </summary>
        /// <param name="entity">Item to add.</param>
        /// <exception cref="AlreadyInCollectionException">Adding item already exists in list of items.</exception>
        public void Upsert(T entity)
        {
            CheckItemInCollection(entity, _addedItems, Resources.ExistingItemCanNotBeUpserted, nameof(AddedItems));
            CheckItemInCollection(entity, _editedItems, Resources.ExistingItemCanNotBeUpserted, nameof(EditedItems));
            CheckItemInCollection(entity, _deletedItems, Resources.ExistingItemCanNotBeUpserted, nameof(DeletedItems));

            _upsertedItems.Add(entity);
        }

        /// <summary>
        /// Adds the items to the context underlying the set in the Upserted state such that it will be updated or
        /// inserted in the database when CommitChanges is called.
        /// </summary>
        /// <param name="entities">Items to add.</param>
        public void Upsert(IEnumerable<T> entities)
            => entities.ForEach(e => Upsert(e));

        /// <summary>
        /// Adds the item to the context underlying the set in the Deleted state such that it will be deleted
        /// from the database when CommitChanges is called.
        /// </summary>
        /// <param name="entity">Item to add.</param>
        /// <exception cref="AlreadyInCollectionException">Adding item already exists in list of items.</exception>
        public void Delete(T entity)
        {
            CheckItemInCollection(entity, _addedItems, Resources.ExistingItemCannotBeDeleted, nameof(AddedItems));
            CheckItemInCollection(entity, _editedItems, Resources.ExistingItemCannotBeDeleted, nameof(EditedItems));
            CheckItemInCollection(entity, _upsertedItems, Resources.ExistingItemCannotBeDeleted, nameof(UpsertedItems));

            _deletedItems.Add(entity);
        }

        /// <inheritdoc />
        public void Delete(object id)
        {
            Check.NotNull(id, nameof(id));
            if (_deletedItemsIds.Contains(id))
            {
                throw new AlreadyInCollectionException(string.Format(Resources.ExistingItemIdCannotBeDeleted, id));
            }
            if (_primaryKeyPropertyType.Value != id.GetType())
            {
                throw new ArgumentException(
                    string.Format(
                        Resources.InvalidPrimaryKeyValueType,
                        _primaryKeyPropertyType.Value.FullName,
                        id.GetType().FullName));
            }

            _deletedItemsIds.Add(id);
        }

        /// <inheritdoc />
        public void Delete(Expression<Func<T, bool>> condition)
        {
            Check.NotNull(condition, nameof(condition));

            ISqlExpressionVisitor generator = _provider.GetExpressionVisitor();
            WhereExpression where = generator.GenerateWhereCondition(condition.Body);

            _deleteExpressions.Add(where);
        }

        /// <inheritdoc />
        public void Delete(RawSqlString condition, params object[] parameters)
        {
            Check.NotNull(condition, nameof(condition));

            var where = new WhereExpression(condition, parameters);

            _deleteExpressions.Add(where);
        }

        private Type GetPrimaryKeyType()
        {
            ThrowHelper.CheckAndThrowMethodNotSupportedWhenNoPrimaryKey(_tableInfo, nameof(Delete));
            ThrowHelper.CheckAndThrowMethodNotSupportedForCompositePrimaryKey(_tableInfo, nameof(Delete));

            return _tableInfo.PrimaryKey.First().PropertyInfo.PropertyType;
        }

        /// <summary>
        /// Adds the items to the context underlying the set in the Added state such that it will be inserted
        /// into the database when CommitChanges is called.
        /// </summary>
        /// <param name="entities">The items to add.</param>
        public void Add(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                Add(entity);
            }
        }

        /// <summary>
        /// Marks the items as Edited such that it will be updated in the database when CommitChanges is called.
        /// </summary>
        /// <param name="entities">The items to edit.</param>
        public void Edit(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                Edit(entity);
            }
        }

        /// <summary>
        /// Marks the items as Deleted such that it will be deleted from the database when CommitChanges is called.
        /// </summary>
        /// <param name="entities">The items to delete.</param>
        public void Delete(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                Delete(entity);
            }
        }

        /// <summary>
        /// Clear Added, Edited and Deleted lists of items.
        /// </summary>
        public void Clear()
        {
            _addedItems.Clear();
            _editedItems.Clear();
            _upsertedItems.Clear();
            _deletedItems.Clear();
            _deletedItemsIds.Clear();
            _deleteExpressions.Clear();
        }

        /// <inheritdoc />
        public void BulkInsert()
        {
            BulkInsertCoreAsync(_addedItems, false, null).GetAwaiter().GetResult();
            _addedItems?.Clear();
        }

        /// <inheritdoc />
        public void BulkInsert(object options)
        {
            BulkInsertCoreAsync(_addedItems, false, options).GetAwaiter().GetResult();
            _addedItems?.Clear();
        }

        /// <inheritdoc />
        public void BulkInsert(IEnumerable<T> items)
            => BulkInsertCoreAsync(Check.NotNull(items, nameof(items)), false, null).GetAwaiter().GetResult();

        /// <inheritdoc />
        public void BulkInsert(IEnumerable<T> items, object options)
            => BulkInsertCoreAsync(Check.NotNull(items, nameof(items)), false, options).GetAwaiter().GetResult();

        /// <inheritdoc />
        public async Task BulkInsertAsync()
        {
            await BulkInsertCoreAsync(_addedItems, true, null);

            _addedItems?.Clear();
        }

        /// <inheritdoc />
        public async Task BulkInsertAsync(object options)
        {
            await BulkInsertCoreAsync(_addedItems, true, options);

            _addedItems?.Clear();
        }

        /// <inheritdoc />
        public Task BulkInsertAsync(IEnumerable<T> items)
            => BulkInsertCoreAsync(Check.NotNull(items, nameof(items)), true, null);

        /// <inheritdoc />
        public Task BulkInsertAsync(IEnumerable<T> items, object options)
            => BulkInsertCoreAsync(Check.NotNull(items, nameof(items)), true, options);

        /// <inheritdoc />
        public void BulkUpdate()
        {
            BulkUpdateCoreAsync(_editedItems, null, false).GetAwaiter().GetResult();
            _editedItems?.Clear();
        }

        /// <inheritdoc />
        public void BulkUpdate(IEnumerable<T> items)
        {
            Check.NotNull(items, nameof(items));

            BulkUpdateCoreAsync(items, null, false).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public void BulkUpdate(Action<IDbConnection, IDbTransaction, string> tempTableAction)
        {
            BulkUpdateCoreAsync(_editedItems, tempTableAction, false).GetAwaiter().GetResult();
            _editedItems?.Clear();
        }

        /// <inheritdoc />
        public void BulkUpdate(IEnumerable<T> items, Action<IDbConnection, IDbTransaction, string> tempTableAction)
        {
            Check.NotNull(items, nameof(items));

            BulkUpdateCoreAsync(items, tempTableAction, false).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public async Task BulkUpdateAsync()
        {
            await BulkUpdateCoreAsync(_editedItems, null, true);
            _editedItems?.Clear();
        }

        /// <inheritdoc />
        public Task BulkUpdateAsync(IEnumerable<T> items)
        {
            Check.NotNull(items, nameof(items));

            return BulkUpdateCoreAsync(items, null, true);
        }

        /// <inheritdoc />
        public async Task BulkUpdateAsync(Action<IDbConnection, IDbTransaction, string> tempTableAction)
        {
            await BulkUpdateCoreAsync(_editedItems, tempTableAction, true);
            _editedItems?.Clear();
        }

        /// <inheritdoc />
        public Task BulkUpdateAsync(IEnumerable<T> items, Action<IDbConnection, IDbTransaction, string> tempTableAction)
        {
            Check.NotNull(items, nameof(items));

            return BulkUpdateCoreAsync(items, tempTableAction, true);
        }

        /// <summary>
        /// Commits all pending changes to the database.
        /// </summary>
        public void CommitChanges(bool ignoreValueGenerators = false)
            => CommitChangesCoreAsync(false, ignoreValueGenerators).GetAwaiter().GetResult();

        /// <inheritdoc/>
        public Task CommitChangesAsync(CancellationToken cancellationToken = default, bool ignoreValueGenerators = false)
            => CommitChangesCoreAsync(true, ignoreValueGenerators, cancellationToken);

        private async Task CommitChangesCoreAsync(
            bool useAsync,
            bool ignoreValueGenerators,
            CancellationToken cancellationToken = default)
        {
            await _provider.ExecuteInTransactionAsync(async () =>
            {
                await CommitChangesAddedItemsAsync(_addedItems, useAsync, ignoreValueGenerators, cancellationToken);
                await CommitChangesEditedItemsAsync(_editedItems, useAsync, ignoreValueGenerators, cancellationToken);
                await CommitChangesUpsertedItemsAsync(_upsertedItems, useAsync, cancellationToken);
                await CommitChangesDeletedItemsAsync(_deletedItems, useAsync, cancellationToken);
                await CommitChangesDeletedItemsByIdAsync(_deletedItemsIds, useAsync, cancellationToken);
                await CommitChangesDeletedByConditionsAsync(_deleteExpressions, useAsync, cancellationToken);

                Clear();
            });
        }

        /// <summary>
        /// List of items in Added state.
        /// </summary>
        public IEnumerable<T> AddedItems { get { return _addedItems; } }

        /// <summary>
        /// List of items in Edited state.
        /// </summary>
        public IEnumerable<T> EditedItems { get { return _editedItems; } }

        /// <summary>
        /// List of items in Deleted state.
        /// </summary>
        public IEnumerable<T> DeletedItems { get { return _deletedItems; } }

        /// <summary>
        /// List of items in Upserted state.
        /// </summary>
        public IEnumerable<T> UpsertedItems { get { return _upsertedItems; } }

        /// <summary>
        /// IDbSet with custom upsert condition columns.
        /// </summary>
        /// <param name="columnNames">The column names.</param>
        public IDbSet<T> WithCustomUpsertConditionColumns(params string[] columnNames)
        {
            _upsertConditionColumnNames = columnNames;
            return this;
        }

        #endregion

        #region Private Helpers

        private void PrepareCommand(IDbCommand command)
        {
            if (_provider.SupportsPrepareCommand())
            {
                command.Prepare();
            }
        }

        private async Task CommitChangesAddedItemsAsync(
            HashSet<T> items,
            bool useAsync,
            bool ignoreValueGenerators,
            CancellationToken cancellationToken = default)
        {
            if (items?.Count > 0)
            {
                GenerateCustomPrimaryKeys(items);
                bool hasIdentity = CheckIfCanUseIdentityPrimaryKey();

                using (DbCommand command = _commandGenerator.GetInsertCommand())
                {
                    PrepareCommand(command);
                    ValueGenerated valueGenerated = ignoreValueGenerators
                        ? ValueGenerated.Never
                        : ValueGenerated.OnInsert;
                    foreach (T item in items)
                    {
                        _commandGenerator.FillCommand(command, item, valueGenerated);
                        if (hasIdentity)
                        {
                            var id = await ExecuteScalarAsync(command, useAsync, cancellationToken);
                            _tableInfo.IdentityPrimaryKey.SetValue(item, id);
                        }
                        else
                        {
                            await ExecuteNonQueryAsync(command, useAsync, cancellationToken);
                        }
                    }
                }
            }
        }

        private bool CheckIfCanUseIdentityPrimaryKey()
        {
            var hasIdentity = _tableInfo.HasIdentityPrimaryKey;
            if (hasIdentity && !_provider.SupportsIdentity())
            {
                throw new InvalidOperationException(
                    string.Format(Resources.ProviderDoesNotSupportIdentity, _provider.GetType().Name, typeof(T).Name));
            }

            return hasIdentity;
        }

        private async Task ExecuteNonQueryAsync(DbCommand command, bool useAsync, CancellationToken cancellationToken = default)
        {
            if (useAsync)
            {
                await _provider.ExecuteNonQueryCommandAsync(command, cancellationToken);
            }
            else
            {
                _provider.ExecuteNonQueryCommand(command);
            }
        }

        private async Task<object> ExecuteScalarAsync(
            DbCommand command,
            bool useAsync,
            CancellationToken cancellationToken = default)
        {
            if (useAsync)
            {
                return await _provider.ExecuteScalarCommandAsync(command, cancellationToken);
            }
            else
            {
                return _provider.ExecuteScalarCommand(command);
            }
        }

        private void GenerateCustomPrimaryKeys(HashSet<T> items)
        {
            if (CanGeneratePrimaryKeys(out ColumnInfo primaryKey))
            {
                Type dataType = primaryKey.PropertyInfo.PropertyType;
                string generatorName = primaryKey.AutoIncrementGeneratorName ?? _tableInfo.Name;
                using (var generator = _provider.CreateIdGenerator(dataType, generatorName, items.Count))
                {
                    foreach (T item in items)
                    {
                        var currentValue = primaryKey.GetValue(item);
                        if (primaryKey.IsDefaultValue(currentValue))
                        {
                            primaryKey.SetValue(item, generator.GetNext());
                        }
                    }
                }
            }
        }

        private bool CanGeneratePrimaryKeys(out ColumnInfo pkColumn)
        {
            var pkColumns = _tableInfo.PrimaryKey
                .Where(p => p.AutoIncrementMethodType == AutoIncrementMethodType.Custom)
                .ToList();

            if (pkColumns.Count == 1)
            {
                pkColumn = pkColumns[0];
                return true;
            }
            pkColumn = null;
            return false;
        }

        private async Task CommitChangesEditedItemsAsync(
            HashSet<T> items,
            bool useAsync,
            bool ignoreValueGenerators,
            CancellationToken cancellationToken = default)
        {
            if (items.Count > 0)
            {
                using (DbCommand command = _commandGenerator.GetUpdateCommand())
                {
                    PrepareCommand(command);
                    ValueGenerated valueGenerated = ignoreValueGenerators
                        ? ValueGenerated.Never
                        : ValueGenerated.OnUpdate;
                    foreach (T item in items)
                    {
                        _commandGenerator.FillCommand(command, item, valueGenerated);
                        await ExecuteNonQueryAsync(command, useAsync, cancellationToken);
                    }
                }
            }
        }

        private async Task CommitChangesUpsertedItemsAsync(
            HashSet<T> items,
            bool useAsync,
            CancellationToken cancellationToken = default)
        {
            if (items.Count > 0)
            {
                using (DbCommand command = _commandGenerator.GetUpsertCommand(_upsertConditionColumnNames))
                {
                    PrepareCommand(command);
                    foreach (T item in items)
                    {
                        _commandGenerator.FillCommand(command, item, ValueGenerated.Never);
                        await ExecuteNonQueryAsync(command, useAsync, cancellationToken);
                    }
                }
            }
        }

        private async Task CommitChangesDeletedItemsAsync(
            HashSet<T> items,
            bool useAsync,
            CancellationToken cancellationToken = default)
        {
            if (items.Count > 0)
            {
                using (DbCommand command = _commandGenerator.GetDeleteCommand())
                {
                    foreach (T item in items)
                    {
                        _commandGenerator.FillCommand(command, item, ValueGenerated.Never);
                        await ExecuteNonQueryAsync(command, useAsync, cancellationToken);
                    }
                }
            }
        }

        private async Task CommitChangesDeletedItemsByIdAsync(
            HashSet<object> ids,
            bool useAsync,
            CancellationToken cancellationToken = default)
        {
            if (ids.Count > 0)
            {
                foreach (DbCommand command in _commandGenerator.GetDeleteCommands(ids))
                {
                    try
                    {
                        await ExecuteNonQueryAsync(command, useAsync, cancellationToken);
                    }
                    finally
                    {
                        command.Dispose();
                    }
                }
            }
        }

        private async Task CommitChangesDeletedByConditionsAsync(
            List<WhereExpression> expressions,
            bool useAsync,
            CancellationToken cancellationToken = default)
        {
            if (expressions.Count > 0)
            {
                foreach (WhereExpression expression in expressions)
                {
                    using (DbCommand command = _commandGenerator.GetDeleteCommand(expression))
                    {
                        await ExecuteNonQueryAsync(command, useAsync, cancellationToken);
                    }
                }
            }
        }

        private void CheckItemInCollection(T entity, HashSet<T> collection, string message, string collectionName)
        {
            if (collection.Contains(entity))
            {
                throw new AlreadyInCollectionException(string.Format(message, entity.GetHashCode(), collectionName));
            }
        }

        private async Task BulkInsertCoreAsync(IEnumerable<T> items, bool useAsync, object options)
        {
            if (items != null)
            {
                using (var bulkInsert = _provider.CreateBulkInsert(options))
                {
                    bulkInsert.DestinationTableName = _tableInfo.Name;

                    const int defaultBatchSize = 100;
                    var batchSize = items is ICollection<T> coll ? coll.Count : defaultBatchSize;

                    var idGenerator = CanGeneratePrimaryKeys(out ColumnInfo pkColumn)
                        ? _provider.CreateIdGenerator(pkColumn.PropertyInfo.PropertyType,
                            pkColumn.AutoIncrementGeneratorName ?? _tableInfo.Name, batchSize)
                        : null;

                    using (var reader = new KormBulkInsertDataReader<T>(items, _commandGenerator, idGenerator, _tableInfo))
                    {
                        if (useAsync)
                        {
                            await bulkInsert.InsertAsync(reader);
                        }
                        else
                        {
                            bulkInsert.Insert(reader);
                        }
                    }
                }
            }
        }

        private async Task BulkUpdateCoreAsync(
            IEnumerable<T> items,
            Action<IDbConnection, IDbTransaction, string>
            tempTableAction,
            bool useAsync)
        {
            if (!_tableInfo.PrimaryKey.Any())
            {
                throw new InvalidOperationException(Resources.BulkUpdatePrimaryKeyIsNotSet);
            }

            if (items != null)
            {
                using (var bulkUpdate = _provider.CreateBulkUpdate())
                {
                    bulkUpdate.DestinationTableName = _tableInfo.Name;
                    bulkUpdate.PrimaryKeyColumn = string.Join(",", _tableInfo.PrimaryKey.Select(pk => pk.Name));
                    bulkUpdate.TempTableAction = tempTableAction;

                    using (var reader = new KormDataReader<T>(items, _commandGenerator))
                    {
                        if (useAsync)
                        {
                            await bulkUpdate.UpdateAsync(reader);
                        }
                        else
                        {
                            bulkUpdate.Update(reader);
                        }
                    }
                }
            }
        }

        #endregion

        #region IEnumerator

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<T> GetEnumerator()
        {
            return _query.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
