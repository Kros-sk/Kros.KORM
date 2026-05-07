using Kros.KORM.Metadata;
using System;
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

            Assert.Equal(ValueGenerated.OnInsert, tableInfo.GetColumnInfoByPropertyName("GeneratedValue").ValueGenerated);
            Assert.IsType<CurrentTimeValueGenerator>(tableInfo.GetColumnInfoByPropertyName("GeneratedValue").ValueGenerator);
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

            Assert.Throws<NotSupportedException>(act);
        }

        private class BuilderTestEntity
        {
            public int GeneratedValue { get; set; }
        }
    }
}
