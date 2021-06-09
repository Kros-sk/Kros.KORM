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
            TableInfo tableInfo = new DatabaseMapper(new ConventionModelMapper()).GetTableInfo<Person>();
            var selectExpression = new SelectExpression(tableInfo);

            var expression = new ColumnsExpression("Id");
            selectExpression.SetColumnsExpression(expression);

            selectExpression.ColumnsExpression.Should().Be(expression);
        }

        [Fact]
        public void GetColumnsExpressionIfWasNotSet()
        {
            TableInfo tableInfo = new DatabaseMapper(new ConventionModelMapper()).GetTableInfo<Person>();
            var selectExpression = new SelectExpression(tableInfo);

            selectExpression.ColumnsExpression.ColumnsPart.Should().Be("Id, Name, LastName");
        }

        [Fact]
        public void GetColumnsExpressionWithQuotas()
        {
            TableInfo tableInfo = new DatabaseMapper(new ConventionModelMapper()).GetTableInfo<Person>();
            tableInfo.UseIdentifierDelimiters(Delimiters.SquareBrackets);
            var selectExpression = new SelectExpression(tableInfo);

            selectExpression.ColumnsExpression.ColumnsPart.Should().Be("[Id], [Name], [LastName]");
        }

        [Fact]
        public void SetTableExpression()
        {
            TableInfo tableInfo = new DatabaseMapper(new ConventionModelMapper()).GetTableInfo<Person>();
            var selectExpression = new SelectExpression(tableInfo);

            var expression = new TableExpression("Person");
            selectExpression.SetTableExpression(expression);

            selectExpression.TableExpression.Should().Be(expression);
        }

        [Fact]
        public void GetTableExpressionIfWasNotSet()
        {
            TableInfo tableInfo = new DatabaseMapper(new ConventionModelMapper()).GetTableInfo<Person>();
            var selectExpression = new SelectExpression(tableInfo);

            selectExpression.TableExpression.TablePart.Should().Be("TPerson");
        }

        [Fact]
        public void GetTableExpressionWithQuota()
        {
            TableInfo tableInfo = new DatabaseMapper(new ConventionModelMapper()).GetTableInfo<Person>();
            tableInfo.UseIdentifierDelimiters(Delimiters.SquareBrackets);
            var selectExpression = new SelectExpression(tableInfo);

            selectExpression.TableExpression.TablePart.Should().Be("[TPerson]");
        }

        [Fact]
        public void SetOrderByExpression()
        {
            TableInfo tableInfo = new DatabaseMapper(new ConventionModelMapper()).GetTableInfo<Person>();
            var selectExpression = new SelectExpression(tableInfo);

            var expression = new OrderByExpression("FirstName");
            selectExpression.SetOrderByExpression(expression);

            selectExpression.OrderByExpression.Should().Be(expression);
        }

        [Fact]
        public void SetGroupByExpression()
        {
            TableInfo tableInfo = new DatabaseMapper(new ConventionModelMapper()).GetTableInfo<Person>();
            var selectExpression = new SelectExpression(tableInfo);

            var expression = new GroupByExpression("FirstName");
            selectExpression.SetGroupByExpression(expression);

            selectExpression.GroupByExpression.Should().Be(expression);
        }

        [Fact]
        public void SetWhereConditionMoreTimes()
        {
            TableInfo tableInfo = new DatabaseMapper(new ConventionModelMapper()).GetTableInfo<Person>();
            var selectExpression = new SelectExpression(tableInfo);

            selectExpression.SetWhereExpression(new WhereExpression("PersonId > @1", 11));
            selectExpression.SetWhereExpression(new WhereExpression("IsDeleted = 0"));

            selectExpression.WhereExpression.Sql.Should().Be("(PersonId > @1) AND (IsDeleted = 0)");
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
