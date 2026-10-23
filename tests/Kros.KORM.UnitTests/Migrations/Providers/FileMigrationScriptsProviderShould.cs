using Kros.KORM.Migrations.Providers;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using static System.IO.Path;

namespace Kros.KORM.UnitTests.Migrations.Providers
{
    public class FileMigrationScriptsProviderShould
    {
        private readonly string _folderFullPath;

        public FileMigrationScriptsProviderShould()
        {
            _folderFullPath = Combine(
                GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "Resources",
                "ScriptsFromFiles");
        }

        [Fact]
        public void GetScriptFromDefinedFolder()
        {
            var provider = new FileMigrationScriptsProvider(_folderFullPath);
            var scripts = provider.GetScripts().ToList();

            Assert.Equal(3, scripts.Count);
            Assert.Equivalent(
                new ScriptInfo(provider)
                {
                    Id = 20190228001,
                    Name = "InitDatabase",
                    Path = GetFileFullName("20190228001_InitDatabase")
                },
                scripts[0],
                strict: true);
            Assert.Equivalent(
                new ScriptInfo(provider)
                {
                    Id = 20190301001,
                    Name = "AddPeopleTable",
                    Path = GetFileFullName("20190301001_AddPeopleTable")
                },
                scripts[1],
                strict: true);
            Assert.Equivalent(
                new ScriptInfo(provider)
                {
                    Id = 20190301002,
                    Name = "AddProjectTable",
                    Path = GetFileFullName("20190301002_AddProjectTable")
                },
                scripts[2],
                strict: true);
        }

        [Fact]
        public async Task LoadScript()
        {
            var provider = new FileMigrationScriptsProvider(_folderFullPath);
            var script = await provider.GetScriptAsync(new ScriptInfo(provider)
            {
                Id = 20190228001,
                Name = "InitDatabase",
                Path = GetFileFullName("20190228001_InitDatabase")
            });

            var expected = File.ReadAllText(GetFileFullName("20190228001_InitDatabase"));
            Assert.Equal(expected, script);
        }

        private string GetFileFullName(string fileName)
                => $"{_folderFullPath}\\{fileName}.sql";
    }
}
