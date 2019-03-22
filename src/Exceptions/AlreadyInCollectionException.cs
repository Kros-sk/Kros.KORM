using System;

namespace Kros.KORM.Exceptions
{
    /// <summary>
    /// Exception class for item already exists in the collection.
    /// </summary>
    public class AlreadyInCollectionException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AlreadyInCollectionException"/> class.
        /// </summary>
        public AlreadyInCollectionException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AlreadyInCollectionException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public AlreadyInCollectionException(string message) : base(message)
        {
        }
    }
}