using FluentAssertions;
using Kros.KORM.Migrations.Providers;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kros.KORM.UnitTests.Migrations.Providers
{
    public class AssemblyMigrationScriptsProviderShould
    {
        [Fact]
        public void GetScriptsFromDefaultNamespace()
        {
            string GetNamespace(string fileName)
                => $"Kros.KORM.UnitTests.SqlScripts.{fileName}.sql";

            AssemblyMigrationScriptsProvider provider = CreateDefaultProvider();
            var scripts = provider.GetScripts().ToList();

            scripts.Should().Equals(new[] {
                new ScriptInfo(provider)
                {
                    Id = 20190228001,
                    Name = "InitDatabase",
                    Path = GetNamespace("20190228001_InitDatabase")
                },
                new ScriptInfo(provider)
                {
                    Id = 20190301001,
                    Name = "AddPeopleTable",
                    Path = GetNamespace("20190301001_AddPeopleTable")
                },
                new ScriptInfo(provider)
                {
                    Id = 20190301002,
                    Name = "AddProjectTable",
                    Path = GetNamespace("20190301002_AddProjectTable")
                }
            });
        }

        private static AssemblyMigrationScriptsProvider CreateDefaultProvider()
            => new AssemblyMigrationScriptsProvider(
                Assembly.GetExecutingAssembly(),
                "Kros.KORM.UnitTests.SqlScripts");

        [Fact]
        public void GetScriptFromDefinedAsemblyAndNamespace()
        {
            string GetNamespace(string fileName)
                => $"Kros.KORM.UnitTests.Resources.AnotherSqlScripts.{fileName}.sql";

            var provider = new AssemblyMigrationScriptsProvider(
                Assembly.GetExecutingAssembly(),
                "Kros.KORM.UnitTests.Resources.AnotherSqlScripts");
            var scripts = provider.GetScripts().ToList();

            scripts.Should().Equals(new[]
            {
                new ScriptInfo(provider)
                {
                    Id = 20190227001,
                    Name = "InitDatabase",
                    Path = GetNamespace("20190227001_InitDatabase")
                },
                new ScriptInfo(provider)
                {
                    Id = 20190227002,
                    Name = "AddProjectTable",
                    Path = GetNamespace("20190227002_AddProjectTable")
                },
                new ScriptInfo(provider)
                {
                    Id = 20190227003,
                    Name = "Script_with_separator",
                    Path = GetNamespace("20190227002_Script_with_separator")
                }
            });
        }

        [Fact]
        public async Task LoadScript()
        {
            var provider = CreateDefaultProvider();
            var script = await provider.GetScriptAsync(new ScriptInfo(provider)
            {
                Id = 20190228001,
                Name = "InitDatabase",
                Path = "Kros.KORM.UnitTests.SqlScripts.20190228001_InitDatabase.sql"
            });

            var expected = await GetStringFromResourceFileAsync(
                "Kros.KORM.UnitTests.SqlScripts.20190228001_InitDatabase.sql");

            script.Should().Be(expected);
        }

        private static async Task<string> GetStringFromResourceFileAsync(string resourceFile)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceStream = assembly.GetManifestResourceStream(resourceFile);
            using (var reader = new StreamReader(resourceStream, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }
}
