using FluentAssertions;
using Kros.KORM.Converter;
using Kros.KORM.Exceptions;
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

            columns.Count.Should().Be(6);
            columns[0].Name.Should().Be("PostCode", "Property \"Code\" has alias \"PostCode\".");
            columns[1].Name.Should().Be("FirstName", "Property \"PropertyString\" has alias \"FirstName\".");
            columns[2].Name.Should().Be("LastName");
            columns[3].Name.Should().Be("PropertyDouble");
            columns[4].Name.Should().Be("PropertyEnum");
            columns[5].Name.Should().Be("DataTypeProperty");
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

            address.Name.Should().Be("Address");
            salary.Name.Should().Be("Salary");
        }

        [Fact]
        public void UseConventionForGettingTableNameWhenAliasDoesNotExist()
        {
            var modelMapper = new ConventionModelMapper();

            var tableInfo = modelMapper.GetTableInfo<Foo>();

            tableInfo.Name.Should().Be("Foo");
        }

        [Fact]
        public void UseAliasForTableName()
        {
            var modelMapper = new ConventionModelMapper();
            var tableInfo = modelMapper.GetTableInfo<AliasedModel>();

            tableInfo.Name.Should().Be("Person", $"Model \"{nameof(AliasedModel)}\" has table alias \"Person\".");
        }

        [Fact]
        public void GetTableInfoWithPrimaryKeyByAttribute()
        {
            var modelMapper = new ConventionModelMapper();
            var tableInfo = modelMapper.GetTableInfo<SinglePrivateKey>();

            const string pkMessage = "Column \"RecordId\" is attributed as key and attribute has precedence over convention (\"Id\" column).";
            const int propertiesCount = 3;
            tableInfo.Columns.Should().HaveCount(propertiesCount,
                "\"{0}\" has {1} properties.", nameof(SinglePrivateKey), propertiesCount);
            tableInfo.Columns.Count(c => c.IsPrimaryKey).Should().Be(1, pkMessage);

            var key = tableInfo.PrimaryKey.ToList();
            key.Should().HaveCount(1, pkMessage);
            key[0].Name.Should().Be("RecordId");
            key[0].IsPrimaryKey.Should().BeTrue();
        }

        [Fact]
        public void GetTableInfoWithCompositePrimaryKey()
        {
            var modelMapper = new ConventionModelMapper();
            var tableInfo = modelMapper.GetTableInfo<CompositePrivateKey>();

            const string pkMessage = "3 columns are attributed as key.";
            const int propertiesCount = 4;
            tableInfo.Columns.Should().HaveCount(propertiesCount,
                "\"{0}\" has {1} properties.", nameof(CompositePrivateKey), propertiesCount);
            tableInfo.Columns.Count(c => c.IsPrimaryKey).Should().Be(3, pkMessage);

            var key = tableInfo.PrimaryKey.ToList();
            key.Should().HaveCount(3, pkMessage);
            key[0].Name.Should().Be("RecordId1", $"Property {nameof(CompositePrivateKey.RecordId1)} has order 1.");
            key[0].IsPrimaryKey.Should().BeTrue();

            key[1].Name.Should().Be("RecordId2", $"Property {nameof(CompositePrivateKey.RecordId2)} has order 2.");
            key[1].IsPrimaryKey.Should().BeTrue();

            key[2].Name.Should().Be("RecordId3", $"Property {nameof(CompositePrivateKey.RecordId3)} has order 3.");
            key[2].IsPrimaryKey.Should().BeTrue();
        }

        [Fact]
        public void ThrowIfCompositePrimaryKeyHasColumnsWithInvalidOrder()
        {
            var modelMapper = new ConventionModelMapper();
            Action tableInfoAction = () => modelMapper.GetTableInfo<CompositePrivateKeyWithInvalidOrder>();

            tableInfoAction.Should().Throw<CompositePrimaryKeyException>(
                "Composite primary key columns must have specified order and this order must be unique for every column.");
        }

        [Fact]
        public void ThrowIfCompositePrimaryKeyHasColumnsWithInvalidName()
        {
            var modelMapper = new ConventionModelMapper();
            Action tableInfoAction = () => modelMapper.GetTableInfo<CompositePrivateKeyWithInvalidName>();

            tableInfoAction.Should().Throw<CompositePrimaryKeyException>(
                "If composite primary key has specified name, this name must be the same for all the columns.");
        }

        [Fact]
        public void ThrowIfCompositePrimaryKeyHasColumnsWithInvalidAutoIncrementMethodType()
        {
            var modelMapper = new ConventionModelMapper();
            Action tableInfoAction = () => modelMapper.GetTableInfo<CompositePrivateKeyWithInvalidAutoIncrementMethodType>();

            tableInfoAction.Should().Throw<CompositePrimaryKeyException>(
                $"All columns of the composite primary key must have \"{nameof(KeyAttribute.AutoIncrementMethodType)}\" set to \"{nameof(AutoIncrementMethodType.None)}\".");
        }

        [Fact]
        public void GetTableInfoWithPrimaryKeyByConvention()
        {
            var modelMapper = new ConventionModelMapper();

            const string pkMessage = "No attributed key was found, co column \"Id\" is primary key by convention.";

            var tableInfo = modelMapper.GetTableInfo<ConventionalPrivateKey>();
            tableInfo.Columns.Should().HaveCount(2);
            tableInfo.Columns.Count(c => c.IsPrimaryKey).Should().Be(1, pkMessage);

            var key = tableInfo.PrimaryKey.ToList();
            key.Should().HaveCount(1, pkMessage);

            key[0].Name.Should().Be("Id", "Column \"Id\" is primary key by convention.");
            key[0].IsPrimaryKey.Should().BeTrue();
        }

        [Fact]
        public void GetTableInfoWithoutPrimarKey()
        {
            var modelMapper = new ConventionModelMapper();
            var tableInfo = modelMapper.GetTableInfo<NoPrivateKeyModel>();

            tableInfo.Columns.Should().HaveCount(2);
            tableInfo.Columns.Count(c => c.IsPrimaryKey).Should().Be(0);
            tableInfo.PrimaryKey.Should().HaveCount(0);
        }

        [Fact]
        public void GetTableInfoWithColumnConverter()
        {
            var modelMapper = new ConventionModelMapper();
            var tableInfo = modelMapper.GetTableInfo<Foo>();

            var columnWithConverter = tableInfo.Columns.Single(c => c.Name == "PropertyEnum");

            columnWithConverter.Converter.Should().BeOfType<TestConverter>();
        }

        [Fact]
        public void GetTableInfoWithoutReadOnlyProperty()
        {
            var modelMapper = new ConventionModelMapper();

            var tableInfo = modelMapper.GetTableInfo<Foo>();

            var columns = tableInfo.Columns.ToList();

            tableInfo.GetColumnInfo("ReadOnlyProperty").Should().BeNull();
        }

        [Fact]
        public void GetTableInfoWithoutNoMappAttribute()
        {
            var modelMapper = new ConventionModelMapper();

            var tableInfo = modelMapper.GetTableInfo<Foo>();

            tableInfo.GetColumnInfo("Ignore").Should().BeNull();
        }

        [Fact]
        public void GetTableInfoWithAutoIncrementKey()
        {
            var modelMapper = new ConventionModelMapper();

            var tableInfo = modelMapper.GetTableInfo<FooWithAutoIncrement>();

            tableInfo.PrimaryKey.Single().AutoIncrementMethodType.Should().Be(AutoIncrementMethodType.Custom);
        }

        [Fact]
        public void GetTableInfoWithoutAutoIncrementKey()
        {
            var modelMapper = new ConventionModelMapper();

            var tableInfo = modelMapper.GetTableInfo<Foo>();

            tableInfo.PrimaryKey
                .Any(p => p.AutoIncrementMethodType != AutoIncrementMethodType.None)
                .Should().BeFalse();
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

            tableInfo.Name.Should().Be("customconventionmodel");

            var columns = tableInfo.Columns.ToList();
            columns.Count.Should().Be(2);
            columns[1].Name.Should().Be("PROPERTYDOUBLE");

            tableInfo.PrimaryKey.Should().HaveCount(1);
            tableInfo.PrimaryKey.FirstOrDefault().Name.Should().Be("OID");
        }

        [Fact]
        public void HaveOnAfterMaterializeMethodInfo()
        {
            var modelMapper = new ConventionModelMapper();

            var tableInfo = modelMapper.GetTableInfo<AliasedModel>();
            tableInfo.OnAfterMaterialize.Name.Should().Be("OnAfterMaterialize");
        }

        [Fact]
        public void KnowConfigureInjection()
        {
            var modelMapper = new ConventionModelMapper();

            var configurator = modelMapper.InjectionConfigurator<Foo>()
                .FillProperty(p => p.PropertyString, () => "lorem")
                .FillProperty(p => p.PropertyDouble, () => 1);

            modelMapper.GetInjector<Foo>().Should().Be(configurator);
        }

        [Fact]
        public void DontThrowExceptionIfInjectionIsNotConfigured()
        {
            var modelMapper = new ConventionModelMapper();

            modelMapper.GetInjector<Foo>().Should().NotBeNull();
        }

        #endregion

        public void Dispose()
        {
            ConverterAttribute.ClearCache();
        }
    }
}
