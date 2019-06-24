using System;
using System.Collections.Generic;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Provides a simple fluent API for building mapping definition between CLR entities and database objects.
    /// </summary>
    public class ModelConfigurationBuilder
    {
        private readonly Dictionary<Type, EntityTypeBuilderBase> _entityBuilders = new Dictionary<Type, EntityTypeBuilderBase>();

        /// <summary>
        /// Returns an object that can be used to configure of a given entity type.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <returns>An object that can be used to configure of a given entity type.</returns>
        public IEntityTypeBuilder<TEntity> Entity<TEntity>() where TEntity : class
        {
            Type entityType = typeof(TEntity);

            if (!(_entityBuilders.TryGetValue(entityType, out EntityTypeBuilderBase eb)
                && eb is EntityTypeBuilder<TEntity> entityBuilder))
            {
                entityBuilder = new EntityTypeBuilder<TEntity>();
                _entityBuilders[entityType] = entityBuilder;
            }

            return entityBuilder;
        }

        /// <summary>
        /// Builds model configuration.
        /// </summary>
        /// <param name="modelMapper">Model mapper.</param>
        internal void Build(IModelMapperInternal modelMapper)
        {
            foreach (EntityTypeBuilderBase entityBuilder in _entityBuilders.Values)
            {
                entityBuilder.Build(modelMapper);
            }
        }
    }
}
