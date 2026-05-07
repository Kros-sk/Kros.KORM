using Kros.KORM.Metadata;
using Kros.KORM.Metadata.Attribute;
using Kros.KORM.Query.Expressions;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Expressions
{
    public class ColumnsExpressionShould
    {
        [Fact]
        public void ConstructExpressionWithColumnsString()
        {
            var expression = new ColumnsExpression("Id, FirstName, LastName");

            Assert.Equal("Id, FirstName, LastName", expression.ColumnsPart);
        }

        [Fact]
        public void ConstructExpressionWithoutSelectKeyWord()
        {
            var expression = new ColumnsExpression("Select Id, FirstName, LastName");

            Assert.Equal("Id, FirstName, LastName", expression.ColumnsPart);
        }

        [Fact]
        public void ConstructExpressionWithColumnsList()
        {
            var expression = new ColumnsExpression("Id", "FirstName", "LastName");

            Assert.Equal("Id, FirstName, LastName", expression.ColumnsPart);
        }

        [Fact]
        public void ConstructExpressionWithSelector()
        {
            var expression = ColumnsExpression.Create<Person, object>((p) => new { p.Id, p.FirstName, p.LastName },
                GetTableInfo());

            Assert.Equal("Id, Name, LastName", expression.ColumnsPart);
        }

        private TableInfo GetTableInfo()
        {
            return new DatabaseMapper(new ConventionModelMapper()).GetTableInfo<Person>();
        }

        private class Person
        {
            public int Id { get; set; }

            [Alias("Name")]
            public string FirstName { get; set; }

            public string LastName { get; set; }
        }
    }
}
