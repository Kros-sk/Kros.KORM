using Kros.KORM.Query;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Providers
{
    public class IdGeneratorConnectionStringShould
    {
        private const string Server = "Server=sql.example.com;Database=Projects;";

        [Theory]
        [InlineData("Authentication=Active Directory Managed Identity;User Id=6b0e5e9e-3c1f-4a8e-9f5a-2d6c1b7a0e11")]
        [InlineData("Authentication=Active Directory Managed Identity")]
        [InlineData("Authentication=Active Directory MSI")]
        [InlineData("User Id=sa;Password=secret")]
        [InlineData("Authentication=SqlPassword;User Id=sa;Password=secret")]
        public void UseOwnApplicationName(string authentication)
        {
            string connectionString = QueryProvider.GetIdGeneratorConnectionString(Server + authentication);

            var builder = new SqlConnectionStringBuilder(connectionString);
            var original = new SqlConnectionStringBuilder(Server + authentication);
            Assert.Equal(original.ApplicationName + QueryProvider.IdGeneratorApplicationNameSuffix, builder.ApplicationName);
            Assert.Equal(original.UserID, builder.UserID);
            Assert.Equal(original.Password, builder.Password);
            Assert.Equal(original.Authentication, builder.Authentication);
        }

        [Theory]
        [InlineData("User Id=sa")]
        [InlineData("Integrated Security=True")]
        [InlineData("Authentication=Active Directory Default")]
        [InlineData("Authentication=Active Directory Service Principal;User Id=6b0e5e9e-3c1f-4a8e-9f5a-2d6c1b7a0e11")]
        public void ReturnNullForOtherAuthentication(string authentication)
            => Assert.Null(QueryProvider.GetIdGeneratorConnectionString(Server + authentication));
    }
}
