﻿using Kros.KORM.Metadata;

namespace Kros.KORM
{
    /// <summary>
    /// Base class, can be derived to configure database.
    /// </summary>
    public abstract class DatabaseConfigurationBase
    {
        /// <summary>
        /// You can override this method for configuration model builder.
        /// </summary>
        /// <param name="modelBuilder">Model builder.</param>
        public virtual void OnModelCreating(ModelConfigurationBuilder modelBuilder)
        {
        }
    }
}
