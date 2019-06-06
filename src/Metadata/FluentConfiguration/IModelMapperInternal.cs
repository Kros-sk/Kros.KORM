using Kros.KORM.Converter;
using Kros.KORM.Injection;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Internal model mapper interface for fluent configuration.
    /// </summary>
    internal interface IModelMapperInternal
    {
        /// <summary>
        /// Sets table name for <typeparamref name="TEntity"/>.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <param name="tableName">Database table name.</param>
        void SetTableName<TEntity>(string tableName) where TEntity: class;

        /// <summary>
        /// Sets column name for specific property.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <param name="propertyName">Property name.</param>
        /// <param name="columnName">Database column name.</param>
        void SetColumnName<TEntity>(string propertyName, string columnName) where TEntity : class;

        /// <summary>
        /// Sets no map flag for property <paramref name="propertyName"/>.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <param name="propertyName">Property name.</param>
        void SetNoMap<TEntity>(string propertyName) where TEntity : class;

        /// <summary>
        /// Sets converter for specific property.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <param name="propertyName">Property name.</param>
        /// <param name="converter">Converter.</param>
        void SetConverter<TEntity>(string propertyName, IConverter converter) where TEntity : class;

        /// <summary>
        /// Set injector for <typeparamref name="TEntity"/>.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <param name="injector">Injector.</param>
        void SetInjector<TEntity>(IInjector injector) where TEntity : class;

        /// <summary>
        /// Set primary key for entity <typeparamref name="TEntity"/>.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <param name="propertyName">Property name, which represent primary key.</param>
        /// <param name="autoIncrementType">Autoincrement method type.</param>
        void SetPrimaryKey<TEntity>(string propertyName, AutoIncrementMethodType autoIncrementType) where TEntity : class;
    }
}
