using System;

namespace Kros.KORM.Exceptions
{
    /// <summary>
    /// Exception class for composite primary key.
    /// </summary>
    public class CompositePrimaryKeyException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompositePrimaryKeyException" /> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="tableName">Table name which has composite primary key.</param>
        public CompositePrimaryKeyException(string message, string tableName) : base(message)
        {
            TableName = tableName;
        }

        /// <summary>
        /// Table name which has composite primary key.
        /// </summary>
        public string TableName { get; }
    }
}