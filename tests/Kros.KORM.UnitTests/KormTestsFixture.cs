using System;
using System.Threading.Tasks;
using Testcontainers.MsSql;
using Xunit;

namespace Kros.KORM.UnitTests;

public sealed class KormTestsFixture : IAsyncDisposable, IAsyncLifetime
{
    private readonly MsSqlContainer _msSqlContainer;

    public KormTestsFixture()
    {
        // https://testcontainers.com/modules/mssql/?language=dotnet
        _msSqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU10-ubuntu-22.04").Build();
    }

    internal string GetConnectionString() => _msSqlContainer.GetConnectionString();

    public async ValueTask DisposeAsync() => await _msSqlContainer.DisposeAsync();

    public async ValueTask InitializeAsync() => await _msSqlContainer.StartAsync();
}
