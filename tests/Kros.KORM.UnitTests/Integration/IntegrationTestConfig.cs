namespace Kros.KORM.UnitTests.Integration
{
    internal static class IntegrationTestConfig
    {
        internal static string ConnectionString
            => Helpers.GetConfiguration().GetSection("ConnectionStrings:DefaultConnection").Value;
    }
}
