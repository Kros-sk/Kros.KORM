namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Provides a simple fluent API for building mapping definition between entity and database table.
    /// </summary>
    public abstract class EntityTypeBuilderBase
    {
        /// <summary>
        /// Builds entity configuration.
        /// </summary>
        /// <param name="modelMapper">Model mappper.</param>
        internal abstract void Build(IModelMapperInternal modelMapper);
    }
}
