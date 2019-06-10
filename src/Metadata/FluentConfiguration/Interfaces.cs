using Kros.KORM.Converter;
using System;
using System.Linq.Expressions;

namespace Kros.KORM.Metadata
{
    public interface IEntityTypeBuilder<TEntity>
        : INamedEntityTypeBuilder<TEntity> where TEntity : class
    {
        INamedEntityTypeBuilder<TEntity> HasTableName(string tableName);
    }

    public interface INamedEntityTypeBuilder<TEntity>
        : IEntityPropertyBuilder<TEntity> where TEntity : class
    {
        IPrimaryKeyBuilder<TEntity> HasPrimaryKey<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression);
    }

    public interface IEntityPropertyBuilder<TEntity> where TEntity : class
    {
        IPropertyBuilder<TEntity> Property<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression);
    }

    public interface IPrimaryKeyBuilder<TEntity> where TEntity : class
    {
        IEntityPropertyBuilder<TEntity> AutoIncrement(
            AutoIncrementMethodType autoIncrementType = AutoIncrementMethodType.Identity);
        IPropertyBuilder<TEntity> Property<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression);
    }

    public interface IPropertyBuilder<TEntity>
        : INamedPropertyBuilder<TEntity> where TEntity : class
    {
        IEntityPropertyBuilder<TEntity> NoMap();
        INamedPropertyBuilder<TEntity> HasColumnName(string columnName);
    }

    public interface INamedPropertyBuilder<TEntity>
        : IMappedPropertyBuilder<TEntity> where TEntity : class
    {
        IPropertyBuilder<TEntity> Property<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression);
    }

    public interface IMappedPropertyBuilder<TEntity> where TEntity : class
    {
        IEntityPropertyBuilder<TEntity> UseConverter<TCoverter>() where TCoverter : IConverter, new();
        IEntityPropertyBuilder<TEntity> UseConverter(IConverter converter);
        IEntityPropertyBuilder<TEntity> InjectValue(Func<object> injector);
    }
}
