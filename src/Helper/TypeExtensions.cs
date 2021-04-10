using System;
using System.Reflection;

namespace Kros.KORM.Helper
{
    /// <summary>
    /// Type extensions.
    /// </summary>
    internal static class TypeExtensions
    {
        /// <summary>
        /// Gets the constructor info.
        /// </summary>
        /// <param name="type">The type.</param>
        public static (ConstructorInfo ctor, bool isDefault) GetConstructor(this Type type)
        {
            ConstructorInfo ctor = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, Type.EmptyTypes, null);

            if (ctor is not null)
            {
                return (ctor, true);
            }

            ConstructorInfo[] ctors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);

            if (ctors.Length == 1)
            {
                return (ctors[0], false);
            }

            return (null, false);
        }
    }
}
