using FluentAssertions;
using Xunit;

namespace Kros.KORM.UnitTests
{
    public class KormConnectionSettingsShould
    {
        [Theory]
        [InlineData("server=localhost", "server=localhost", KormConnectionSettings.DefaultProviderName, KormConnectionSettings.DefaultAutoMigrate)]
        [InlineData("server=localhost;KormAutoMigrate=LoremIpsum;KormProvider='  '", "server=localhost", KormConnectionSettings.DefaultProviderName, KormConnectionSettings.DefaultAutoMigrate)]
        [InlineData("server=localhost;KormAutoMigrate=true;KormProvider=LoremIpsum", "server=localhost", "LoremIpsum", true)]
        public void ParseConnectionString(string fullCnstr, string cnstr, string kormProvider, bool autoMigrate)
        {
            var settings = new KormConnectionSettings(fullCnstr);
            settings.ConnectionString.Should().Be(cnstr, "Connection string was parsed.");
            settings.KormProvider.Should().Be(kormProvider, "KORM provider was parsed.");
            settings.AutoMigrate.Should().Be(autoMigrate, "KORM auto migrate was parsed.");
        }

        [Theory]
        [InlineData("server=localhost", "server=localhost")]
        [InlineData("server=localhost;KormAutoMigrate=false;KormProvider=" + KormConnectionSettings.DefaultProviderName, "server=localhost")]
        [InlineData("server=localhost;KormAutoMigrate=LoremIpsum", "server=localhost")]
        [InlineData("server=localhost;KormAutoMigrate=true;KormProvider=LoremIpsum", "server=localhost;KormAutoMigrate=True;KormProvider='LoremIpsum'")]
        public void BuildConnectionString(string inputCnstr, string outputCnstr)
        {
            var settings = new KormConnectionSettings(inputCnstr);
            settings.GetFullConnectionString().Should().Be(outputCnstr);
        }
    }
}
