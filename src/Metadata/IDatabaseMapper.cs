using Kros.KORM.Injection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Interface, which describe mapper for database.
    /// Map object types to database informations.
    /// </summary>
    public interface IDatabaseMapper
    {
        /// <summary>
        /// Gets the table information by model type.
        /// </summary>
        /// <typeparam name="T">Model type.</typeparam>
        /// <returns>
        /// Database table info for model.
        /// </returns>
        TableInfo GetTableInfo<T>();

        /// <summary>
        /// Gets the table information by model type.
        /// </summary>
        /// <param name="modelType">Type of the model.</param>
        /// <returns>
        /// Database table info for model.
        /// </returns>
        TableInfo GetTableInfo(Type modelType);

        /// <summary>
        /// Get property service injector.
        /// </summary>
        /// <typeparam name="T">Model type.</typeparam>
        /// <returns>Service property injector.</returns>
        IInjector GetInjector<T>();
    }
}
