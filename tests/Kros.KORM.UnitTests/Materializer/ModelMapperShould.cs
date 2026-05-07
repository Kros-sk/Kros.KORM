using Kros.KORM.Converter;
using Kros.KORM.Exceptions;
using Kros.KORM.Injection;
using Kros.KORM.Materializer;
using Kros.KORM.Metadata;
using Kros.KORM.Metadata.Attribute;
using System;
using System.Data;
using System.Linq;
using Xunit;

namespace Kros.KORM.UnitTests.Metadata
{
    public class ModelMapperShould : IDisposable
    {
        #region Nested Types

        private class Foo
        {
            [Alias("PostCode")]
            public string Code { get; set; }

            [Alias("FirstName")]
            public string PropertyString { get; set; }

            public string LastName { get; set; }

            public double PropertyDouble { get; set; }

            [Converter(typeof(TestConverter))]
            public TestEnum PropertyEnum { get; set; }

            public int ReadOnlyProperty { get { return 5; } }

            public string DataTypeProperty { get; set; }

            [NoMap()]
            public int Ignore { get; set; }
        }

        private class NoPrivateKeyModel
        {
            public int RecordId { get; set; }
            public string Data { get; set; }
        }

        private class SinglePrivateKey
        {
            [Key]
            public int RecordId { get; set; }
            public int Id { get; set; }
            public string Data { get; set; }
        }

        private class FluentPrivateKey
        {
            public int RecordId { get; set; }
            public int Id { get; set; }
            public string Data { get; set; }
        }

        private class CompositePrivateKey
        {
            [Key(2)]
            public int RecordId2 { get; set; }

            [Key(3)]
            public int RecordId3 { get; set; }

            [Key(1)]
            public int RecordId1 { get; set; }

            public string Data { get; set; }
        }

        private class CompositePrivateKeyWithInvalidOrder
        {
            [Key(1)]
            public int RecordId1 { get; set; }

            [Key(1)]
            public int RecordId2 { get; set; }

            [Key(3)]
            public int RecordId3 { get; set; }

            public string Data { get; set; }
        }

        private class CompositePrivateKeyWithInvalidName
        {
            [Key("PK", 1)]
            public int RecordId1 { get; set; }

            [Key("PK_Test", 2)]
            public int RecordId2 { get; set; }

            [Key("PK_Test", 3)]
            public int RecordId3 { get; set; }

            public string Data { get; set; }
        }

        private class CompositePrivateKeyWithInvalidAutoIncrementMethodType
        {
            [Key(AutoIncrementMethodType.Custom)]
            public int RecordId1 { get; set; }

            [Key(2)]
            public int RecordId2 { get; set; }

            [Key(3)]
            public int RecordId3 { get; set; }

            public string Data { get; set; }
        }

        private class ConventionalPrivateKey
        {
            public string Data { get; set; }
            public int Id { get; set; }
        }

        [Alias("Person")]
        private class AliasedModel : IMaterialize
        {
            public int Id { get; set; }

            public void OnAfterMaterialize(IDataRecord source)
            {
                throw new NotImplementedException();
            }
        }

        private class CustomConventionModel
        {
            public int OId { get; set; }
            public double PropertyDouble { get; set; }
        }

        private class FooWithAutoIncrement
        {
            [Key(AutoIncrementMethodType.Custom)]
            public int Id { get; set; }

            public double PropertyDouble { get; set; }
        }

        private enum TestEnum
        {
            Value1,
            Value2,
            Value3
        }

        private class TestConverter : IConverter
        {
            public object Convert(object value)
            {
                throw new NotImplementedException();
            }

            public object ConvertBack(object value)
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region Tests

        [Fact]
        public void ReturnColumnsWithCorrectNames()
        {
            var modelMapper = new ConventionModelMapper();
            var tableInfo = modelMapper.GetTableInfo<Foo>();

            var columns = tableInfo.Columns.ToList();

            Assert.Equal(6, columns.Count);
            Assert.Equal("PostCode", columns[0].Name);
            Assert.Equal("FirstName", columns[1].Name);
            Assert.Equal("LastName", columns[2].Name);
            Assert.Equal("PropertyDouble", columns[3].Name);
            Assert.Equal("PropertyEnum", columns[4].Name);
            Assert.Equal("DataTypeProperty", columns[5].Name);
        }

