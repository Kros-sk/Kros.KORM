using Kros.KORM.Query.Expressions;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Expressions
{
    public class OrderByExpressionShould
    {
        [Fact]
        public void ConstructExpressionWithColumnsString()
        {
            var expression = new OrderByExpression("FirstName, LastName");

            Assert.Equal("FirstName, LastName", expression.OrderByPart);
        }

        [Fact]
        public void ConstructExpressionWithoutGroupByKeyWord()
        {
            var expression = new OrderByExpression("Order by FirstName, LastName desc");

            Assert.Equal("FirstName, LastName desc", expression.OrderByPart);
        }

        [Fact]
        public void ConstructExpressionWithoutGroupByKeyWord2()
        {
            var expression = new OrderByExpression("Order   by FirstName, LastName");

            Assert.Equal("FirstName, LastName", expression.OrderByPart);
        }

        [Fact]
        public void ConstructExpressionWithColumnsList()
        {
            var expression = new OrderByExpression("FirstName", "LastName desc");

            Assert.Equal("FirstName, LastName desc", expression.OrderByPart);
        }

        private class Person
        {
            public int Id { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }
        }
    }
}
