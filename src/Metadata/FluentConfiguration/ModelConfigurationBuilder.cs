using Kros.KORM.Metadata.FluentConfiguration;
using System;
using System.Collections.Generic;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Provide a simple fluent API for building mapping definition between CLR entities and database objects.
    /// </summary>
    public class ModelConfigurationBuilder
    {
        private Dictionary<Type, EntityTypeBuilderInternal> _entityBuilders = new Dictionary<Type, EntityTypeBuilderInternal>();

        /// <summary>
        /// Returns an object that can be used to configure of a given entity type.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <returns>An object that can be used to configure of a given entity type.</returns>
        public EntityTypeBuilder<TEntity> Entity<TEntity>() where TEntity : class
        {
            var entityType = typeof(TEntity);
            EntityTypeBuilder<TEntity> entityBuilder;

            if (_entityBuilders.ContainsKey(entityType))
            {
                entityBuilder = _entityBuilders[entityType] as EntityTypeBuilder<TEntity>;
            }
            else
            {
                entityBuilder = new EntityTypeBuilder<TEntity>();
                _entityBuilders[entityType] = entityBuilder;
            }

            return entityBuilder;
        }

        /// <summary>
        /// Build model configuration.
        /// </summary>
        /// <param name="modelMapper">Model mapper.</param>
        internal void Build(IModelMapperInternal modelMapper)
        {
            foreach (var entityBuilder in _entityBuilders.Values)
            {
                entityBuilder.Build(modelMapper);
            }
        }
    }
}