        [Fact]
        public void UseNamesFromConfigurationMap()
        {
            var modelMapper = new ConventionModelMapper();
            modelMapper.SetColumnName<Foo, string>(p => p.PropertyString, "Address");
            modelMapper.SetColumnName<Foo, double>(p => p.PropertyDouble, "Salary");

            var tableInfo = modelMapper.GetTableInfo<Foo>();

            var address = tableInfo.GetColumnInfoByPropertyName(nameof(Foo.PropertyString));
            var salary = tableInfo.GetColumnInfoByPropertyName(nameof(Foo.PropertyDouble));

            Assert.Equal("Address", address.Name);
            Assert.Equal("Salary", salary.Name);
        }

        [Fact]
        public void UseNamesFromConfigurationMapWhenPropertyNameIsUsed()
        {
            var modelMapper = new ConventionModelMapper();
            ((IModelMapperInternal)modelMapper).SetColumnName<Foo>("PropertyString", "Address");
            ((IModelMapperInternal)modelMapper).SetColumnName<Foo>("PropertyDouble", "Salary");

            var tableInfo = modelMapper.GetTableInfo<Foo>();

            var address = tableInfo.GetColumnInfoByPropertyName(nameof(Foo.PropertyString));
            var salary = tableInfo.GetColumnInfoByPropertyName(nameof(Foo.PropertyDouble));

            Assert.Equal("Address", address.Name);
            Assert.Equal("Salary", salary.Name);
        }

        [Fact]
        public void UseConventionForGettingTableNameWhenAliasDoesNotExist()
        {
            var modelMapper = new ConventionModelMapper();

            var tableInfo = modelMapper.GetTableInfo<Foo>();

            Assert.Equal("Foo", tableInfo.Name);
        }

        [Fact]
        public void UseSetsTableName()
        {
            var modelMapper = new ConventionModelMapper();
            (modelMapper as IModelMapperInternal).SetTableName<Foo>("Foo2");

            var tableInfo = modelMapper.GetTableInfo<Foo>();

            Assert.Equal("Foo2", tableInfo.Name);
        }

        [Fact]
        public void UseAliasForTableName()
        {
            var modelMapper = new ConventionModelMapper();
            var tableInfo = modelMapper.GetTableInfo<AliasedModel>();

            Assert.Equal("Person", tableInfo.Name);
        }

        [Fact]
        public void GetTableInfoWithPrimaryKeyByAttribute()
        {
            var modelMapper = new ConventionModelMapper();
            var tableInfo = modelMapper.GetTableInfo<SinglePrivateKey>();

            const int propertiesCount = 3;
            Assert.Equal(propertiesCount, tableInfo.Columns.Count());
            Assert.Equal(1, tableInfo.Columns.Count(c => c.IsPrimaryKey));

            var key = tableInfo.PrimaryKey.ToList();
            Assert.Single(key);
            Assert.Equal("RecordId", key[0].Name);
            Assert.True(key[0].IsPrimaryKey);
        }

        [Fact]
        public void GetTableInfoWithPrimaryKeyFluentDefinition()
        {
            var modelMapper = new ConventionModelMapper();
            ((IModelMapperInternal)modelMapper)
                .SetPrimaryKey<FluentPrivateKey>("RecordId", AutoIncrementMethodType.Identity, null);

            var tableInfo = modelMapper.GetTableInfo<FluentPrivateKey>();
            var pkList = tableInfo.PrimaryKey.ToList();
            Assert.Single(pkList);
            Assert.Single(pkList, (c) => c.Name == "RecordId" && c.AutoIncrementMethodType == AutoIncrementMethodType.Identity);
        }

        [Fact]
        public void GetTableInfoWithPrimaryKeyFluentDefinitionWithGeneratorName()
        {
            var modelMapper = new ConventionModelMapper();
            ((IModelMapperInternal)modelMapper)
                .SetPrimaryKey<FluentPrivateKey>("RecordId", AutoIncrementMethodType.Identity, "LoremIpsum");

            var tableInfo = modelMapper.GetTableInfo<FluentPrivateKey>();
            var pkList = tableInfo.PrimaryKey.ToList();
            Assert.Single(pkList);
            Assert.Single(pkList, (c) => c.Name == "RecordId"
                    && c.AutoIncrementMethodType == AutoIncrementMethodType.Identity
                    && c.AutoIncrementGeneratorName == "LoremIpsum");
        }

