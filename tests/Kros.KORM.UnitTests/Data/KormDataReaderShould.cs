using FluentAssertions;
using Kros.KORM.CommandGenerator;
using Kros.KORM.Data;
using Kros.KORM.Metadata;
using NSubstitute;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Kros.KORM.UnitTests.Data
{
    public class KormDataReaderShould
    {
        [Fact]
        public void HaveCorrectFieldCount()
        {
            var data = new HashSet<Foo>() { new Foo() { Prop1 = 1, Prop2 = "1", Prop3 = 1m }, new Foo() { Prop1 = 2, Prop2 = "2", Prop3 = 2m } };
            var commandGenerator = Substitute.For<ICommandGenerator<Foo>>();
            commandGenerator.GetQueryColumns().Returns(new List<ColumnInfo>() { new ColumnInfo() });

            using (var reader = new KormDataReader<Foo>(data, commandGenerator))
            {
                reader.FieldCount.Should().Be(1);
            }
        }

        [Fact]
        public void GetColumnNameByIndex()
        {
            var data = new HashSet<Foo>() { new Foo() { Prop1 = 1, Prop2 = "1", Prop3 = 1m }, new Foo() { Prop1 = 2, Prop2 = "2", Prop3 = 2m } };
            var commandGenerator = Substitute.For<ICommandGenerator<Foo>>();
            commandGenerator.GetQueryColumns().Returns(new List<ColumnInfo>() { new ColumnInfo() { Name = "Prop1" }, new ColumnInfo() { Name = "Prop2" } });

            using (var reader = new KormDataReader<Foo>(data, commandGenerator))
            {
                reader.GetName(1).Should().Be("Prop2");
            }
        }

        [Fact]
        public void GetColumnIndexByName()
        {
            var data = new HashSet<Foo>() { new Foo() { Prop1 = 1, Prop2 = "1", Prop3 = 1m }, new Foo() { Prop1 = 2, Prop2 = "2", Prop3 = 2m } };
            var commandGenerator = Substitute.For<ICommandGenerator<Foo>>();
            commandGenerator.GetQueryColumns().Returns(new List<ColumnInfo>() { new ColumnInfo() { Name = "Prop1" }, new ColumnInfo() { Name = "Prop2" } });

            using (var reader = new KormDataReader<Foo>(data, commandGenerator))
            {
                reader.GetOrdinal("Prop2").Should().Be(1);
            }
        }

        [Fact]
        public void Read2Value()
        {
            var data = new HashSet<Foo>() { new Foo() { Prop1 = 1, Prop2 = "1", Prop3 = 1m }, new Foo() { Prop1 = 2, Prop2 = "2", Prop3 = 2m } };
            var commandGenerator = Substitute.For<ICommandGenerator<Foo>>();
            commandGenerator.GetQueryColumns().Returns(new List<ColumnInfo>() {
                CreateColumnInfo(nameof(Foo.Prop1)),
                CreateColumnInfo(nameof(Foo.Prop2)) });

            using (var reader = new KormDataReader<Foo>(data, commandGenerator))
            {
                var count = 0;
                while (reader.Read())
                {
                    count++;
                }

                count.Should().Be(2);
            }
        }

        private ColumnInfo CreateColumnInfo(string name) => new ColumnInfo()
        {
            Name = name,
            PropertyInfo = GetPropertyInfo(name)
        };

        private PropertyInfo GetPropertyInfo(string propertyName)
            => typeof(Foo).GetProperty(propertyName);

        public class Foo
        {
            public int Prop1 { get; set; }

            public string Prop2 { get; set; }

            public decimal Prop3 { get; set; }
        }
    }
}
