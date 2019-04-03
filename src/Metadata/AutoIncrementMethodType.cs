using Kros.KORM.Query;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Type of primary key auto increment method.
    /// </summary>
    public enum AutoIncrementMethodType
    {
        /// <summary>
        /// The primary key is not auto incremented.
        /// </summary>
        None = 0,

        /// <summary>
        /// KORM generate primary key for entity.
        /// </summary>
        Custom = 1,

        /// <summary>
        /// Sql server generates the primary key and KORM fills it into the entity.
        /// When calling <see cref="IDbSet{T}.BulkInsert()"/> keys are not fill back to the entities.
        /// </summary>
        Identity = 2
    }
}
