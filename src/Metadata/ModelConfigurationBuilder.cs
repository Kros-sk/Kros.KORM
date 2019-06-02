using System;
using System.Collections.Generic;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Provide a simple fluent API for building mapping definition between CLR entities and database objects.
    /// </summary>
    public class ModelConfigurationBuilder
    {
        private Dictionary<Type, EntityTypeBuilderInternal> _entityBuilders;

        /// <summary>
        /// Returns an object that can be used to configure of a given entity type.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <returns>An object that can be used to configure of a given entity type.</returns>
        public EntityTypeBuilder<TEntity> Entity<TEntity>() where TEntity : class
        {
            var entityType = typeof(TEntity);
            if (_entityBuilders.ContainsKey(entityType))
            {
                throw new InvalidOperationException("Nie nie toto nemôžeš.");
            }

            var entityBuilder = new EntityTypeBuilder<TEntity>();
            _entityBuilders[entityType] = entityBuilder;

            return entityBuilder;
        }
    }
}
