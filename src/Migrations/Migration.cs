using Kros.KORM.Metadata.Attribute;
using System;

namespace Kros.KORM.Migrations
{
    /// <summary>
    /// Model, which represents migration information.
    /// </summary>
    [Alias(Migration.TableName)]
    internal class Migration
    {
        internal const string TableName = "__KormMigrationsHistory";

        /// <summary>
        /// None migration.
        /// </summary>
        public static Migration None { get; } = new Migration() { MigrationId = 0 };

        /// <summary>
        /// Migration Id.
        /// </summary>
        [Key]
        public long MigrationId { get; set; }

        /// <summary>
        /// Name of migration.
        /// </summary>
        public string MigrationName { get; set; }

        /// <summary>
        /// Information about project, which executed this migration.
        /// </summary>
        public string ProductInfo { get; set; }

        /// <summary>
        /// Information, when the migration was executed.
        /// </summary>
        /// <remarks>
        /// If value is <see langword="null" />, migration has not been completed.
        /// </remarks>
        public DateTime? Updated { get; set; }
    }
}
