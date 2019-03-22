using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Kros.KORM.Materializer
{
    /// <summary>
    /// Specifies that this object supports notification about materializing.
    /// </summary>
    public interface IMaterialize
    {
        /// <summary>
        /// Called when model is materialized.
        /// </summary>
        /// <param name="source">The source, which from was model materialized.</param>
        void OnAfterMaterialize(IDataRecord source);
    }
}
