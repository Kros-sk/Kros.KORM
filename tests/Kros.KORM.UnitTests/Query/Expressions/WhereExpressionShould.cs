using Kros.KORM.Query.Expressions;
using Kros.KORM.Query.Sql;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Expressions
{
    public class WhereExpressionShould
    {
        [Fact]
        public void ConstructExpressionWithCondition()
        {
            var expression = new WhereExpression("PersonId > @1 and Age > @2", 1, 18);

            Assert.Equal("PersonId > @1 and Age > @2", expression.Sql);
        }

        [Fact]
        public void ConstructExpressionWithoutWhereKeyWord()
        {
            var expression = new WhereExpression("where PersonId > @1 and Age > @2");

            Assert.Equal("PersonId > @1 and Age > @2", expression.Sql);
        }

        [Fact]
        public void ConstructExpressionWithParameters()
        {
            var expression = new WhereExpression("PersonId > @1 and Age > @2", 1, 18);

            Assert.Equal(new object[] { 1, 18 }, expression.Parameters);
        }

        [Fact]
        public void ConstructExpressionWithParametersAndBrackets()
        {
            var command = new SqlCommand();
            var expression = new WhereExpression("(PersonId > @1) and (Age > @2)", 1, 18);
            ParameterExtractingExpressionVisitor.ExtractParametersToCommand(command, expression);

            Assert.Equal("@1", command.Parameters[0].ParameterName);
            Assert.Equal("@2", command.Parameters[1].ParameterName);
        }

        [Fact]
        public void ConstructSubSelectWithWhere()
        {
            var expression = new WhereExpression("where PersonId > @1 and Age > @2 and exists (select id from address where address.id = id)");

            Assert.Equal("PersonId > @1 and Age > @2 and exists (select id from address where address.id = id)", expression.Sql);
        }

        [Fact]
        public void AppendNewAndCondition()
        {
            var expression = new WhereExpression("PersonId > @1", 11);

            WhereExpression actual = expression.And(new WhereExpression("Age > @q1", 18));
            Assert.Equal("(PersonId > @1) AND (Age > @q1)", actual.Sql);
            Assert.Equal(new object[] { 11, 18 }, actual.Parameters);
        }

        [Fact]
        public void AppendNewAndConditionIfConditionDoNotHaveParameters()
        {
            var expression = new WhereExpression("IsDeleted = 0");

            WhereExpression actual = expression.And(new WhereExpression("Age > @q1", 18));
            Assert.Equal("(IsDeleted = 0) AND (Age > @q1)", actual.Sql);
            Assert.Equal(new object[] { 18 }, actual.Parameters);
        }

        [Fact]
        public void AppendNewAndConditionIfNewConditionDoNotHaveParameters()
        {
            var expression = new WhereExpression("PersonId > @1", 11);

            WhereExpression actual = expression.And(new WhereExpression("IsDeleted = 0"));
            Assert.Equal("(PersonId > @1) AND (IsDeleted = 0)", actual.Sql);
            Assert.Equal(new object[] { 11 }, actual.Parameters);
        }

        [Fact]
        public void AppendNewAndConditionWithoutParameters()
        {
            var expression = new WhereExpression("PersonId > 1");

            WhereExpression actual = expression.And(new WhereExpression("IsDeleted = 0"));
            Assert.Equal("(PersonId > 1) AND (IsDeleted = 0)", actual.Sql);
            Assert.Empty(actual.Parameters);
        }
    }
}
