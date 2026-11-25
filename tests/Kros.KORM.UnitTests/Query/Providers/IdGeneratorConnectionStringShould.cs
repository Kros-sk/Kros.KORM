using Kros.KORM.Query;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Providers
{
    public class IdGeneratorConnectionStringShould
    {
        private const string Server = "Server=sql.example.com;Database=Projects;";

        [Theory]
        [InlineData("Authentication=Active Directory Managed Identity;User Id=6b0e5e9e-3c1f-4a8e-9f5a-2d6c1b7a0e11", false)]
        [InlineData("Authentication=Active Directory Managed Identity;User Id=6b0e5e9e-3c1f-4a8e-9f5a-2d6c1b7a0e11", true)]
        [InlineData("Authentication=Active Directory Managed Identity", true)]
        [InlineData("Authentication=Active Directory Default", true)]
        [InlineData("Integrated Security=True", true)]
        [InlineData("User Id=sa;Password=secret", true)]
        [InlineData("User Id=sa", false)]
        public void UseOwnApplicationName(string authentication, bool passwordMayBeHidden)
        {
            string connectionString = QueryProvider.GetIdGeneratorConnectionString(Server + authentication, passwordMayBeHidden);

            var builder = new SqlConnectionStringBuilder(connectionString);
            var original = new SqlConnectionStringBuilder(Server + authentication);
            Assert.Equal(original.ApplicationName + QueryProvider.IdGeneratorApplicationNameSuffix, builder.ApplicationName);
            Assert.Equal(original.UserID, builder.UserID);
            Assert.Equal(original.Password, builder.Password);
            Assert.Equal(original.Authentication, builder.Authentication);
        }

        [Theory]
        [InlineData("User Id=sa")]
        [InlineData("Authentication=SqlPassword;User Id=sa")]
        [InlineData("Authentication=Active Directory Password;User Id=user@example.com")]
        public void ReturnNullWhenPasswordIsHidden(string authentication)
            => Assert.Null(QueryProvider.GetIdGeneratorConnectionString(Server + authentication, passwordMayBeHidden: true));
    }
}
