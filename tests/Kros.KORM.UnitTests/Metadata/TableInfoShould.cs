using FluentAssertions;
using Kros.KORM.Metadata;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Kros.KORM.UnitTests.Metadata
{
    public class TableInfoShould
    {
        [Fact]
        public void ReturnColumns()
        {
            var columns = new List<ColumnInfo>();

            var name = new ColumnInfo() { Name = "Name", IsPrimaryKey = true };
            var type = new ColumnInfo() { Name = "Type", IsPrimaryKey = false };
            var surname = new ColumnInfo() { Name = "Surname", IsPrimaryKey = false };

            columns.Add(name);
            columns.Add(type);
            columns.Add(surname);

            var tableInfo = new TableInfo(columns, new List<PropertyInfo>(), null);

            var actual = tableInfo.Columns;
            var expected = columns;

            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void ReturnsColumnsOfPrimaryKeyColumns()
        {
            var columns = new List<ColumnInfo>();

            var id = new ColumnInfo() { Name = "ID", IsPrimaryKey = true };
            var name = new ColumnInfo() { Name = "NAME", IsPrimaryKey = false };
            var type = new ColumnInfo() { Name = "TYPE", IsPrimaryKey = true };
            var surname = new ColumnInfo() { Name = "SURNAME", IsPrimaryKey = false };
            var modelYear = new ColumnInfo() { Name = "MODELYEAR", IsPrimaryKey = true };

            columns.Add(id);
            columns.Add(name);
            columns.Add(type);
            columns.Add(surname);
            columns.Add(modelYear);

            var tableInfo = new TableInfo(columns, new List<PropertyInfo>(), null);

            var actual = tableInfo.PrimaryKey;
            var expected = columns.Where(x => x.IsPrimaryKey).ToList();

            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void ReturnOnAfterMaterializeIfGetByConstructor()
        {
            var columns = new List<ColumnInfo>();

            var id = new ColumnInfo() { Name = "ID", IsPrimaryKey = true };
            columns.Add(id);

            var methInfo = typeof(ColumnInfo).GetMethod("SetValue");

            var tableInfo = new TableInfo(columns, new List<PropertyInfo>() ,methInfo);

            tableInfo.OnAfterMaterialize.Should().BeSameAs(methInfo);
        }

        [Fact]
        public void ReturnColumnInformation()
        {
            var columns = new List<ColumnInfo>();

            var id = new ColumnInfo() { Name = "ID", IsPrimaryKey = true };
            var name = new ColumnInfo() { Name = "NAME", IsPrimaryKey = true };
            var surname = new ColumnInfo() { Name = "SURNAME", IsPrimaryKey = false };

            columns.Add(id);
            columns.Add(name);
            columns.Add(surname);

            var tableInfo = new TableInfo(columns, new List<PropertyInfo>(),null);

            var actual = tableInfo.GetColumnInfo("ID");
            var expected = id;

            actual.Should().Be(expected);
        }

        [Fact]
        public void ReturnColumnInformationByIgnoreCase()
        {
            var columns = new List<ColumnInfo>();

            var id = new ColumnInfo() { Name = "ID", IsPrimaryKey = true };
            var name = new ColumnInfo() { Name = "NAME", IsPrimaryKey = true };
            var surname = new ColumnInfo() { Name = "SURNAME", IsPrimaryKey = false };

            columns.Add(id);
            columns.Add(name);
            columns.Add(surname);

            var tableInfo = new TableInfo(columns, new List<PropertyInfo>(), null);

            var actual = tableInfo.GetColumnInfo("id");
            var expected = id;

            actual.Should().Be(expected);
        }

        [Fact]
        public void ReturnNullWhenItemDoesNotExists()
        {
            var columns = new List<ColumnInfo>();

            var id = new ColumnInfo() { Name = "ID", IsPrimaryKey = true };
            var name = new ColumnInfo() { Name = "NAME", IsPrimaryKey = true };
            var surname = new ColumnInfo() { Name = "SURNAME", IsPrimaryKey = false };

            columns.Add(id);
            columns.Add(name);
            columns.Add(surname);

            var tableInfo = new TableInfo(columns,new List<PropertyInfo>(), null);

            var actual = tableInfo.GetColumnInfo("car");
            ColumnInfo expected = null;

            actual.Should().Be(expected);
        }

        [Fact]
        public void ReturnColumnInformationByPropertyInfo()
        {
            var columns = new List<ColumnInfo>();

            var id = new ColumnInfo()
            {
                Name = "ID",
                IsPrimaryKey = true,
                PropertyInfo = typeof(Person).GetProperty("Id")
            };
            var name = new ColumnInfo()
            {
                Name = "NAME",
                IsPrimaryKey = true,
                PropertyInfo = typeof(Person).GetProperty("Name")
            };
            var surname = new ColumnInfo()
            {
                Name = "SURNAME",
                IsPrimaryKey = false,
                PropertyInfo = typeof(Person).GetProperty("Surname")
            };

            columns.Add(id);
            columns.Add(name);
            columns.Add(surname);

            var tableInfo = new TableInfo(columns, new List<PropertyInfo>(), null);

            var actual = tableInfo.GetColumnInfo(typeof(Person).GetProperty("Id"));
            var expected = id;

            actual.Should().Be(expected);
        }

        public class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Surname { get; set; }
        }
    }
}
