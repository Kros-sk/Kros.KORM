namespace Kros.KORM.UnitTests.Integration
{
    internal static class IntegrationTestConfig
    {
        internal static string ConnectionString
            => ConfigurationHelper.GetConfiguration().GetSection("connectionString").Value;
    }
}
