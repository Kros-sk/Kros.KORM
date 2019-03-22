using Kros.Caching;
using Kros.KORM.Converter;
using Kros.KORM.Properties;
using Kros.Utils;
using System;

namespace Kros.KORM.Metadata.Attribute
{
    /// <summary>
    /// Attribute for getting data converter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ConverterAttribute : System.Attribute
    {
        private static ICache<Type, IConverter> _converters = new Cache<Type, IConverter>();
        private readonly Type _converterType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConverterAttribute"/> class.
        /// </summary>
        /// <param name="converterType">Type of the converter.</param>
        /// <exception cref="ArgumentNullException">The value of <paramref name="converterType"/> is <see langword="null"/>.
        /// </exception>
        public ConverterAttribute(Type converterType)
        {
            Check.NotNull(converterType, nameof(converterType));
            if (!typeof(IConverter).IsAssignableFrom(converterType))
            {
                throw new ArgumentException(Resources.ConverterTypeIsNotIConverter, nameof(converterType));
            }

            _converterType = converterType;
        }

        /// <summary>
        /// Gets the converter for property.
        /// </summary>
        public IConverter Converter
        {
            get
            {
                return _converters.Get(_converterType, () => Activator.CreateInstance(_converterType) as IConverter);
            }
        }

        /// <summary>
        /// Clears the cache.
        /// </summary>
        internal static void ClearCache()
        {
            _converters.Clear();
        }
    }
}
