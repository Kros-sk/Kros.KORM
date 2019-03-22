using FluentAssertions;
using Kros.KORM.Query.Expressions;
using Kros.KORM.Query.Sql;
using System.Data.SqlClient;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Expressions
{
    public class WhereExpressionShould
    {
        [Fact]
        public void ConstructExpressionWithCondition()
        {
            var expression = new WhereExpression("Person Id > @1 and Age > @2", 1, 18);

            expression.Sql.Should().Be("Person Id > @1 and Age > @2");
        }

        [Fact]
        public void ConstructExpressionWithoutWhereKeyWord()
        {
            var expression = new WhereExpression("where Person Id > @1 and Age > @2");

            expression.Sql.Should().Be("Person Id > @1 and Age > @2");
        }

        [Fact]
        public void ConstructExpressionWithParameters()
        {
            var expression = new WhereExpression("Person Id > @1 and Age > @2", 1, 18);

            expression.Parameters.Should().Equal(1, 18);
        }

        [Fact]
        public void ConstructExpressionWithParametersAndBrackets()
        {
            var command = new SqlCommand();
            var expression = new WhereExpression("(Person Id > @1) and (Age > @2)", 1, 18);
            ParameterExtractingExpressionVisitor.ExtractParametersToCommand(command, expression);

            command.Parameters[0].ParameterName.Should().Be("@1");
            command.Parameters[1].ParameterName.Should().Be("@2");
        }

        [Fact]
        public void ConstructSubSelectWithWhere()
        {
            var expression = new WhereExpression("where Person Id > @1 and Age > @2 and exists (select id from address where address.id = id)");

            expression.Sql.Should().Be("Person Id > @1 and Age > @2 and exists (select id from address where address.id = id)");
        }
    }
}
