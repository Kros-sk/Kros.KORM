using FluentAssertions;
using Kros.KORM.Query.Expressions;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Expressions
{
    public class TableExpressionShould
    {
        [Fact]
        public void ConstructExpressionWithTable()
        {
            var expression = new TableExpression("Person as p join Avatar as a ON (p.Id = a.PersonId)");

            expression.TablePart.Should().Be("Person as p join Avatar as a ON (p.Id = a.PersonId)");
        }

        [Fact]
        public void ConstructExpressionWithoutFromKeyWord()
        {
            var expression = new TableExpression("From Person as p join Avatar as a ON (p.Id = a.PersonId)");

            expression.TablePart.Should().Be("Person as p join Avatar as a ON (p.Id = a.PersonId)");
        }
    }
}
