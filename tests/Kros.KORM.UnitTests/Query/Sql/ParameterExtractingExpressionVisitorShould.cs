using Kros.KORM.Query.Sql;
using Microsoft.Data.SqlClient;
using System;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Sql
{
    public class ParameterExtractingExpressionVisitorShould
    {
        [Fact]
        public void ExtractParamsFromSelectExpression()
        {
            var connection = new SqlConnection();
            var database = new Database(connection);

            var query = database.Query<Person>().Where("Id > @Id and Age > @Age", 1, 18).OrderBy("Name");
            var command = connection.CreateCommand();

            ParameterExtractingExpressionVisitor.ExtractParametersToCommand(command, query.Expression);

            Assert.Equal(2, command.Parameters.Count);
            Assert.Equal("@Id", command.Parameters[0].ParameterName);
            Assert.Equal(1, command.Parameters[0].Value);

            Assert.Equal("@Age", command.Parameters[1].ParameterName);
            Assert.Equal(18, command.Parameters[1].Value);
        }

        [Fact]
        public void ExtractParamsFromSqlExpression()
        {
            var connection = new SqlConnection();
            var database = new Database(connection);

            var query = database.Query<Person>()
                .Sql("Select * from Person where (Id = @Id Or Name = @Name Or Name = @Name1)", 0, "Victor", "Thomas");
            var command = connection.CreateCommand();

            ParameterExtractingExpressionVisitor.ExtractParametersToCommand(command, query.Expression);

            Assert.Equal(3, command.Parameters.Count);
            Assert.Equal("@Id", command.Parameters[0].ParameterName);
            Assert.Equal(0, command.Parameters[0].Value);

            Assert.Equal("@Name", command.Parameters[1].ParameterName);
            Assert.Equal("Victor", command.Parameters[1].Value);

            Assert.Equal("@Name1", command.Parameters[2].ParameterName);
            Assert.Equal("Thomas", command.Parameters[2].Value);
        }

        [Fact]
        public void ExtractParamsFromSqlExpressionInterpolated()
        {
            using (var connection = new SqlConnection())
            using (var database = new Database(connection))
            {
                var name = "Milan";
                var query = database.Query<Person>()
                    .Sql($"Select * from Person where (Id = {0} Or Name = {"Victor"} Or Name = {name})");
                var command = connection.CreateCommand();

                ParameterExtractingExpressionVisitor.ExtractParametersToCommand(command, query.Expression);

                Assert.Equal(3, command.Parameters.Count);
                Assert.Equal("@0", command.Parameters[0].ParameterName);
                Assert.Equal(0, command.Parameters[0].Value);

                Assert.Equal("@1", command.Parameters[1].ParameterName);
                Assert.Equal("Victor", command.Parameters[1].Value);

                Assert.Equal("@2", command.Parameters[2].ParameterName);
                Assert.Equal("Milan", command.Parameters[2].Value);
            }
        }

        [Fact]
        public void ExtractParamsFromSqlExpressionWithEnter()
        {
            var connection = new SqlConnection();
            var database = new Database(connection);

            var query = database.Query<Person>()
                .Sql(@"Select * from Person where (Id = @Id
                                                Or Name = @Name     Or Name = @Name1)", 0, "Victor", "Thomas");
            var command = connection.CreateCommand();

            ParameterExtractingExpressionVisitor.ExtractParametersToCommand(command, query.Expression);

            Assert.Equal(3, command.Parameters.Count);
            Assert.Equal("@Id", command.Parameters[0].ParameterName);
            Assert.Equal(0, command.Parameters[0].Value);

            Assert.Equal("@Name", command.Parameters[1].ParameterName);
            Assert.Equal("Victor", command.Parameters[1].Value);

            Assert.Equal("@Name1", command.Parameters[2].ParameterName);
            Assert.Equal("Thomas", command.Parameters[2].Value);
        }

        [Fact]
        public void ExtractParamsFromSqlExpressionWithInOperator()
        {
            var connection = new SqlConnection();
            var database = new Database(connection);

            var query = database.Query<Person>()
                .Sql(@"Select * from Person where Id IN (@1,@2, @3 , @4)", 1, 3, 5, 6);
            var command = connection.CreateCommand();

            ParameterExtractingExpressionVisitor.ExtractParametersToCommand(command, query.Expression);

            Assert.Equal(4, command.Parameters.Count);
            Assert.Equal("@1", command.Parameters[0].ParameterName);
            Assert.Equal(1, command.Parameters[0].Value);

            Assert.Equal("@2", command.Parameters[1].ParameterName);
            Assert.Equal(3, command.Parameters[1].Value);

            Assert.Equal("@3", command.Parameters[2].ParameterName);
            Assert.Equal(5, command.Parameters[2].Value);

            Assert.Equal("@4", command.Parameters[3].ParameterName);
            Assert.Equal(6, command.Parameters[3].Value);
        }

        [Fact]
        public void ExtractParamsFromSqlExpressionWithMultiplyOccurrences()
        {
            var connection = new SqlConnection();
            var database = new Database(connection);

            var query = database.Query<Person>()
                .Sql(@"Select * from Person where Id = @A AND FirstName = @2 AND LastName = @2 AND EMail = @3 AND SupervisorId = @A", 1, "Milan", null);
            var command = connection.CreateCommand();

            ParameterExtractingExpressionVisitor.ExtractParametersToCommand(command, query.Expression);

            Assert.Equal(3, command.Parameters.Count);
            Assert.Equal("@A", command.Parameters[0].ParameterName);
            Assert.Equal(1, command.Parameters[0].Value);

            Assert.Equal("@2", command.Parameters[1].ParameterName);
            Assert.Equal("Milan", command.Parameters[1].Value);

            Assert.Equal("@3", command.Parameters[2].ParameterName);
            Assert.Equal(DBNull.Value, command.Parameters[2].Value);
        }

        [Fact]
        public void ExtractNullParamFromSqlExpression()
        {
            var connection = new SqlConnection();
            var database = new Database(connection);

            var query = database.Query<Person>()
                .Sql(@"Select * from Person where Id = @A AND FirstName = @1", string.Empty, null);
            var command = connection.CreateCommand();

            ParameterExtractingExpressionVisitor.ExtractParametersToCommand(command, query.Expression);

            Assert.Equal(2, command.Parameters.Count);
            Assert.Equal("@A", command.Parameters[0].ParameterName);
            Assert.Equal(string.Empty, command.Parameters[0].Value);

            Assert.Equal("@1", command.Parameters[1].ParameterName);
            Assert.Equal(DBNull.Value, command.Parameters[1].Value);
        }

        [Fact]
        public void ExtractParamFromWhereExpressionWhereIsFunction()
        {
            var connection = new SqlConnection();
            var database = new Database(connection);

            var query = database.Query<Person>()
                .Where(@"Col1 = @1 AND Col2 <> @2 AND
                      Col3 <> @3 AND Col4 = @4 AND ROUND((Col5 - Col6), 6) > 0", 1, 2, 3, 4);
            var command = connection.CreateCommand();

            ParameterExtractingExpressionVisitor.ExtractParametersToCommand(command, query.Expression);

            Assert.Equal(4, command.Parameters.Count);
            Assert.Equal("@1", command.Parameters[0].ParameterName);
            Assert.Equal(1, command.Parameters[0].Value);

            Assert.Equal("@2", command.Parameters[1].ParameterName);
            Assert.Equal(2, command.Parameters[1].Value);

            Assert.Equal("@3", command.Parameters[2].ParameterName);
            Assert.Equal(3, command.Parameters[2].Value);

            Assert.Equal("@4", command.Parameters[3].ParameterName);
            Assert.Equal(4, command.Parameters[3].Value);
        }

        private class Person
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public int Age { get; set; }
        }
    }
}
