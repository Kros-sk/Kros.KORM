using System;

namespace Kros.KORM.Exceptions
{
    /// <summary>
    /// Exception class for missing primary key.
    /// </summary>
    public class MissingPrimaryKeyException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MissingPrimaryKeyException" /> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="tableName">Table name which doesn't have primary key.</param>
        public MissingPrimaryKeyException(string message, string tableName) : base(message)
        {
            TableName = tableName;
        }

        /// <summary>
        /// Table name which doesn't have primary key.
        /// </summary>
        public string TableName { get; }
    }
}