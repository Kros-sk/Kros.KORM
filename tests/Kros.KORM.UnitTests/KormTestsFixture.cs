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
        _msSqlContainer = new MsSqlBuilder().Build();
    }

    internal string GetConnectionString() => _msSqlContainer.GetConnectionString();

    public async ValueTask DisposeAsync() => await _msSqlContainer.DisposeAsync();

    public async ValueTask InitializeAsync() => await _msSqlContainer.StartAsync();
}
