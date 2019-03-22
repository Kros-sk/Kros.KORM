using FluentAssertions;
using Kros.KORM.Injection;
using Kros.KORM.Metadata;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Kros.KORM.UnitTests.Metadata
{
    public class DatabaseMapperShould
    {
        [Fact]
        public void ReturnTableInfoForType()
        {
            var modelMapper = NSubstitute.Substitute.For<IModelMapper>();
            var tableInfo = new TableInfo(new List<ColumnInfo>(), new List<PropertyInfo>(), null);
            modelMapper.GetTableInfo<Foo>().Returns(tableInfo);
            modelMapper.GetTableInfo(Arg.Any<Type>()).Returns(tableInfo);

            var databaseModelMapper = new DatabaseMapper(modelMapper);
            var expected = tableInfo;

            var actual = databaseModelMapper.GetTableInfo<Foo>();

            actual.Should().Be(expected);
        }

        [Fact]
        public void ReturnInjectorForTypeWhichWasConfigured()
        {
            var modelMapper = NSubstitute.Substitute.For<IModelMapper>();
            var injector = new Injector();
            modelMapper.GetInjector<Foo>().Returns(injector);

            var databaseModelMapper = new DatabaseMapper(modelMapper);

            databaseModelMapper.GetInjector<Foo>().Should().Be(injector);
        }

        private class Injector : IInjector
        {
            public object GetValue(string propertyName)
            {
                throw new NotImplementedException();
            }

            public bool IsInjectable(string propertyName)
            {
                throw new NotImplementedException();
            }
        }

        private class Foo
        {
            public int Bar { get; set; }
        }
    }
}
