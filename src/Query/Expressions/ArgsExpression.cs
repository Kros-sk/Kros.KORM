using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Kros.KORM.Query.Expressions
{
    /// <summary>
    /// Base class for args expression.
    /// </summary>
    /// <seealso cref="System.Linq.Expressions.Expression" />
    public abstract class ArgsExpression: QueryExpression
    {
        /// <summary>
        /// Sql.
        /// </summary>
        public string Sql { get; protected set; }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        public IEnumerable<object> Parameters { get; protected set; }
    }
}
