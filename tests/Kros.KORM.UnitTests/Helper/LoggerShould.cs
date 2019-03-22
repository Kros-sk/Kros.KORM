using FluentAssertions;
using Kros.KORM.Helper;
using System.Data.SqlClient;
using System.Text;
using Xunit;

namespace Kros.KORM.UnitTests.Helper
{
    public class LoggerShould
    {
        [Fact]
        public void LogCommand()
        {
            var sb = new StringBuilder();
            string log = string.Empty;
            Database.Log = (a) => sb.Append(a);

            var logger = new Logger();
            var command = new SqlCommand("SELECT * FROM PERSON WHERE Name = @1");
            var param = command.CreateParameter();
            param.ParameterName = "@1";
            param.Value = "Milan";
            command.Parameters.Add(param);

            logger.LogCommand(command);

            log = sb.ToString();
            log = log.Substring(log.IndexOf("-"), log.Length - log.IndexOf("-"));

            log.Should().BeEquivalentTo("- SELECT * FROM PERSON WHERE Name = @1  WITH PARAMETERS (Milan)");
        }
    }
}
