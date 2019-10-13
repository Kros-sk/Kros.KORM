using Kros.Utils;
using System;
using System.Linq.Expressions;

namespace Kros.KORM.Metadata.FluentConfiguration
{
    /// <summary>
    /// Interface for configuration of behavior for all entity for table.
    /// </summary>
    /// <seealso cref="Kros.KORM.Metadata.ITableBuilder" />
    internal class TableBuilder : ITableBuilder
    {
        private Expression _queryFilter;
        private readonly string _tableName;

        public TableBuilder(string tableName)
        {
            _tableName = Check.NotNullOrWhiteSpace(tableName, nameof(tableName));
        }

        void ITableBuilder.UseQueryFilter<TEntity>(Expression<Func<TEntity, bool>> queryFilter)
        {
            _queryFilter = Check.NotNull(queryFilter, nameof(queryFilter));
        }

        public void Build(IModelMapperInternal modelMapper)
        {
            if (_queryFilter != null)
            {
                modelMapper.SetQueryFilter(_tableName, _queryFilter);
            }
        }
    }
}
