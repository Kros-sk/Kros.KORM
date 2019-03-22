using System;
using System.Collections.Generic;
using System.Linq;

namespace Kros.KORM.Migrations.Providers
{
    internal static class MigrationScriptsProviderHelper
    {
        public static IEnumerable<ScriptInfo> GetScripts(
            this IMigrationScriptsProvider provider,
            string[] scriptPaths,
            string folder)
        {
            const string extension = ".sql";
            var startIndex = folder.Length;

            return scriptPaths
                .Where(path => path.StartsWith(folder) && path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                .Select(path =>
                {
                    var splits = path.Substring(startIndex + 1, path.Length - startIndex - extension.Length - 1)
                        .Split(new []{ '_' }, 2);

                    if (splits.Length == 2 && long.TryParse(splits[0], out var id))
                    {
                        return new ScriptInfo(provider)
                        {
                            Id = long.Parse(splits[0]),
                            Name = splits[1],
                            Path = path
                        };
                    }
                    else
                    {
                        return null;
                    }
                })
                .Where(s => s != null)
                .OrderBy(s => s.Id);
        }
    }
}
