using Kros.Caching;
using Kros.KORM.Injection;
using System;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Mapper for database.
    /// Map object types to database informations.
    /// </summary>
    /// <seealso cref="Kros.KORM.Metadata.IDatabaseMapper" />
    public class DatabaseMapper : IDatabaseMapper
    {
        #region Private fields

        private ICache<Type, TableInfo> _tablesInfoCache = new Cache<Type, TableInfo>();
        private IModelMapper _modelMapper;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseMapper"/> class.
        /// </summary>
        /// <param name="modelMapper">The model mapper.</param>
        public DatabaseMapper(IModelMapper modelMapper)
        {
            _modelMapper = modelMapper;
        }

        #endregion

        /// <summary>
        /// Gets the table information by model type.
        /// </summary>
        /// <typeparam name="T">Model type.</typeparam>
        /// <returns>
        /// Database table info for model.
        /// </returns>
        public TableInfo GetTableInfo<T>() => GetTableInfo(typeof(T));

        /// <summary>
        /// Gets the table information by model type.
        /// </summary>
        /// <param name="modelType">Type of the model.</param>
        /// <returns>
        /// Database table info for model.
        /// </returns>
        public TableInfo GetTableInfo(Type modelType) => _tablesInfoCache.Get(modelType, () => _modelMapper.GetTableInfo(modelType));

        /// <summary>
        /// Get property service injector.
        /// </summary>
        /// <typeparam name="T">Model type.</typeparam>
        /// <returns>Service property injector.</returns>
        public IInjector GetInjector<T>() =>_modelMapper.GetInjector<T>();
    }
}