        [Fact]
        public void GetTableInfoWithCompositePrimaryKey()
        {
            var modelMapper = new ConventionModelMapper();
            var tableInfo = modelMapper.GetTableInfo<CompositePrivateKey>();

            const int propertiesCount = 4;
            Assert.Equal(propertiesCount, tableInfo.Columns.Count());
            Assert.Equal(3, tableInfo.Columns.Count(c => c.IsPrimaryKey));

            var key = tableInfo.PrimaryKey.ToList();
            Assert.Equal(3, key.Count);
            Assert.Equal("RecordId1", key[0].Name);
            Assert.True(key[0].IsPrimaryKey);

            Assert.Equal("RecordId2", key[1].Name);
            Assert.True(key[1].IsPrimaryKey);

            Assert.Equal("RecordId3", key[2].Name);
            Assert.True(key[2].IsPrimaryKey);
        }

        [Fact]
        public void ThrowIfCompositePrimaryKeyHasColumnsWithInvalidOrder()
        {
            var modelMapper = new ConventionModelMapper();
            Action tableInfoAction = () => modelMapper.GetTableInfo<CompositePrivateKeyWithInvalidOrder>();

            Assert.Throws<CompositePrimaryKeyException>(tableInfoAction);
        }

        [Fact]
        public void ThrowIfCompositePrimaryKeyHasColumnsWithInvalidName()
        {
            var modelMapper = new ConventionModelMapper();
            Action tableInfoAction = () => modelMapper.GetTableInfo<CompositePrivateKeyWithInvalidName>();

            Assert.Throws<CompositePrimaryKeyException>(tableInfoAction);
        }

        [Fact]
        public void ThrowIfCompositePrimaryKeyHasColumnsWithInvalidAutoIncrementMethodType()
        {
            var modelMapper = new ConventionModelMapper();
            Action tableInfoAction = () => modelMapper.GetTableInfo<CompositePrivateKeyWithInvalidAutoIncrementMethodType>();

            Assert.Throws<CompositePrimaryKeyException>(tableInfoAction);
        }

        [Fact]
        public void GetTableInfoWithPrimaryKeyByConvention()
        {
            var modelMapper = new ConventionModelMapper();

            var tableInfo = modelMapper.GetTableInfo<ConventionalPrivateKey>();
            Assert.Equal(2, tableInfo.Columns.Count());
            Assert.Equal(1, tableInfo.Columns.Count(c => c.IsPrimaryKey));

            var key = tableInfo.PrimaryKey.ToList();
            Assert.Single(key);

            Assert.Equal("Id", key[0].Name);
            Assert.True(key[0].IsPrimaryKey);
        }

        [Fact]
        public void GetTableInfoWithoutPrimarKey()
        {
            var modelMapper = new ConventionModelMapper();
            var tableInfo = modelMapper.GetTableInfo<NoPrivateKeyModel>();

            Assert.Equal(2, tableInfo.Columns.Count());
            Assert.Equal(0, tableInfo.Columns.Count(c => c.IsPrimaryKey));
            Assert.Empty(tableInfo.PrimaryKey);
        }

        [Fact]
        public void GetTableInfoWithColumnConverter()
        {
            var modelMapper = new ConventionModelMapper();
            var tableInfo = modelMapper.GetTableInfo<Foo>();

            var columnWithConverter = tableInfo.Columns.Single(c => c.Name == "PropertyEnum");

            Assert.IsType<TestConverter>(columnWithConverter.Converter);
        }

        [Fact]
        public void GetTableInfoWithColumnConverterSetByConfiguration()
        {
            var modelMapper = new ConventionModelMapper();
            ((IModelMapperInternal)modelMapper).SetConverter<Foo>("LastName", new TestConverter());
            var tableInfo = modelMapper.GetTableInfo<Foo>();

            var columnWithConverter = tableInfo.Columns.Single(c => c.Name == "LastName");

            Assert.IsType<TestConverter>(columnWithConverter.Converter);
        }

        [Fact]
        public void GetTableInfoWithoutReadOnlyProperty()
        {
            var modelMapper = new ConventionModelMapper();

            var tableInfo = modelMapper.GetTableInfo<Foo>();

            Assert.Null(tableInfo.GetColumnInfo("ReadOnlyProperty"));
        }

