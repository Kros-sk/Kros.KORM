using Kros.KORM.Converter;
using Kros.KORM.Data;
using System;
using System.Linq.Expressions;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Builder for fluent configuration of entity.
    /// </summary>
    /// <typeparam name="TEntity">Type of entity. In general it is a class representing table in database.</typeparam>
    public interface IEntityTypeBuilder<TEntity>
        : INamedEntityTypeBuilder<TEntity> where TEntity : class
    {
        /// <summary>
        /// Configures table name in databse for entity. If table name is not explicitly configured, the name of
        /// <typeparamref name="TEntity"/> is used.
        /// </summary>
        /// <param name="tableName">Database table name.</param>
        /// <returns>Returns builder for additional configuration of the entity.</returns>
        INamedEntityTypeBuilder<TEntity> HasTableName(string tableName);
    }

    /// <summary>
    /// Builder for fluent configuration of entity, which has alredy configured table name.
    /// </summary>
    /// <typeparam name="TEntity">Type of entity. In general it is a class representing table in database.</typeparam>
    public interface INamedEntityTypeBuilder<TEntity>
        : IEntityTypeConvertersBuilder<TEntity> where TEntity : class
    {
        /// <summary>
        /// Configures which property represents primary key of the entity.
        /// </summary>
        /// <typeparam name="TProperty">Property of the entity.</typeparam>
        /// <param name="propertyExpression">Expression which returns property representing primary key.</param>
        /// <returns>Builder for further configuring of primary key.</returns>
        IPrimaryKeyBuilder<TEntity> HasPrimaryKey<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression);
    }

    /// <summary>
    /// Builder for fluent configuration of entity, which allows configuration of data converters (<see cref="IConverter"/>).
    /// </summary>
    /// <typeparam name="TEntity">Type of entity. In general it is a class representing table in database.</typeparam>
    public interface IEntityTypeConvertersBuilder<TEntity>
        : IEntityTypePropertyBuilder<TEntity> where TEntity : class
    {
        /// <summary>
        /// Configures <paramref name="converter"/> for all properties of specified type <typeparamref name="TProperty"/>.
        /// </summary>
        /// <typeparam name="TProperty">Type of the property.</typeparam>
        /// <param name="converter">Converter to be used for all properties of given type.</param>
        /// <returns>Returns self, for additional configuration of the entity.</returns>
        IEntityTypeConvertersBuilder<TEntity> UseConverterForProperties<TProperty>(IConverter converter);

        /// <summary>
        /// Configures converter of type <typeparamref name="TConverter"/> for all properties of specified type
        /// <typeparamref name="TProperty"/>.
        /// </summary>
        /// <typeparam name="TProperty">Type of the property.</typeparam>
        /// <typeparam name="TConverter">Type of the converter to be used for all properties of given type.
        /// Instance of that type is created.</typeparam>
        /// <returns>Returns self, for additional configuration of the entity.</returns>
        IEntityTypeConvertersBuilder<TEntity> UseConverterForProperties<TProperty, TConverter>()
            where TConverter : IConverter, new();
    }

    /// <summary>
    /// Builder for fluent configuration of entity, which allows configuration of individual entity's properties.
    /// </summary>
    /// <typeparam name="TEntity">Type of entity. In general it is a class representing table in database.</typeparam>
    public interface IEntityTypePropertyBuilder<TEntity> where TEntity : class
    {
        /// <summary>
        /// Allows configuration of specific entity's property.
        /// </summary>
        /// <typeparam name="TProperty">Property of the entity.</typeparam>
        /// <param name="propertyExpression">Expression which returns property to be configured.</param>
        /// <returns>Builder for configuration of specific entity property.</returns>
        IPropertyBuilder<TEntity> Property<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression);
    }

    /// <summary>
    /// Builder for fluent configuration of entity, which allows configuration of primary key.
    /// </summary>
    /// <typeparam name="TEntity">Type of entity. In general it is a class representing table in database.</typeparam>
    public interface IPrimaryKeyBuilder<TEntity>
        : IEntityTypePropertyBuilder<TEntity> where TEntity : class
    {
        /// <summary>
        /// Configures auto-increment type of primary key. If
        /// </summary>
        /// <param name="autoIncrementType">Auto-increment type of the primary key. If called without specifying the value,
        /// <see cref="AutoIncrementMethodType">AutoIncrementMethodType.Identity</see> will be used.</param>
        /// <returns>Returns builder for additional configuration of the entity.</returns>
        IEntityTypeConvertersBuilder<TEntity> AutoIncrement(
            AutoIncrementMethodType autoIncrementType = AutoIncrementMethodType.Identity);
    }

    /// <summary>
    /// Builder for fluent configuration of entity's property.
    /// </summary>
    /// <typeparam name="TEntity">Type of entity. In general it is a class representing table in database.</typeparam>
    public interface IPropertyBuilder<TEntity>
        : IMappedPropertyBuilder<TEntity> where TEntity : class
    {
        /// <summary>
        /// Configures property as no mapping. The property will be ignored by KORM.
        /// </summary>
        /// <returns>Entity builder for configuring another properties.</returns>
        IEntityTypePropertyBuilder<TEntity> NoMap();

        /// <summary>
        /// Configures column name for the property. Needed when column in the database has different name than
        /// the property itself.
        /// </summary>
        /// <param name="columnName">Name of the column in database.</param>
        /// <returns>Builder for additional configuration of the property.</returns>
        INamedPropertyBuilder<TEntity> HasColumnName(string columnName);
    }

    /// <summary>
    /// Builder for fluent configuration of entity's property.
    /// </summary>
    /// <typeparam name="TEntity">Type of entity. In general it is a class representing table in database.</typeparam>
    public interface INamedPropertyBuilder<TEntity>
        : IMappedPropertyBuilder<TEntity> where TEntity : class
    {
        /// <summary>
        /// Allows configuration of specific entity's property.
        /// </summary>
        /// <typeparam name="TProperty">Property of the entity.</typeparam>
        /// <param name="propertyExpression">Expression which returns property to be configured.</param>
        /// <returns>Builder for configuration of specific entity property.</returns>
        IPropertyBuilder<TEntity> Property<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression);
    }

    /// <summary>
    /// Builder for fluent configuration of entity's property which is mapped to table in database.
    /// </summary>
    /// <typeparam name="TEntity">Type of entity. In general it is a class representing table in database.</typeparam>
    public interface IMappedPropertyBuilder<TEntity> where TEntity : class
    {
        /// <summary>
        /// Configures converter of type <typeparamref name="TCoverter"/> for the property.
        /// The instance of the converter is created.
        /// </summary>
        /// <typeparam name="TCoverter">Type of the converter to be used for current property.</typeparam>
        /// <returns>Entity builder for configuring another properties.</returns>
        IEntityTypePropertyBuilder<TEntity> UseConverter<TCoverter>() where TCoverter : IConverter, new();

        /// <summary>
        /// Configures <paramref name="converter"/> for the property.
        /// </summary>
        /// <param name="converter">Converter to be used for current property.</param>
        /// <returns>Entity builder for configuring another properties.</returns>
        IEntityTypePropertyBuilder<TEntity> UseConverter(IConverter converter);

        /// <summary>
        /// Configures property to ignore the converter. It is useful, if default converter is set for all properties
        /// of some type (<see cref="IEntityTypeConvertersBuilder{TEntity}.UseConverterForProperties{TProperty}(IConverter)"/>),
        /// but need to be ignored by some of the affected properties.
        /// </summary>
        /// <returns>Entity builder for configuring another properties.</returns>
        IEntityTypePropertyBuilder<TEntity> IgnoreConverter();

        /// <summary>
        /// Configures <paramref name="generator"/> for the property used when an entity is added to the database.
        /// </summary>
        /// <param name="generator">Value generator to be used for current property.</param>
        /// <returns>Entity builder for configuring another properties.</returns>
        IEntityTypePropertyBuilder<TEntity> UseValueGeneratorOnInsert(IValueGenerator generator);

        /// <summary>
        /// Configures <paramref name="generator"/> for the property used when an entity is updated to the database.
        /// </summary>
        /// <param name="generator">Value generator to be used for current property.</param>
        /// <returns>Entity builder for configuring another properties.</returns>
        IEntityTypePropertyBuilder<TEntity> UseValueGeneratorOnUpdate(IValueGenerator generator);

        /// <summary>
        /// Configures <paramref name="generator"/> for the property used when an entity is added or updated to the database.
        /// </summary>
        /// <param name="generator">Value generator to be used for current property.</param>
        /// <returns>Entity builder for configuring another properties.</returns>
        IEntityTypePropertyBuilder<TEntity> UseValueGeneratorOnInsertOrUpdate(IValueGenerator generator);

        /// <summary>
        /// Configures injector delegate for injecting values to property.
        /// </summary>
        /// <param name="injector">Delegate for injecting values to property.</param>
        /// <returns>Entity builder for configuring another properties.</returns>
        IEntityTypePropertyBuilder<TEntity> InjectValue(Func<object> injector);
    }
}
