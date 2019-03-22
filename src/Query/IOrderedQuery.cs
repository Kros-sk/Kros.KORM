using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kros.KORM.Query
{
    /// <summary>
    /// Represents result of a sorting operation.
    /// </summary>
    /// <typeparam name="T">Type of model class.</typeparam>
    /// <seealso cref="Kros.KORM.Query.IQueryBase{T}" />
    public interface IOrderedQuery<T> : IQueryBase<T>
    {
    }
}
