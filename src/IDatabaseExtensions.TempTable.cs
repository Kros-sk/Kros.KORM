using Kros.Data.BulkActions;
using Kros.KORM.Data;
using Kros.KORM.Extensions;
using Kros.KORM.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <remarks>Whole action is executed in transaction.</remarks>
        public static void ExecuteWithTempTable<TValue>(
            this IDatabase database,
            IEnumerable<TValue> values,
            Action<IDatabase, string> action)
        {
            using ITransaction transaction = database.BeginTransaction();

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
        /// <remarks>Whole action is executed in transaction and is asynchronous.</remarks>
        public static async Task ExecuteWithTempTableAsync<TValue>(
            this IDatabase database,
            IEnumerable<TValue> values,
            Func<IDatabase, string, Task> actionAsync)
        {
            using ITransaction transaction = database.BeginTransaction();

            await actionAsync(database, CreateTempTable(database, values));

            TryCommitTransaction(transaction);
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
        /// <remarks>Whole action is executed in transaction.</remarks>
        public static void ExecuteWithTempTable<TKey, TValue>(
            this IDatabase database,
            IDictionary<TKey, TValue> values,
            Action<IDatabase, string> action)
        {
            using ITransaction transaction = database.BeginTransaction();

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
        /// <remarks>Whole action is executed in transaction and is asynchronous.</remarks>
        public static async Task ExecuteWithTempTableAsync<TKey, TValue>(
            this IDatabase database,
            IDictionary<TKey, TValue> values,
            Func<IDatabase, string, Task> actionAsync)
        {
            using ITransaction transaction = database.BeginTransaction();

            await actionAsync(database, CreateTempTable(database, values));

            TryCommitTransaction(transaction);
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
        /// <returns>User defined type <typeparamref name="T"/>.</returns>
        /// <remarks>Whole action is executed in transaction.</remarks>
        public static T ExecuteWithTempTable<T, TValue>(
            this IDatabase database,
            IEnumerable<TValue> values,
            Func<IDatabase, string, T> action)
        {
            using ITransaction transaction = database.BeginTransaction();

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
        /// <returns>User defined type <typeparamref name="T"/>.</returns>
        /// <remarks>Whole action is executed in transaction and is asynchronous.</remarks>
        public static async Task<T> ExecuteWithTempTableAsync<T, TValue>(
            this IDatabase database,
            IEnumerable<TValue> values,
            Func<IDatabase, string, Task<T>> actionAsync)
        {
            using ITransaction transaction = database.BeginTransaction();

            T ret = await actionAsync(database, CreateTempTable(database, values));

            TryCommitTransaction(transaction);

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
        /// <returns>User defined type <typeparamref name="T"/>.</returns>
        /// <remarks>Whole action is executed in transaction.</remarks>
        public static T ExecuteWithTempTable<T, TKey, TValue>(
            this IDatabase database,
            IDictionary<TKey, TValue> values,
            Func<IDatabase, string, T> action)
        {
            using ITransaction transaction = database.BeginTransaction();

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
        /// <returns>User defined type <typeparamref name="T"/>.</returns>
        /// <remarks>Whole action is executed in transaction and is asynchronous.</remarks>
        public static async Task<T> ExecuteWithTempTableAsync<T, TKey, TValue>(
            this IDatabase database,
            IDictionary<TKey, TValue> values,
            Func<IDatabase, string, Task<T>> actionAsync)
        {
            using ITransaction transaction = database.BeginTransaction();

            T ret = await actionAsync(database, CreateTempTable(database, values));

            TryCommitTransaction(transaction);

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
                    ',',
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
    }
}
