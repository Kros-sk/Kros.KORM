using FluentAssertions;
using Kros.KORM.Query.Sql;
using System;
using System.Data.SqlClient;
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

            command.Parameters.Should().HaveCount(2);
            command.Parameters[0].ParameterName.Should().Be("@Id");
            command.Parameters[0].Value.Should().Be(1);

            command.Parameters[1].ParameterName.Should().Be("@Age");
            command.Parameters[1].Value.Should().Be(18);
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

            command.Parameters.Should().HaveCount(3);
            command.Parameters[0].ParameterName.Should().Be("@Id");
            command.Parameters[0].Value.Should().Be(0);

            command.Parameters[1].ParameterName.Should().Be("@Name");
            command.Parameters[1].Value.Should().Be("Victor");

            command.Parameters[2].ParameterName.Should().Be("@Name1");
            command.Parameters[2].Value.Should().Be("Thomas");
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

                command.Parameters.Should().HaveCount(3);
                command.Parameters[0].ParameterName.Should().Be("@0");
                command.Parameters[0].Value.Should().Be(0);

                command.Parameters[1].ParameterName.Should().Be("@1");
                command.Parameters[1].Value.Should().Be("Victor");

                command.Parameters[2].ParameterName.Should().Be("@2");
                command.Parameters[2].Value.Should().Be("Milan");
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

            command.Parameters.Should().HaveCount(3);
            command.Parameters[0].ParameterName.Should().Be("@Id");
            command.Parameters[0].Value.Should().Be(0);

            command.Parameters[1].ParameterName.Should().Be("@Name");
            command.Parameters[1].Value.Should().Be("Victor");

            command.Parameters[2].ParameterName.Should().Be("@Name1");
            command.Parameters[2].Value.Should().Be("Thomas");
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

            command.Parameters.Should().HaveCount(4);
            command.Parameters[0].ParameterName.Should().Be("@1");
            command.Parameters[0].Value.Should().Be(1);

            command.Parameters[1].ParameterName.Should().Be("@2");
            command.Parameters[1].Value.Should().Be(3);

            command.Parameters[2].ParameterName.Should().Be("@3");
            command.Parameters[2].Value.Should().Be(5);

            command.Parameters[3].ParameterName.Should().Be("@4");
            command.Parameters[3].Value.Should().Be(6);
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

            command.Parameters.Should().HaveCount(3);
            command.Parameters[0].ParameterName.Should().Be("@A");
            command.Parameters[0].Value.Should().Be(1);

            command.Parameters[1].ParameterName.Should().Be("@2");
            command.Parameters[1].Value.Should().Be("Milan");

            command.Parameters[2].ParameterName.Should().Be("@3");
            command.Parameters[2].Value.Should().Be(DBNull.Value);
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

            command.Parameters.Should().HaveCount(2);
            command.Parameters[0].ParameterName.Should().Be("@A");
            command.Parameters[0].Value.Should().Be(string.Empty);

            command.Parameters[1].ParameterName.Should().Be("@1");
            command.Parameters[1].Value.Should().Be(DBNull.Value);
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

            command.Parameters.Should().HaveCount(4);
            command.Parameters[0].ParameterName.Should().Be("@1");
            command.Parameters[0].Value.Should().Be(1);

            command.Parameters[1].ParameterName.Should().Be("@2");
            command.Parameters[1].Value.Should().Be(2);

            command.Parameters[2].ParameterName.Should().Be("@3");
            command.Parameters[2].Value.Should().Be(3);

            command.Parameters[3].ParameterName.Should().Be("@4");
            command.Parameters[3].Value.Should().Be(4);
        }

        private class Person
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public int Age { get; set; }
        }
    }
}
