using FluentAssertions;
using Kros.KORM.Query.Expressions;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Expressions
{
    public class GroupByExpressionShould
    {
        [Fact]
        public void ConstructExpressionWithColumnsString()
        {
            var expression = new GroupByExpression("FirstName, LastName");

            expression.GroupByPart.Should().Be("FirstName, LastName");
        }

        [Fact]
        public void ConstructExpressionWithoutGroupByKeyWord()
        {
            var expression = new GroupByExpression("Group by FirstName, LastName");

            expression.GroupByPart.Should().Be("FirstName, LastName");
        }

        [Fact]
        public void ConstructExpressionWithoutGroupByKeyWord2()
        {
            var expression = new GroupByExpression("Group   by FirstName, LastName");

            expression.GroupByPart.Should().Be("FirstName, LastName");
        }

        [Fact]
        public void ConstructExpressionWithColumnsList()
        {
            var expression = new GroupByExpression("FirstName", "LastName");

            expression.GroupByPart.Should().Be("FirstName, LastName");
        }

        [Fact]
        public void ConstructExpressionWithSelector()
        {
            var expression = GroupByExpression.Create<Person, object>((p) => new {p.FirstName, p.LastName });

            expression.GroupByPart.Should().Be("FirstName, LastName");
        }

        private class Person
        {
            public int Id { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }
        }
    }
}
