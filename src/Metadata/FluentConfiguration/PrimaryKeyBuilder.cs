using Kros.KORM.Metadata.FluentConfiguration;
using Kros.Utils;
using System;
using System.Linq.Expressions;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Provide a simple fluent API for definition primary key.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    public class PrimaryKeyBuilder<TEntity> where TEntity : class
    {
        private readonly EntityTypeBuilder<TEntity> _entityTypeBuilder;
        private readonly string _propertyName;
        private AutoIncrementMethodType? _autoIncrementMethodType;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="entityTypeBuilder">Entity type builder.</param>
        /// <param name="propertyName">Name of property which represents primary key.</param>
        internal PrimaryKeyBuilder(EntityTypeBuilder<TEntity> entityTypeBuilder, string propertyName)
        {
            _entityTypeBuilder = Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
            _propertyName = Check.NotNullOrEmpty(propertyName, nameof(propertyName));
        }

        /// <summary>
        /// Configure autoincrement for primary key.
        /// </summary>
        /// <param name="autoIncrementMethodType">Autoincrement method type.</param>
        public PrimaryKeyBuilder<TEntity> AutoIncrement(
            AutoIncrementMethodType autoIncrementMethodType = AutoIncrementMethodType.Identity)
        {
            ExceptionHelper.CheckMultipleTimeCalls(() => _autoIncrementMethodType != null);

            _autoIncrementMethodType = autoIncrementMethodType;

            return this;
        }

        /// <summary>
        /// Returns an object that can be used to configure a property of the entity type.
        /// </summary>
        /// <typeparam name="TProperty">Propery type.</typeparam>
        /// <param name="propertyExpression">A lambda expression representing the property to be configured.</param>
        /// <returns>An object that can be used to configure the property.</returns>
        public virtual PropertyBuilder<TEntity> Property<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyExpression)
            => _entityTypeBuilder.Property(propertyExpression);

        internal void Build(IModelMapperInternal modelMapper)
        {
            modelMapper.SetPrimaryKey<TEntity>(_propertyName,
                _autoIncrementMethodType != null ? _autoIncrementMethodType.Value : AutoIncrementMethodType.None);
        }
    }
}
