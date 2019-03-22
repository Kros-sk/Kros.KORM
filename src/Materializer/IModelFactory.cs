using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Kros.KORM.Materializer
{
    /// <summary>
    /// Interface for factory, which know materialize model from database.
    /// </summary>
    public interface IModelFactory
    {
        /// <summary>
        /// Gets the factory for creating and filling model.
        /// </summary>
        /// <typeparam name="T">Type of model class.</typeparam>
        /// <param name="reader">Reader from fill model.</param>
        /// <returns>
        /// Factory for creating and filling model.
        /// </returns>
        Func<IDataReader, T> GetFactory<T>(IDataReader reader);
    }
}
