using Kros.KORM.Data;
using Kros.KORM.UnitTests.Helper;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kros.KORM.UnitTests.Data
{
    public class ConnectionHelperShould
    {
        [Fact]
        public async Task OpenAsync_OpensClosedConnectionAsynchronously()
        {
            var cn = new FakeDbConnection();

            using var helper = await ConnectionHelper.OpenAsync(cn, TestContext.Current.CancellationToken);

            Assert.Equal(1, cn.OpenAsyncCount);
            Assert.Equal(0, cn.OpenCount);
            Assert.True(helper.CloseConnection);
        }

        [Fact]
        public async Task OpenAsync_DoesNotOpenAlreadyOpenConnection()
        {
            var cn = new FakeDbConnection();
            cn.Open();

            var helper = await ConnectionHelper.OpenAsync(cn, TestContext.Current.CancellationToken);
            helper.Dispose();

            Assert.Equal(0, cn.OpenAsyncCount);
            Assert.False(helper.CloseConnection);
            Assert.Equal(0, cn.CloseCount);
        }

        [Fact]
        public async Task OpenAsync_ClosesOnDisposeWhenItOpened()
        {
            var cn = new FakeDbConnection();

            var helper = await ConnectionHelper.OpenAsync(cn, TestContext.Current.CancellationToken);
            helper.Dispose();

            Assert.Equal(1, cn.CloseCount);
        }

        [Fact]
        public async Task OpenAsync_ThrowsWhenTokenAlreadyCancelled()
        {
            var cn = new FakeDbConnection();

            await Assert.ThrowsAnyAsync<System.OperationCanceledException>(
                () => ConnectionHelper.OpenAsync(cn, new CancellationToken(true)));

            Assert.Equal(0, cn.OpenAsyncCount);
        }
    }
}