        [Fact]
        public void GetTableInfoWithoutNoMapAttribute()
        {
            var modelMapper = new ConventionModelMapper();

            var tableInfo = modelMapper.GetTableInfo<Foo>();

            Assert.Null(tableInfo.GetColumnInfo("Ignore"));
        }

        [Fact]
        public void GetTableInfoWithoutNoMap()
        {
            var modelMapper = new ConventionModelMapper();
            ((IModelMapperInternal)modelMapper).SetNoMap<Foo>("LastName");

            var tableInfo = modelMapper.GetTableInfo<Foo>();

            Assert.Null(tableInfo.GetColumnInfo("LastName"));
        }

        [Fact]
        public void GetTableInfoWithAutoIncrementKey()
        {
            var modelMapper = new ConventionModelMapper();

            var tableInfo = modelMapper.GetTableInfo<FooWithAutoIncrement>();

            Assert.Equal(AutoIncrementMethodType.Custom, tableInfo.PrimaryKey.Single().AutoIncrementMethodType);
        }

        [Fact]
        public void GetTableInfoWithoutAutoIncrementKey()
        {
            var modelMapper = new ConventionModelMapper();

            var tableInfo = modelMapper.GetTableInfo<Foo>();

#pragma warning disable xUnit2012 // Do not use boolean check to check if a value exists in a collection
            Assert.False(tableInfo.PrimaryKey
                .Any(p => p.AutoIncrementMethodType != AutoIncrementMethodType.None));
#pragma warning restore xUnit2012 // Do not use boolean check to check if a value exists in a collection
        }

        [Fact]
        public void UseCustomConvention()
        {
            var modelMapper = new ConventionModelMapper
            {
                MapColumnName = (colInfo, modelType) =>
                {
                    return colInfo.PropertyInfo.Name.ToUpper();
                },

                MapTableName = (tInfo, type) =>
                {
                    return type.Name.ToLower();
                },

                MapPrimaryKey = (tInfo) =>
                {
                    var primaryKey = tInfo.Columns.Where(p => p.Name == "OID");

                    foreach (var key in primaryKey)
                    {
                        key.IsPrimaryKey = true;
                    }

                    return primaryKey;
                }
            };

            var tableInfo = modelMapper.GetTableInfo<CustomConventionModel>();

            Assert.Equal("customconventionmodel", tableInfo.Name);

            var columns = tableInfo.Columns.ToList();
            Assert.Equal(2, columns.Count);
            Assert.Equal("PROPERTYDOUBLE", columns[1].Name);

            Assert.Single(tableInfo.PrimaryKey);
            Assert.Equal("OID", tableInfo.PrimaryKey.FirstOrDefault().Name);
        }

        [Fact]
        public void HaveOnAfterMaterializeMethodInfo()
        {
            var modelMapper = new ConventionModelMapper();

            TableInfo tableInfo = modelMapper.GetTableInfo<AliasedModel>();
            Assert.Equal("OnAfterMaterialize", tableInfo.OnAfterMaterialize.Name);
        }

        [Fact]
        public void KnowConfigureInjection()
        {
            var modelMapper = new ConventionModelMapper();

            var configurator = modelMapper.InjectionConfigurator<Foo>()
                .FillProperty(p => p.PropertyString, () => "lorem")
                .FillProperty(p => p.PropertyDouble, () => 1);

            Assert.Same(configurator, modelMapper.GetInjector<Foo>());
        }

        [Fact]
        public void KnowConfigureInjectionExternal()
        {
            var modelMapper = new ConventionModelMapper();
            var configurator = new InjectionConfiguration<Foo>()
                .FillProperty(p => p.PropertyDouble, () => 1);

            ((IModelMapperInternal)modelMapper).SetInjector<Foo>((IInjector)configurator);

            Assert.Same(configurator, modelMapper.GetInjector<Foo>());
        }

        [Fact]
        public void DontThrowExceptionIfInjectionIsNotConfigured()
        {
            var modelMapper = new ConventionModelMapper();

            Assert.NotNull(modelMapper.GetInjector<Foo>());
        }

        #endregion

        public void Dispose()
        {
            ConverterAttribute.ClearCache();
        }
    }
}
