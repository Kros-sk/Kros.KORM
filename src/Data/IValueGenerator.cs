﻿using System.Collections.Generic;

namespace Kros.KORM.Data
{
    /// <summary>
    /// Interface for column value generator.
    /// </summary>
    public interface IValueGenerator
    {
        /// <summary>
        /// Gets value.
        /// </summary>
        object GetValue();
    }

    public interface IValueGenerator<T> : IValueGenerator
    {
        T GetValue();
    }

    //public class CurrentTimeValueGenerator : IValueGenerator<DateTimeOffset>
    //{
    //    public DateTimeOffset GetValue() => DateTimeOffset.UtcNow;

    //    object IValueGenerator.GetValue() => GetValue();
    //}

    /// <summary>
    /// Indicates when a value for a property will be generated by the database.
    /// </summary>
    public enum ValueGenerated
    {
        /// <summary>
        /// A value is never generated.
        /// </summary>
        Never,

        /// <summary>
        /// A value is generated when an entity is added to the database.
        /// </summary>
        OnInsert,

        /// <summary>
        /// A value is generated when an entity is updated to the database.
        /// </summary>
        OnUpdate,

        /// <summary>
        /// A value is generated when an entity is added or update to the database.
        /// </summary>
        OnInsertOrUpdate = OnInsert | OnUpdate
    }
}