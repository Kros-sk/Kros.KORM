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
        : IEntityTypePropertyBuilder<TEntity> where TEntity : class
    {
        IPrimaryKeyBuilder<TEntity> HasPrimaryKey<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression);
    }

    public interface IEntityTypePropertyBuilder<TEntity> where TEntity : class
    {
        IPropertyBuilder<TEntity> Property<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression);
    }

    public interface IPrimaryKeyBuilder<TEntity>
        : IEntityTypePropertyBuilder<TEntity> where TEntity : class
    {
        IEntityTypePropertyBuilder<TEntity> AutoIncrement(
            AutoIncrementMethodType autoIncrementType = AutoIncrementMethodType.Identity);
    }

    public interface IPropertyBuilder<TEntity>
        : IMappedPropertyBuilder<TEntity> where TEntity : class
    {
        IEntityTypePropertyBuilder<TEntity> NoMap();
        INamedPropertyBuilder<TEntity> HasColumnName(string columnName);
    }

    public interface INamedPropertyBuilder<TEntity>
        : IMappedPropertyBuilder<TEntity> where TEntity : class
    {
        IPropertyBuilder<TEntity> Property<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression);
    }

    public interface IMappedPropertyBuilder<TEntity> where TEntity : class
    {
        IEntityTypePropertyBuilder<TEntity> UseConverter<TCoverter>() where TCoverter : IConverter, new();
        IEntityTypePropertyBuilder<TEntity> UseConverter(IConverter converter);
        IEntityTypePropertyBuilder<TEntity> InjectValue(Func<object> injector);
    }
}
