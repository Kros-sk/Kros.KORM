using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Kros.KORM.Materializer
{
    /// <summary>
    /// Class, which generate key from reader for caching factory delegates.
    /// </summary>
    internal class ReaderKeyGenerator
    {
        /// <summary>
        /// Generates the key.
        /// </summary>
        /// <param name="dataReader">The data reader.</param>
        /// <returns>
        /// Key, which reprezent reeaders, which same fields.
        /// </returns>
        /// <typeparam name="T">Table type.</typeparam>
        public int GenerateKey<T>(IDataReader dataReader)
        {
            return this.GetKey(dataReader, typeof(T).FullName).GetHashCode();
        }

        private string GetKey(IDataReader dataReader, string fullName)
        {
            var sb = new StringBuilder(fullName);

            for (int i = 0; i < dataReader.FieldCount; i++)
            {
                sb.Append($"{dataReader.GetName(i)}{dataReader.GetFieldType(i).Name}");
            }

            return sb.ToString().ToUpper();
        }
    }
}
