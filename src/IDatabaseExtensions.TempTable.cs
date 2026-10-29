using Kros.Data.BulkActions;
using Kros.KORM.Data;
using Kros.KORM.Extensions;
using Kros.KORM.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kros.KORM
{
    public partial class IDatabaseExtensions
    {
        /// <summary>
        /// Special execute command, that will insert <paramref name="values"/> into temp table
        /// and then executes <paramref name="action"/> that is dependent on data in temp table.
        /// </summary>
        /// <typeparam name="TValue">Type of values for inserting into temp table.</typeparam>
        /// <param name="database">Korm database access.</param>
        /// <param name="values">Values for inserting into temp table.</param>
        /// <param name="action">Execute action.</param>
        /// <param name="commandTimeout">Command timeount in seconds. A value of 0 indicates no limit.</param>
        /// <remarks>Whole action is executed in transaction.</remarks>
        public static void ExecuteWithTempTable<TValue>(
            this IDatabase database,
            IEnumerable<TValue> values,
            Action<IDatabase, string> action,
            int commandTimeout = TransactionHelper.TIMEOUT_DEFAULT)
        {
            using ITransaction transaction = BeginTransaction(database, commandTimeout);

            action(database, CreateTempTable(database, values));

            TryCommitTransaction(transaction);
        }

        /// <summary>
        /// Special execute command, that will insert <paramref name="values"/> into temp table
        /// and then executes <paramref name="actionAsync"/> that is dependent on data in temp table.
        /// </summary>
        /// <typeparam name="TValue">Type of values for inserting into temp table.</typeparam>
        /// <param name="database">Korm database access.</param>
        /// <param name="values">Values for inserting into temp table.</param>
        /// <param name="actionAsync">Execute action.</param>
        /// <param name="commandTimeout">Command timeount in seconds. A value of 0 indicates no limit.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <remarks>
        /// <para>Whole action is executed in transaction and is asynchronous.</para>
        /// <para>
        /// The <paramref name="cancellationToken"/> is not passed to <paramref name="actionAsync"/>. To make the work
        /// done inside the callback cancellable, capture the token in the callback's closure and forward it
        /// to the asynchronous calls made there.
        /// </para>
        /// </remarks>
        public static async Task ExecuteWithTempTableAsync<TValue>(
            this IDatabase database,
            IEnumerable<TValue> values,
            Func<IDatabase, string, Task> actionAsync,
            int commandTimeout = TransactionHelper.TIMEOUT_DEFAULT,
            CancellationToken cancellationToken = default)
        {
            using ITransaction transaction = await BeginTransactionAsync(database, commandTimeout, cancellationToken);

            await actionAsync(database, await CreateTempTableAsync(database, values, cancellationToken));

            await TryCommitTransactionAsync(transaction, cancellationToken);
        }

        /// <summary>
        /// Special execute command, that will insert <paramref name="values"/> into temp table
        /// and then executes <paramref name="action"/> that is dependent on data in temp table.
        /// </summary>
        /// <typeparam name="TKey">Type of keys for inserting into temp table.</typeparam>
        /// <typeparam name="TValue">Type of values for inserting into temp table.</typeparam>
        /// <param name="database">Korm database access.</param>
        /// <param name="values">Values for inserting into temp table.</param>
        /// <param name="action">Execute action.</param>
        /// <param name="commandTimeout">Command timeount in seconds. A value of 0 indicates no limit.</param>
        /// <remarks>Whole action is executed in transaction.</remarks>
        public static void ExecuteWithTempTable<TKey, TValue>(
            this IDatabase database,
            IDictionary<TKey, TValue> values,
            Action<IDatabase, string> action,
            int commandTimeout = TransactionHelper.TIMEOUT_DEFAULT)
        {
            using ITransaction transaction = BeginTransaction(database, commandTimeout);

            action(database, CreateTempTable(database, values));

            TryCommitTransaction(transaction);
        }

        /// <summary>
        /// Special execute command, that will insert <paramref name="values"/> into temp table
        /// and then executes <paramref name="actionAsync"/> that is dependent on data in temp table.
        /// </summary>
        /// <typeparam name="TKey">Type of keys for inserting into temp table.</typeparam>
        /// <typeparam name="TValue">Type of values for inserting into temp table.</typeparam>
        /// <param name="database">Korm database access.</param>
        /// <param name="values">Values for inserting into temp table.</param>
        /// <param name="actionAsync">Execute action.</param>
        /// <param name="commandTimeout">Command timeount in seconds. A value of 0 indicates no limit.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <remarks>
        /// <para>Whole action is executed in transaction and is asynchronous.</para>
        /// <para>
        /// The <paramref name="cancellationToken"/> is not passed to <paramref name="actionAsync"/>. To make the work
        /// done inside the callback cancellable, capture the token in the callback's closure and forward it
        /// to the asynchronous calls made there.
        /// </para>
        /// </remarks>
        public static async Task ExecuteWithTempTableAsync<TKey, TValue>(
            this IDatabase database,
            IDictionary<TKey, TValue> values,
            Func<IDatabase, string, Task> actionAsync,
            int commandTimeout = TransactionHelper.TIMEOUT_DEFAULT,
            CancellationToken cancellationToken = default)
        {
            using ITransaction transaction = await BeginTransactionAsync(database, commandTimeout, cancellationToken);

            await actionAsync(database, await CreateTempTableAsync(database, values, cancellationToken));

            await TryCommitTransactionAsync(transaction, cancellationToken);
        }

        /// <summary>
        /// Special execute command, that will insert <paramref name="values"/> into temp table
        /// and then executes <paramref name="action"/> that is dependent on data in temp table.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <typeparam name="TValue">Type of values for inserting into temp table.</typeparam>
        /// <param name="database">Korm database access.</param>
        /// <param name="values">Values for inserting into temp table.</param>
        /// <param name="action">Execute action.</param>
        /// <param name="commandTimeout">Command timeount in seconds. A value of 0 indicates no limit.</param>
        /// <returns>User defined type <typeparamref name="T"/>.</returns>
        /// <remarks>Whole action is executed in transaction.</remarks>
        public static T ExecuteWithTempTable<T, TValue>(
            this IDatabase database,
            IEnumerable<TValue> values,
            Func<IDatabase, string, T> action,
            int commandTimeout = TransactionHelper.TIMEOUT_DEFAULT)
        {
            using ITransaction transaction = BeginTransaction(database, commandTimeout);

            T ret = action(database, CreateTempTable(database, values));

            TryCommitTransaction(transaction);

            return ret;
        }

        /// <summary>
        /// Special execute command, that will insert <paramref name="values"/> into temp table
        /// and then executes <paramref name="actionAsync"/> that is dependent on data in temp table.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <typeparam name="TValue">Type of values for inserting into temp table.</typeparam>
        /// <param name="database">Korm database access.</param>
        /// <param name="values">Values for inserting into temp table.</param>
        /// <param name="actionAsync">Execute action.</param>
        /// <param name="commandTimeout">Command timeount in seconds. A value of 0 indicates no limit.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>User defined type <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// <para>Whole action is executed in transaction and is asynchronous.</para>
        /// <para>
        /// The <paramref name="cancellationToken"/> is not passed to <paramref name="actionAsync"/>. To make the work
        /// done inside the callback cancellable, capture the token in the callback's closure and forward it
        /// to the asynchronous calls made there.
        /// </para>
        /// </remarks>
        public static async Task<T> ExecuteWithTempTableAsync<T, TValue>(
            this IDatabase database,
            IEnumerable<TValue> values,
            Func<IDatabase, string, Task<T>> actionAsync,
            int commandTimeout = TransactionHelper.TIMEOUT_DEFAULT,
            CancellationToken cancellationToken = default)
        {
            using ITransaction transaction = await BeginTransactionAsync(database, commandTimeout, cancellationToken);

            T ret = await actionAsync(database, await CreateTempTableAsync(database, values, cancellationToken));

            await TryCommitTransactionAsync(transaction, cancellationToken);

            return ret;
        }

        /// <summary>
        /// Special execute command, that will insert <paramref name="values"/> into temp table
        /// and then executes <paramref name="action"/> that is dependent on data in temp table.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <typeparam name="TKey">Type of keys for inserting into temp table.</typeparam>
        /// <typeparam name="TValue">Type of values for inserting into temp table.</typeparam>
        /// <param name="database">Korm database access.</param>
        /// <param name="values">Values for inserting into temp table.</param>
        /// <param name="action">Execute action.</param>
        /// <param name="commandTimeout">Command timeount in seconds. A value of 0 indicates no limit.</param>
        /// <returns>User defined type <typeparamref name="T"/>.</returns>
        /// <remarks>Whole action is executed in transaction.</remarks>
        public static T ExecuteWithTempTable<T, TKey, TValue>(
            this IDatabase database,
            IDictionary<TKey, TValue> values,
            Func<IDatabase, string, T> action,
            int commandTimeout = TransactionHelper.TIMEOUT_DEFAULT)
        {
            using ITransaction transaction = BeginTransaction(database, commandTimeout);

            T ret = action(database, CreateTempTable(database, values));

            TryCommitTransaction(transaction);

            return ret;
        }

        /// <summary>
        /// Special execute command, that will insert <paramref name="values"/> into temp table
        /// and then executes <paramref name="actionAsync"/> that is dependent on data in temp table.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <typeparam name="TKey">Type of keys for inserting into temp table.</typeparam>
        /// <typeparam name="TValue">Type of values for inserting into temp table.</typeparam>
        /// <param name="database">Korm database access.</param>
        /// <param name="values">Values for inserting into temp table.</param>
        /// <param name="actionAsync">Execute action.</param>
        /// <param name="commandTimeout">Command timeount in seconds. A value of 0 indicates no limit.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>User defined type <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// <para>Whole action is executed in transaction and is asynchronous.</para>
        /// <para>
        /// The <paramref name="cancellationToken"/> is not passed to <paramref name="actionAsync"/>. To make the work
        /// done inside the callback cancellable, capture the token in the callback's closure and forward it
        /// to the asynchronous calls made there.
        /// </para>
        /// </remarks>
        public static async Task<T> ExecuteWithTempTableAsync<T, TKey, TValue>(
            this IDatabase database,
            IDictionary<TKey, TValue> values,
            Func<IDatabase, string, Task<T>> actionAsync,
            int commandTimeout = TransactionHelper.TIMEOUT_DEFAULT,
            CancellationToken cancellationToken = default)
        {
            using ITransaction transaction = await BeginTransactionAsync(database, commandTimeout, cancellationToken);

            T ret = await actionAsync(database, await CreateTempTableAsync(database, values, cancellationToken));

            await TryCommitTransactionAsync(transaction, cancellationToken);

            return ret;
        }

        private static void TryCommitTransaction(ITransaction transaction)
        {
            try
            {
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static string CreateTempTable<TValue>(IDatabase database, IEnumerable<TValue> values)
        {
            string tempTableName = GetTempTableName();
            InsertValuesIntoTempTable(database, values, tempTableName);

            return tempTableName;
        }

        private static string CreateTempTable<TKey, TValue>(IDatabase database, IDictionary<TKey, TValue> values)
        {
            string tempTableName = GetTempTableName();
            InsertValuesIntoTempTable(database, values, tempTableName);

            return tempTableName;
        }

        private static string GetTempTableName()
            => $"#tt_{Guid.NewGuid():N}";

        private static void InsertValuesIntoTempTable<TValue>(
            IDatabase database,
            IEnumerable<TValue> values,
            string tempTableName)
        {
            TableInfo tableInfo = Database.DatabaseMapper.GetTableInfo<TValue>();
            string columns = GetColumnsWithSqlTypes(tableInfo, typeof(TValue));
            database.ExecuteNonQuery($"CREATE TABLE {tempTableName} ( {columns} )");

            using IBulkInsert bulkInsert = database.CreateBulkInsert();

            bulkInsert.DestinationTableName = tempTableName;

            using var reader = new EnumerableDataReader<TValue>(values, GetColumns(tableInfo, typeof(TValue)));

            bulkInsert.Insert(reader);
        }

        private static string GetColumnsWithSqlTypes(TableInfo tableInfo, Type type)
            => (type.IsPrimitive || type == typeof(string))
                ? $"Value {type.ToSqlDataType()}"
                : string.Join(
                    ",",
                    tableInfo.Columns.Select(c => $"[{c.PropertyInfo.Name}] {c.PropertyInfo.PropertyType.ToSqlDataType()}"));

        private static IEnumerable<string> GetColumns(TableInfo tableInfo, Type type)
            => (type.IsPrimitive || type == typeof(string))
                ? new string[] { "Value" }
                : tableInfo.Columns.Select(c => c.PropertyInfo.Name);

        private static void InsertValuesIntoTempTable<TKey, TValue>(
            IDatabase database,
            IDictionary<TKey, TValue> values,
            string tempTableName)
        {
            database.ExecuteNonQuery(
                $"CREATE TABLE {tempTableName}([Key] {typeof(TKey).ToSqlDataType()}, [Value] {typeof(TValue).ToSqlDataType()})");

            using IBulkInsert bulkInsert = database.CreateBulkInsert();
            bulkInsert.DestinationTableName = tempTableName;

            using var reader = new EnumerableDataReader<KeyValuePair<TKey, TValue>>(values, new string[] { "Key", "Value" });
            bulkInsert.Insert(reader);
        }

        private static ITransaction BeginTransaction(IDatabase database, int commandTimeout)
        {
            ITransaction transaction = database.BeginTransaction();

            if (commandTimeout != TransactionHelper.TIMEOUT_DEFAULT)
            {
                transaction.CommandTimeout = commandTimeout;
            }

            return transaction;
        }

        private static async Task TryCommitTransactionAsync(ITransaction transaction, CancellationToken cancellationToken)
        {
            try
            {
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                // Rollback must not be skipped just because the token has been cancelled.
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        }

        private static async Task<string> CreateTempTableAsync<TValue>(
            IDatabase database,
            IEnumerable<TValue> values,
            CancellationToken cancellationToken)
        {
            string tempTableName = GetTempTableName();
            await InsertValuesIntoTempTableAsync(database, values, tempTableName, cancellationToken);

            return tempTableName;
        }

        private static async Task<string> CreateTempTableAsync<TKey, TValue>(
            IDatabase database,
            IDictionary<TKey, TValue> values,
            CancellationToken cancellationToken)
        {
            string tempTableName = GetTempTableName();
            await InsertValuesIntoTempTableAsync(database, values, tempTableName, cancellationToken);

            return tempTableName;
        }

        private static async Task InsertValuesIntoTempTableAsync<TValue>(
            IDatabase database,
            IEnumerable<TValue> values,
            string tempTableName,
            CancellationToken cancellationToken)
        {
            TableInfo tableInfo = Database.DatabaseMapper.GetTableInfo<TValue>();
            string columns = GetColumnsWithSqlTypes(tableInfo, typeof(TValue));
            await database.ExecuteNonQueryAsync($"CREATE TABLE {tempTableName} ( {columns} )", cancellationToken);

            using IBulkInsert bulkInsert = database.CreateBulkInsert();

            bulkInsert.DestinationTableName = tempTableName;

            using var reader = new EnumerableDataReader<TValue>(values, GetColumns(tableInfo, typeof(TValue)));

            await bulkInsert.InsertAsync(reader, cancellationToken);
        }

        private static async Task InsertValuesIntoTempTableAsync<TKey, TValue>(
            IDatabase database,
            IDictionary<TKey, TValue> values,
            string tempTableName,
            CancellationToken cancellationToken)
        {
            await database.ExecuteNonQueryAsync(
                $"CREATE TABLE {tempTableName}([Key] {typeof(TKey).ToSqlDataType()}, [Value] {typeof(TValue).ToSqlDataType()})",
                cancellationToken);

            using IBulkInsert bulkInsert = database.CreateBulkInsert();
            bulkInsert.DestinationTableName = tempTableName;

            using var reader = new EnumerableDataReader<KeyValuePair<TKey, TValue>>(values, new string[] { "Key", "Value" });
            await bulkInsert.InsertAsync(reader, cancellationToken);
        }

        private static async Task<ITransaction> BeginTransactionAsync(
            IDatabase database,
            int commandTimeout,
            CancellationToken cancellationToken)
        {
            ITransaction transaction = await database.BeginTransactionAsync(cancellationToken);

            if (commandTimeout != TransactionHelper.TIMEOUT_DEFAULT)
            {
                transaction.CommandTimeout = commandTimeout;
            }

            return transaction;
        }
    }
}
