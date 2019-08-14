using Kros.KORM.Converter;
using Kros.KORM.Injection;
using Kros.KORM.ValueGeneration;
using System;
using System.Collections.Generic;

namespace Kros.KORM.Metadata
{
    public partial class ConventionModelMapper
    {
        private class DummyInjector : IInjector
        {
            public static IInjector Default { get; } = new DummyInjector();
            private DummyInjector() { }
            public object GetValue(string propertyName) => throw new NotImplementedException();
            public bool IsInjectable(string propertyName) => false;
        }

        private class EntityMapper
        {
            public EntityMapper(Type entityType)
            {
                EntityType = entityType;
            }

            public Type EntityType { get; }
            public string TableName { get; set; } = null;
            public string PrimaryKeyPropertyName { get; set; } = null;
            public AutoIncrementMethodType PrimaryKeyAutoIncrementType { get; set; } = AutoIncrementMethodType.None;
            public Dictionary<string, string> ColumnMap { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, IConverter> Converters { get; } = new Dictionary<string, IConverter>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, IValueGenerator> ValueGenerators { get; } =
                new Dictionary<string, IValueGenerator>(StringComparer.OrdinalIgnoreCase);
            public ValueGenerated ValueGenerated { get; set; }
            public HashSet<string> NoMap { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public IInjector Injector { get; set; } = null;
            public Dictionary<Type, IConverter> PropertyConverters { get; } = new Dictionary<Type, IConverter>();
        }
    }
}
