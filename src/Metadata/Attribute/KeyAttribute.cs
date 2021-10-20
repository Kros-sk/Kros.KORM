using System;

namespace Kros.KORM.Metadata.Attribute
{
    /// <summary>
    /// Attribute, which describe property, which are part of primary key.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class KeyAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyAttribute"/> class.
        /// </summary>
        public KeyAttribute()
            : this(null, 0, AutoIncrementMethodType.None, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyAttribute"/> class.
        /// </summary>
        /// <param name="order">The order of the column in composite primary key.</param>
        public KeyAttribute(int order)
            : this(null, order, AutoIncrementMethodType.None, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyAttribute"/> class.
        /// </summary>
        /// <param name="name">The key name.</param>
        public KeyAttribute(string name)
            : this(name, 0, AutoIncrementMethodType.None, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyAttribute"/> class.
        /// </summary>
        /// <param name="name">The key name.</param>
        /// <param name="order">The order of the column in composite primary key.</param>
        public KeyAttribute(string name, int order)
            : this(name, order, AutoIncrementMethodType.None, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyAttribute"/> class.
        /// </summary>
        /// <param name="autoIncrementMethodType">Type of primary key auto increment method.</param>
        public KeyAttribute(AutoIncrementMethodType autoIncrementMethodType)
            : this(null, 0, autoIncrementMethodType, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyAttribute"/> class.
        /// </summary>
        /// <param name="autoIncrementMethodType">Type of primary key auto increment method.</param>
        /// <param name="generatorName">Name of the value generator. If not set, table name will be used.</param>
        public KeyAttribute(AutoIncrementMethodType autoIncrementMethodType, string generatorName)
            : this(null, 0, autoIncrementMethodType, generatorName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyAttribute"/> class.
        /// </summary>
        /// <param name="name">The key name.</param>
        /// <param name="autoIncrementMethodType">Type of primary key auto increment method.</param>
        public KeyAttribute(string name, AutoIncrementMethodType autoIncrementMethodType)
            : this(name, 0, autoIncrementMethodType, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyAttribute"/> class.
        /// </summary>
        /// <param name="name">The key name.</param>
        /// <param name="autoIncrementMethodType">Type of primary key auto increment method.</param>
        /// <param name="generatorName">Name of the value generator. If not set, table name will be used.</param>
        public KeyAttribute(string name, AutoIncrementMethodType autoIncrementMethodType, string generatorName)
            : this(name, 0, autoIncrementMethodType, generatorName)
        {
        }

        private KeyAttribute(string name, int order, AutoIncrementMethodType autoIncrementMethodType, string generatorName)
        {
            AutoIncrementMethodType = autoIncrementMethodType;
            GeneratorName = generatorName;
            Name = name;
            Order = order;
        }

        /// <summary>
        /// Type of primary key auto increment method.
        /// </summary>
        public AutoIncrementMethodType AutoIncrementMethodType { get; }

        /// <summary>
        /// Name of the generator when <see cref="AutoIncrementMethodType"/> is <c>Custom</c>.
        /// If not set, table name will be used.
        /// </summary>
        public string GeneratorName { get; }

        /// <summary>
        /// The order of the column in composite primary key.
        /// </summary>
        public int Order { get; }

        /// <summary>
        /// Gets the name of key.
        /// </summary>
        public string Name { get; }
    }
}
