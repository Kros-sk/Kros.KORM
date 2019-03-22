using Kros.Utils;
using System;

namespace Kros.KORM.Metadata.Attribute
{
    /// <summary>
    /// Attribute which describe database name of property/class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
    public class AliasAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AliasAttribute"/> class.
        /// </summary>
        /// <param name="alias">The database alias.</param>
        /// <exception cref="ArgumentNullException">The value of <paramref name="alias"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The value of <paramref name="alias"/> is empty string, or stirng
        /// containing whitespace characters only.</exception>
        public AliasAttribute(string alias)
        {
            Alias = Check.NotNullOrWhiteSpace(alias, nameof(alias));
        }

        /// <summary>
        /// Database alias
        /// </summary>
        public string Alias { get; }
    }
}
