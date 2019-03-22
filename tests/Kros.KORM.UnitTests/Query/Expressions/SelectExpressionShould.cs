using FluentAssertions;
using Kros.KORM.Metadata;
using Kros.KORM.Metadata.Attribute;
using Kros.KORM.Query.Expressions;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Expressions
{
    public class SelectExpressionShould
    {
        [Fact]
        public void SetColumnsExpression()
        {
            var tableInfo = new DatabaseMapper(new ConventionModelMapper()).GetTableInfo<Person>();
            var selectExpression = new SelectExpression(tableInfo);

            var expression = new ColumnsExpression("Id");
            selectExpression.SetColumnsExpression(expression);

            selectExpression.ColumnsExpression.Should().Be(expression);
        }

        [Fact]
        public void GetColumnsExpressionIfWasNotSet()
        {
            var tableInfo = new DatabaseMapper(new ConventionModelMapper()).GetTableInfo<Person>();
            var selectExpression = new SelectExpression(tableInfo);

            selectExpression.ColumnsExpression.ColumnsPart.Should().Be("Id, Name, LastName");
        }

        [Fact]
        public void SetTableExpression()
        {
            var tableInfo = new DatabaseMapper(new ConventionModelMapper()).GetTableInfo<Person>();
            var selectExpression = new SelectExpression(tableInfo);

            var expression = new TableExpression("Person");
            selectExpression.SetTableExpression(expression);

            selectExpression.TableExpression.Should().Be(expression);
        }

        [Fact]
        public void GetTableExpressionIfWasNotSet()
        {
            var tableInfo = new DatabaseMapper(new ConventionModelMapper()).GetTableInfo<Person>();
            var selectExpression = new SelectExpression(tableInfo);

            selectExpression.TableExpression.TablePart.Should().Be("TPerson");
        }

        [Fact]
        public void SetOrderByExpression()
        {
            var tableInfo = new DatabaseMapper(new ConventionModelMapper()).GetTableInfo<Person>();
            var selectExpression = new SelectExpression(tableInfo);

            var expression = new OrderByExpression("FirstName");
            selectExpression.SetOrderByExpression(expression);

            selectExpression.OrderByExpression.Should().Be(expression);
        }

        [Fact]
        public void SetGroupByExpression()
        {
            var tableInfo = new DatabaseMapper(new ConventionModelMapper()).GetTableInfo<Person>();
            var selectExpression = new SelectExpression(tableInfo);

            var expression = new GroupByExpression("FirstName");
            selectExpression.SetGroupByExpression(expression);

            selectExpression.GroupByExpression.Should().Be(expression);
        }

        [Alias("TPerson")]
        public class Person
        {
            public int Id { get; set; }

            [Alias("Name")]
            public string FirstName { get; set; }

            public string LastName { get; set; }

            [NoMap]
            public int Age { get; set; }
        }
    }
}
