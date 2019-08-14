using FluentAssertions;
using Kros.KORM.Materializer;
using Kros.KORM.Metadata;
using Kros.KORM.Query;
using Kros.KORM.UnitTests.Integration;
using Kros.KORM.ValueGeneration;
using Kros.UnitTests;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Xunit;

namespace Kros.KORM.UnitTests.Metadata
{
    public class PropertyBuilderExtensionsShould
    {
        [Fact]
        public void UseCurrentTimeGeneratorOnInsert()
        {
            var modelBuilder = new ModelConfigurationBuilder();
            var modelMapper = new ConventionModelMapper();

            modelBuilder.Entity<BuilderTestEntity>()
                .Property(p => p.GeneratedValue).UseCurrentTimeValueGenerator(ValueGenerated.OnInsert);

            modelBuilder.Build(modelMapper);

            TableInfo tableInfo = modelMapper.GetTableInfo<BuilderTestEntity>();

            tableInfo.GetColumnInfoByPropertyName("GeneratedValue").ValueGenerated.Should().Be(ValueGenerated.OnInsert);
            tableInfo.GetColumnInfoByPropertyName("GeneratedValue").ValueGenerator.Should().BeOfType<CurrentTimeValueGenerator>();
        }

        [Fact]
        public void ThrowExceptionWhenUseCurrentTimeGeneratorOnNever()
        {
            var modelBuilder = new ModelConfigurationBuilder();
            var modelMapper = new ConventionModelMapper();

            Action act = () =>
                modelBuilder.Entity<BuilderTestEntity>()
                    .Property(p => p.GeneratedValue)
                    .UseCurrentTimeValueGenerator(ValueGenerated.Never);

            act.Should().Throw<NotSupportedException>();
        }

        private class BuilderTestEntity
        {
            public int GeneratedValue { get; set; }
        }
    }
}
