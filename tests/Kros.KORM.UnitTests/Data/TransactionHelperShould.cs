using Kros.KORM.Data;
using Kros.KORM.UnitTests.Helper;
using System.Threading.Tasks;
using Xunit;

namespace Kros.KORM.UnitTests.Data
{
    public class TransactionHelperShould
    {
        [Fact]
        public async Task RollbackAsync_CallsAsyncRollbackOnDbTransaction()
        {
            var cn = new FakeDbConnection();
            var helper = new TransactionHelper(cn);
            using var tx = helper.BeginTransaction();
            _ = helper.CurrentTransaction;

            await tx.RollbackAsync(TestContext.Current.CancellationToken);

            Assert.Equal(1, cn.LastTransaction.RollbackAsyncCount);
            Assert.Equal(0, cn.LastTransaction.RollbackCount);
        }

        [Fact]
        public void Rollback_CallsSyncRollbackOnDbTransaction()
        {
            var cn = new FakeDbConnection();
            var helper = new TransactionHelper(cn);
            using var tx = helper.BeginTransaction();
            _ = helper.CurrentTransaction;

            tx.Rollback();

            Assert.Equal(1, cn.LastTransaction.RollbackCount);
            Assert.Equal(0, cn.LastTransaction.RollbackAsyncCount);
        }
    }
}
