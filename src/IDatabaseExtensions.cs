using Kros.KORM.Query;
using Kros.KORM.Query.Sql;
using Kros.KORM.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Kros.KORM
{
    /// <summary>
    /// Extensions over <see cref="IDatabase"/>.
    /// </summary>
    public static partial class IDatabaseExtensions
    {
        /// <summary>
        /// Adds the <paramref name="entity"/> to the database.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <param name="database"><see cref="IDatabase"/> instance.</param>
        /// <param name="entity">The entity to add.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public static async Task AddAsync<TEntity>(
            this IDatabase database,
            TEntity entity,
            CancellationToken cancellationToken = default) where TEntity : class
        {
            ThrowIfCollection(entity, nameof(AddRangeAsync));
            await CommitChangesAsync(database, (IDbSet<TEntity> dbSet) => dbSet.Add(entity), cancellationToken);
        }

        /// <summary>
        /// Adds <paramref name="entities"/> to the database.
        /// </summary>
        /// <typeparam name="TEntity">Entities type.</typeparam>
        /// <param name="database"><see cref="IDatabase"/> instance.</param>
        /// <param name="entities">Entities to add.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public static async Task AddRangeAsync<TEntity>(
            this IDatabase database,
            IEnumerable<TEntity> entities,
            CancellationToken cancellationToken = default) where TEntity : class
            => await CommitChangesAsync(database, (IDbSet<TEntity> dbSet) => dbSet.Add(entities), cancellationToken);

        /// <summary>
        /// Adds <paramref name="entities"/> to the database via bulk insert.
        /// </summary>
        /// <typeparam name="TEntity">Entities type.</typeparam>
        /// <param name="database"><see cref="IDatabase"/> instance.</param>
        /// <param name="entities">Entities to add.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public static async Task BulkAddAsync<TEntity>(
            this IDatabase database,
            IEnumerable<TEntity> entities,
            CancellationToken cancellationToken = default) where TEntity : class
            => await ProcessBulkOperationAsync(
                database,
                async (IDbSet<TEntity> dbSet, CancellationToken ct) => await dbSet.BulkInsertAsync(entities, ct),
                cancellationToken);

        /// <summary>
        /// Adds <paramref name="entities"/> to the database via bulk insert.
        /// </summary>
        /// <typeparam name="TEntity">Entities type.</typeparam>
        /// <param name="database"><see cref="IDatabase"/> instance.</param>
        /// <param name="entities">Entities to add.</param>
        /// <param name="options">
        /// Database specific provider options. For SQL Server use <see cref="SqlServerProviderOptions"/>.
        /// </param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public static async Task BulkAddAsync<TEntity>(
            this IDatabase database,
            IEnumerable<TEntity> entities,
            object options,
            CancellationToken cancellationToken = default) where TEntity : class
            => await ProcessBulkOperationAsync(
                database,
                async (IDbSet<TEntity> dbSet, CancellationToken ct) => await dbSet.BulkInsertAsync(entities, options, ct),
                cancellationToken);

        /// <summary>
        /// Deletes the <paramref name="entity"/> from the database.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <param name="database"><see cref="IDatabase"/> instance.</param>
        /// <param name="entity">The entity to delete.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public static async Task DeleteAsync<TEntity>(
            this IDatabase database,
            TEntity entity,
            CancellationToken cancellationToken = default) where TEntity : class
        {
            ThrowIfCollection(entity, nameof(DeleteRangeAsync));
            await CommitChangesAsync(database, (IDbSet<TEntity> dbSet) => dbSet.Delete(entity), cancellationToken);
        }

        /// <summary>
        /// Deletes <paramref name="entities"/> from the database.
        /// </summary>
        /// <typeparam name="TEntity">Entities type.</typeparam>
        /// <param name="database"><see cref="IDatabase"/> instance.</param>
        /// <param name="entities">Entities to delete.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public static async Task DeleteRangeAsync<TEntity>(
            this IDatabase database,
            IEnumerable<TEntity> entities,
            CancellationToken cancellationToken = default) where TEntity : class
            => await CommitChangesAsync(database, (IDbSet<TEntity> dbSet) => dbSet.Delete(entities), cancellationToken);

        /// <summary>
        /// Deletes the <typeparamref name="TEntity"/> with <paramref name="id"/> from the database.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <param name="database"><see cref="IDatabase"/> instance.</param>
        /// <param name="id">The entity id to delete.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public static async Task DeleteAsync<TEntity>(
            this IDatabase database,
            object id,
            CancellationToken cancellationToken = default) where TEntity : class
            => await CommitChangesAsync(database, (IDbSet<TEntity> dbSet) => dbSet.Delete(id), cancellationToken);

        /// <summary>
        /// Deletes the <typeparamref name="TEntity"/> from the database by <paramref name="condition"/>.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <param name="database"><see cref="IDatabase"/> instance.</param>
        /// <param name="condition">The delete condition.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public static async Task DeleteAsync<TEntity>(
            this IDatabase database,
            Expression<Func<TEntity, bool>> condition,
            CancellationToken cancellationToken = default) where TEntity : class
            => await CommitChangesAsync(database, (IDbSet<TEntity> dbSet) => dbSet.Delete(condition), cancellationToken);

        /// <summary>
        /// Deletes the <typeparamref name="TEntity"/> from the database by <paramref name="condition"/>.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <param name="database"><see cref="IDatabase"/> instance.</param>
        /// <param name="condition">The delete condition.</param>
        /// <param name="parameters">Condition parameters.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public static async Task DeleteAsync<TEntity>(
            this IDatabase database,
            RawSqlString condition,
            CancellationToken cancellationToken = default,
            params object[] parameters) where TEntity : class
            => await CommitChangesAsync(database, (IDbSet<TEntity> dbSet) => dbSet.Delete(condition, parameters), cancellationToken);

        /// <summary>
        /// Edits the <paramref name="entity"/> in the database.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <param name="database"><see cref="IDatabase"/> instance.</param>
        /// <param name="entity">The entity to edit.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public static async Task EditAsync<TEntity>(
            this IDatabase database,
            TEntity entity,
            CancellationToken cancellationToken = default) where TEntity : class
        {
            ThrowIfCollection(entity, nameof(EditRangeAsync));
            await CommitChangesAsync(database, (IDbSet<TEntity> dbSet) => dbSet.Edit(entity), cancellationToken);
        }

        /// <summary>
        /// Edits <paramref name="entities"/> in the database.
        /// </summary>
        /// <typeparam name="TEntity">Entities type.</typeparam>
        /// <param name="database"><see cref="IDatabase"/> instance.</param>
        /// <param name="entities">Entities to edit.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public static async Task EditRangeAsync<TEntity>(
            this IDatabase database,
            IEnumerable<TEntity> entities,
            CancellationToken cancellationToken = default) where TEntity : class
            => await CommitChangesAsync(database, (IDbSet<TEntity> dbSet) => dbSet.Edit(entities), cancellationToken);

        /// <summary>
        /// Edits <paramref name="entities"/> in the database via bulk update.
        /// </summary>
        /// <typeparam name="TEntity">Entities type.</typeparam>
        /// <param name="database"><see cref="IDatabase"/> instance.</param>
        /// <param name="entities">Entities to edit.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public static async Task BulkEditAsync<TEntity>(
            this IDatabase database,
            IEnumerable<TEntity> entities,
            CancellationToken cancellationToken = default) where TEntity : class
            => await ProcessBulkOperationAsync(
                database,
                async (IDbSet<TEntity> dbSet, CancellationToken ct) => await dbSet.BulkUpdateAsync(entities, ct),
                cancellationToken);

        /// <summary>
        /// Edits the <paramref name="entity"/> in the database.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <param name="database"><see cref="IDatabase"/> instance.</param>
        /// <param name="entity">The entity to edit.</param>
        /// <param name="columns">Columns for editing.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public static async Task EditAsync<TEntity>(
            this IDatabase database,
            TEntity entity,
            CancellationToken cancellationToken = default,
            params string[] columns) where TEntity : class
        {
            ThrowIfCollection(entity, nameof(EditRangeAsync));
            await CommitChangesAsync(
                database,
                (IDbSet<TEntity> dbSet) => dbSet.Edit(entity),
                cancellationToken: cancellationToken,
                columns: columns);
        }

        /// <summary>
        /// Inserts or updates <paramref name="entity"/> in the database.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <param name="database"><see cref="IDatabase"/> instance.</param>
        /// <param name="entity">The entity to edit.</param>
        /// <param name="columns">Columns for editing.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public static async Task UpsertAsync<TEntity>(
            this IDatabase database,
            TEntity entity,
            CancellationToken cancellationToken = default,
            params string[] columns) where TEntity : class
            => await CommitChangesAsync(
                database, (IDbSet<TEntity> dbSet) => dbSet.Upsert(entity),
                cancellationToken,
                columns);

        /// <summary>
        /// Inserts or updates <paramref name="entities"/> in the database.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <param name="database"><see cref="IDatabase"/> instance.</param>
        /// <param name="entities">The entity to edit.</param>
        /// <param name="columns">Columns for editing.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public static async Task UpsertAsync<TEntity>(
            this IDatabase database,
            IEnumerable<TEntity> entities,
            CancellationToken cancellationToken = default,
            params string[] columns) where TEntity : class
            => await CommitChangesAsync(
                database, (IDbSet<TEntity> dbSet) => dbSet.Upsert(entities),
                cancellationToken,
                columns);

        private static void ThrowIfCollection<TEntity>(TEntity entity, string rangeMethodName)
        {
            if (entity is IEnumerable && entity is not string)
            {
                throw new ArgumentException(
                    string.Format(Resources.UseRangeMethodForCollections, rangeMethodName),
                    nameof(entity));
            }
        }

        private static IDbSet<TEntity> GetDbSet<TEntity>(IDatabase database, params string[] columns) where TEntity : class
            => columns.Length == 0
                ? database.Query<TEntity>().AsDbSet()
                : database.Query<TEntity>().Select(columns).AsDbSet();

        private static async Task CommitChangesAsync<TEntity>(
            IDatabase database,
            Action<IDbSet<TEntity>> action,
            CancellationToken cancellationToken = default,
            params string[] columns) where TEntity : class
        {
            IDbSet<TEntity> dbSet = GetDbSet<TEntity>(database, columns);

            action(dbSet);

            await dbSet.CommitChangesAsync(cancellationToken);
        }

        private static async Task ProcessBulkOperationAsync<TEntity>(
            IDatabase database,
            Func<IDbSet<TEntity>, CancellationToken, Task> action,
            CancellationToken cancellationToken = default,
            params string[] columns) where TEntity : class
        {
            IDbSet<TEntity> dbSet = GetDbSet<TEntity>(database, columns);
            await action(dbSet, cancellationToken);
        }

        #region Obsolete

        /// <summary>
        /// Obsolete for collections. Use <see cref="AddRangeAsync{TEntity}(IDatabase, IEnumerable{TEntity}, CancellationToken)"/> instead.
        /// </summary>
        [Obsolete("Use AddRangeAsync to add a collection of entities.", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task AddAsync<TEntity>(
            this IDatabase database,
            IEnumerable<TEntity> entities,
            CancellationToken cancellationToken = default) where TEntity : class
            => throw new NotSupportedException("Use AddRangeAsync to add a collection of entities.");

        /// <summary>
        /// Obsolete for collections. Use <see cref="AddRangeAsync{TEntity}(IDatabase, IEnumerable{TEntity}, CancellationToken)"/> instead.
        /// </summary>
        [Obsolete("Use AddRangeAsync to add a collection of entities.", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task AddAsync<TEntity>(
            this IDatabase database,
            List<TEntity> entities,
            CancellationToken cancellationToken = default) where TEntity : class
            => throw new NotSupportedException("Use AddRangeAsync to add a collection of entities.");

        /// <summary>
        /// Obsolete for collections. Use <see cref="AddRangeAsync{TEntity}(IDatabase, IEnumerable{TEntity}, CancellationToken)"/> instead.
        /// </summary>
        [Obsolete("Use AddRangeAsync to add a collection of entities.", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task AddAsync<TEntity>(
            this IDatabase database,
            TEntity[] entities,
            CancellationToken cancellationToken = default) where TEntity : class
            => throw new NotSupportedException("Use AddRangeAsync to add a collection of entities.");

        /// <summary>
        /// Obsolete for collections. Use <see cref="EditRangeAsync{TEntity}(IDatabase, IEnumerable{TEntity}, CancellationToken)"/> instead.
        /// </summary>
        [Obsolete("Use EditRangeAsync to edit a collection of entities.", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task EditAsync<TEntity>(
            this IDatabase database,
            IEnumerable<TEntity> entities,
            CancellationToken cancellationToken = default) where TEntity : class
            => throw new NotSupportedException("Use EditRangeAsync to edit a collection of entities.");

        /// <summary>
        /// Obsolete for collections. Use <see cref="EditRangeAsync{TEntity}(IDatabase, IEnumerable{TEntity}, CancellationToken)"/> instead.
        /// </summary>
        [Obsolete("Use EditRangeAsync to edit a collection of entities.", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task EditAsync<TEntity>(
            this IDatabase database,
            List<TEntity> entities,
            CancellationToken cancellationToken = default) where TEntity : class
            => throw new NotSupportedException("Use EditRangeAsync to edit a collection of entities.");

        /// <summary>
        /// Obsolete for collections. Use <see cref="EditRangeAsync{TEntity}(IDatabase, IEnumerable{TEntity}, CancellationToken)"/> instead.
        /// </summary>
        [Obsolete("Use EditRangeAsync to edit a collection of entities.", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task EditAsync<TEntity>(
            this IDatabase database,
            TEntity[] entities,
            CancellationToken cancellationToken = default) where TEntity : class
            => throw new NotSupportedException("Use EditRangeAsync to edit a collection of entities.");

        /// <summary>
        /// Obsolete for collections. Use <see cref="DeleteRangeAsync{TEntity}(IDatabase, IEnumerable{TEntity}, CancellationToken)"/> instead.
        /// </summary>
        [Obsolete("Use DeleteRangeAsync to delete a collection of entities.", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task DeleteAsync<TEntity>(
            this IDatabase database,
            IEnumerable<TEntity> entities,
            CancellationToken cancellationToken = default) where TEntity : class
            => throw new NotSupportedException("Use DeleteRangeAsync to delete a collection of entities.");

        /// <summary>
        /// Obsolete for collections. Use <see cref="DeleteRangeAsync{TEntity}(IDatabase, IEnumerable{TEntity}, CancellationToken)"/> instead.
        /// </summary>
        [Obsolete("Use DeleteRangeAsync to delete a collection of entities.", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task DeleteAsync<TEntity>(
            this IDatabase database,
            List<TEntity> entities,
            CancellationToken cancellationToken = default) where TEntity : class
            => throw new NotSupportedException("Use DeleteRangeAsync to delete a collection of entities.");

        /// <summary>
        /// Obsolete for collections. Use <see cref="DeleteRangeAsync{TEntity}(IDatabase, IEnumerable{TEntity}, CancellationToken)"/> instead.
        /// </summary>
        [Obsolete("Use DeleteRangeAsync to delete a collection of entities.", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task DeleteAsync<TEntity>(
            this IDatabase database,
            TEntity[] entities,
            CancellationToken cancellationToken = default) where TEntity : class
            => throw new NotSupportedException("Use DeleteRangeAsync to delete a collection of entities.");

        #endregion
    }
}
