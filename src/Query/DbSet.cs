using Kros.KORM.CommandGenerator;
using Kros.KORM.Data;
using Kros.KORM.Exceptions;
using Kros.KORM.Metadata;
using Kros.KORM.Properties;
using Kros.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
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
        private readonly TableInfo _tableInfo;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DbSet{T}" /> class.
        /// </summary>
        /// <param name="commandGenerator">Generator to create commands.</param>
        /// <param name="provider">Provider to executing commands.</param>
        /// <param name="query">Query.</param>
        /// <param name="tableInfo">Information about table from database.</param>
        public DbSet(ICommandGenerator<T> commandGenerator, IQueryProvider provider, IQueryBase<T> query, TableInfo tableInfo)
        {
            _commandGenerator = Check.NotNull(commandGenerator, nameof(commandGenerator));
            _provider = Check.NotNull(provider, nameof(provider));
            _query = Check.NotNull(query, nameof(query));
            _tableInfo = Check.NotNull(tableInfo, nameof(tableInfo));
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

            _editedItems.Add(entity);
        }

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

            _deletedItems.Add(entity);
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
                this.Add(entity);
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
                this.Edit(entity);
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
                this.Delete(entity);
            }
        }

        /// <summary>
        /// Clear Added, Edited and Deleted lists of items.
        /// </summary>
        public void Clear()
        {
            _addedItems.Clear();
            _editedItems.Clear();
            _deletedItems.Clear();
        }

        /// <inheritdoc />
        public void BulkInsert()
        {
            BulkInsertCoreAsync(_addedItems, false).GetAwaiter().GetResult();
            _addedItems?.Clear();
        }

        /// <inheritdoc />
        public void BulkInsert(IEnumerable<T> items)
        {
            Check.NotNull(items, nameof(items));

            BulkInsertCoreAsync(items, false).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public async Task BulkInsertAsync()
        {
            await BulkInsertCoreAsync(_addedItems, true);

            _addedItems?.Clear();
        }

        /// <inheritdoc />
        public Task BulkInsertAsync(IEnumerable<T> items)
        {
            Check.NotNull(items, nameof(items));

            return BulkInsertCoreAsync(items, true);
        }

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
        public void CommitChanges() => CommitChangesCoreAsync(false).GetAwaiter().GetResult();

        /// <inheritdoc/>
        public Task CommitChangesAsync() => CommitChangesCoreAsync(true);

        private async Task CommitChangesCoreAsync(bool useAsync)
        {
            await _provider.ExecuteInTransactionAsync(async () =>
            {
                await CommitChangesAddedItemsAsync(_addedItems, useAsync);
                await CommitChangesEditedItemsAsync(_editedItems, useAsync);
                await CommitChangesDeletedItemsAsync(_deletedItems, useAsync);

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

        #endregion

        #region Private Helpers

        private void PrepareCommand(IDbCommand command)
        {
            if (_provider.SupportsPrepareCommand())
            {
                command.Prepare();
            }
        }

        private async Task CommitChangesAddedItemsAsync(HashSet<T> items, bool useAsync)
        {
            if (items?.Count > 0)
            {
                GeneratePrimaryKeys(items);
                using (DbCommand command = _commandGenerator.GetInsertCommand())
                {
                    PrepareCommand(command);
                    foreach (T item in items)
                    {
                        _commandGenerator.FillCommand(command, item);
                        await ExecuteNonQueryAsync(command, useAsync);
                    }
                }
            }
        }

        private async Task ExecuteNonQueryAsync(DbCommand command, bool useAsync)
        {
            if (useAsync)
            {
                await _provider.ExecuteNonQueryCommandAsync(command);
            }
            else
            {
                _provider.ExecuteNonQueryCommand(command);
            }
        }

        private void GeneratePrimaryKeys(HashSet<T> items)
        {
            if (CanGeneratePrimaryKeys())
            {
                var primaryKey = _tableInfo.PrimaryKey.Single(p => p.AutoIncrementMethodType == AutoIncrementMethodType.Custom);

                using (var generator = _provider.CreateIdGenerator(_tableInfo.Name, items.Count))
                {
                    foreach (T item in items)
                    {
                        if ((int)primaryKey.GetValue(item) == 0)
                        {
                            primaryKey.SetValue(item, generator.GetNext());
                        }
                    }
                }
            }
        }

        private bool CanGeneratePrimaryKeys() =>
            _tableInfo.PrimaryKey.Count(p => p.AutoIncrementMethodType == AutoIncrementMethodType.Custom) == 1;

        private async Task CommitChangesEditedItemsAsync(HashSet<T> items, bool useAsync)
        {
            if (items?.Count > 0)
            {
                using (DbCommand command = _commandGenerator.GetUpdateCommand())
                {
                    PrepareCommand(command);
                    foreach (T item in items)
                    {
                        _commandGenerator.FillCommand(command, item);
                        await ExecuteNonQueryAsync(command, useAsync);
                    }
                }
            }
        }

        private async Task CommitChangesDeletedItemsAsync(HashSet<T> items, bool useAsync)
        {
            if (items?.Count > 0)
            {
                using (DbCommand command = _commandGenerator.GetDeleteCommand())
                {
                    foreach (T item in items)
                    {
                        _commandGenerator.FillCommand(command, item);
                        await ExecuteNonQueryAsync(command, useAsync);
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

        private async Task BulkInsertCoreAsync(IEnumerable<T> items, bool useAsync)
        {
            if (items != null)
            {
                using (var bulkInsert = _provider.CreateBulkInsert())
                {
                    bulkInsert.DestinationTableName = _tableInfo.Name;

                    const int defaultBatchSize = 100;
                    var batchSize = items is ICollection<T> coll ? coll.Count : defaultBatchSize;

                    var idGenerator = CanGeneratePrimaryKeys()
                        ? _provider.CreateIdGenerator(_tableInfo.Name, batchSize)
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
            if (_tableInfo.PrimaryKey.Count() != 1)
            {
                throw new InvalidOperationException(string.Format(Resources.InvalidPrimaryKeyForBulkUpdate, _tableInfo.Name));
            }

            if (items != null)
            {
                using (var bulkUpdate = _provider.CreateBulkUpdate())
                {
                    bulkUpdate.DestinationTableName = _tableInfo.Name;
                    bulkUpdate.PrimaryKeyColumn = _tableInfo.PrimaryKey.FirstOrDefault().Name;
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
            return this.GetEnumerator();
        }

        #endregion
    }
}
