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
            var expression = new WhereExpression("PersonId > @1 and Age > @2", 1, 18);

            expression.Sql.Should().Be("PersonId > @1 and Age > @2");
        }

        [Fact]
        public void ConstructExpressionWithoutWhereKeyWord()
        {
            var expression = new WhereExpression("where PersonId > @1 and Age > @2");

            expression.Sql.Should().Be("PersonId > @1 and Age > @2");
        }

        [Fact]
        public void ConstructExpressionWithParameters()
        {
            var expression = new WhereExpression("PersonId > @1 and Age > @2", 1, 18);

            expression.Parameters.Should().Equal(1, 18);
        }

        [Fact]
        public void ConstructExpressionWithParametersAndBrackets()
        {
            var command = new SqlCommand();
            var expression = new WhereExpression("(PersonId > @1) and (Age > @2)", 1, 18);
            ParameterExtractingExpressionVisitor.ExtractParametersToCommand(command, expression);

            command.Parameters[0].ParameterName.Should().Be("@1");
            command.Parameters[1].ParameterName.Should().Be("@2");
        }

        [Fact]
        public void ConstructSubSelectWithWhere()
        {
            var expression = new WhereExpression("where PersonId > @1 and Age > @2 and exists (select id from address where address.id = id)");

            expression.Sql.Should().Be("PersonId > @1 and Age > @2 and exists (select id from address where address.id = id)");
        }

        [Fact]
        public void AppendNewAndCondition()
        {
            var expression = new WhereExpression("PersonId > @1", 11);

            WhereExpression actual = expression.And(new WhereExpression("Age > @q1", 18));
            actual.Sql.Should().Be("(PersonId > @1) AND (Age > @q1)");
            actual.Parameters.Should().Equal(11, 18);
        }

        [Fact]
        public void AppendNewAndConditionIfConditionDoNotHaveParameters()
        {
            var expression = new WhereExpression("IsDeleted = 0");

            WhereExpression actual = expression.And(new WhereExpression("Age > @q1", 18));
            actual.Sql.Should().Be("(IsDeleted = 0) AND (Age > @q1)");
            actual.Parameters.Should().Equal(18);
        }

        [Fact]
        public void AppendNewAndConditionIfNewConditionDoNotHaveParameters()
        {
            var expression = new WhereExpression("PersonId > @1", 11);

            WhereExpression actual = expression.And(new WhereExpression("IsDeleted = 0"));
            actual.Sql.Should().Be("(PersonId > @1) AND (IsDeleted = 0)");
            actual.Parameters.Should().Equal(11);
        }

        [Fact]
        public void AppendNewAndConditionWithoutParameters()
        {
            var expression = new WhereExpression("PersonId > 1");

            WhereExpression actual = expression.And(new WhereExpression("IsDeleted = 0"));
            actual.Sql.Should().Be("(PersonId > 1) AND (IsDeleted = 0)");
            actual.Parameters.Should().BeEmpty();
        }
    }
}
