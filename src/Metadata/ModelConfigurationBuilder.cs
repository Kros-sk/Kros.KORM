using System;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Provide a simple fluent API for building mapping definition between CLR entities and database objects.
    /// </summary>
    public class ModelConfigurationBuilder
    {
        /// <summary>
        /// Returns an object that can be used to configure of a given entity type.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <returns>An object that can be used to configure of a given entity type.</returns>
        public EntityTypeBuilder<TEntity> Entity<TEntity>() where TEntity : class
        {
            throw new NotImplementedException();
        }
    }
}
