using Kros.KORM.Metadata;
using System;
using Xunit;

namespace Kros.KORM.UnitTests.Metadata
{
    public class ColumnInfoShould
    {
        [Fact]
        public void ReturnStringUsingGetValue()
        {
            var columnInfo = new ColumnInfo();
            var propName = "Name";
            var car = new Car()
            {
                Name = "Honda",
                Dors = 5,
                CreateDate = new DateTime(2010, 1, 1)
            };

            columnInfo.PropertyInfo = car.GetType().GetProperty(propName);
            var actual = columnInfo.GetValue(car);
            var expected = "Honda";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ReturnIntUsingGetValue()
        {
            var columnInfo = new ColumnInfo();
            var propName = "Dors";
            var car = new Car()
            {
                Name = "Honda",
                Dors = 5,
                CreateDate = new DateTime(2010, 1, 1)
            };

            columnInfo.PropertyInfo = car.GetType().GetProperty(propName);
            var actual = columnInfo.GetValue(car);
            var expected = 5;

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ReturnDateTimeUsingGetValue()
        {
            var columnInfo = new ColumnInfo();
            var propName = "CreateDate";
            var car = new Car()
            {
                Name = "Honda",
                Dors = 5,
                CreateDate = new DateTime(2010, 1, 1)
            };

            columnInfo.PropertyInfo = car.GetType().GetProperty(propName);
            var actual = columnInfo.GetValue(car);
            var expected = new DateTime(2010, 1, 1);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SetStringUsingSetValue()
        {
            var columnInfo = new ColumnInfo();
            var propName = "Name";
            var car = new Car()
            {
                Name = "Honda",
                Dors = 5,
                CreateDate = new DateTime (2010, 1, 1)
            };

            columnInfo.PropertyInfo = car.GetType().GetProperty(propName);
            columnInfo.SetValue(car, "Mazda");
            var actual = columnInfo.GetValue(car);
            var expected = "Mazda";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SetIntUsingSetValue()
        {
            var columnInfo = new ColumnInfo();
            var propName = "Dors";
            var car = new Car()
            {
                Name = "Honda",
                Dors = 5,
                CreateDate = new DateTime(2010, 1, 1)
            };

            columnInfo.PropertyInfo = car.GetType().GetProperty(propName);
            columnInfo.SetValue(car, 4);
            var actual = columnInfo.GetValue(car);
            var expected = 4;

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SetDateTimeUsingSetValue()
        {
            var columnInfo = new ColumnInfo();
            var propName = "CreateDate";
            var car = new Car()
            {
                Name = "Honda",
                Dors = 5,
                CreateDate = new DateTime(2010, 1, 1)
            };

            columnInfo.PropertyInfo = car.GetType().GetProperty(propName);
            columnInfo.SetValue(car, new DateTime(2015, 1, 1));
            var actual = columnInfo.GetValue(car);
            var expected = new DateTime(2015, 1, 1);

            Assert.Equal(expected, actual);
        }

        private class Car
        {
            public string Name { get; set; }
            public int Dors { get; set; }
            public DateTime CreateDate { get; set; }
        }
    }
}
