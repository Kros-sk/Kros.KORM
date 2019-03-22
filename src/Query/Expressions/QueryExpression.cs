using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Kros.KORM.Query.Expressions
{
    /// <summary>
    /// Base expression.
    /// </summary>
    /// <seealso cref="System.Linq.Expressions.Expression" />
    public abstract class QueryExpression : Expression
    {
        /// <summary>
        /// Gets the node type of this <see cref="T:System.Linq.Expressions.Expression"></see>.
        /// </summary>
        public override ExpressionType NodeType => ExpressionType.Constant;

        /// <summary>
        /// Gets the static type of the expression that this <see cref="T:System.Linq.Expressions.Expression"></see> represents.
        /// </summary>
        public override Type Type => typeof(string);
    }
}
