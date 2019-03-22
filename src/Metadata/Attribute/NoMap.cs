using System;

namespace Kros.KORM.Metadata.Attribute
{
    /// <summary>
    /// Attribute, which describe property, which doesn't exist in database.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class NoMapAttribute : System.Attribute
    {
    }
}
