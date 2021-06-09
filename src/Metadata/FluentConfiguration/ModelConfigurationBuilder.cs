using Kros.KORM.Metadata.FluentConfiguration;
using Kros.Utils;
using System;
using System.Collections.Generic;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Provides a simple fluent API for building mapping definition between CLR entities and database objects.
    /// </summary>
    public class ModelConfigurationBuilder
    {
        private Delimiters _delimiters = Delimiters.Empty;
        private readonly Dictionary<Type, EntityTypeBuilderBase> _entityBuilders = new Dictionary<Type, EntityTypeBuilderBase>();
        private readonly Dictionary<string, TableBuilder> _tableBuilders
            = new Dictionary<string, TableBuilder>(StringComparer.OrdinalIgnoreCase);

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
        /// Returns an object that can be used to configure behavior for all entities over table <paramref name="tableName"/>.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <returns>
        /// An object that can be used to configure behavior for all entities over table <paramref name="tableName"/>.
        /// </returns>
        public ITableBuilder Table(string tableName)
        {
            Check.NotNullOrWhiteSpace(tableName, nameof(tableName));

            if (!_tableBuilders.TryGetValue(tableName, out TableBuilder builder))
            {
                builder = new TableBuilder(tableName);
                _tableBuilders[tableName] = builder;
            }

            return builder;
        }

        /// <summary>
        /// Use delimieters for identifiers in the generated query.
        /// </summary>
        /// <param name="delimiters">The delimiters.</param>
        public void UseIdentifierDelimiters(Delimiters delimiters)
            => _delimiters = Check.NotNull(delimiters, nameof(delimiters));

        /// <summary>
        /// Builds model configuration.
        /// </summary>
        /// <param name="modelMapper">Model mapper.</param>
        internal void Build(IModelMapperInternal modelMapper)
        {
            modelMapper.UseIdentifierDelimiters(_delimiters);
            foreach (EntityTypeBuilderBase entityBuilder in _entityBuilders.Values)
            {
                entityBuilder.Build(modelMapper);
            }

            foreach (TableBuilder tableBuilder in _tableBuilders.Values)
            {
                tableBuilder.Build(modelMapper);
            }
        }
    }
}
