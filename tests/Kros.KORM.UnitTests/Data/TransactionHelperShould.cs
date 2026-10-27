using Kros.KORM.Data;
using Kros.KORM.UnitTests.Helper;
using System;
using System.Data;
using System.Threading;
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
        public async Task BeginTransactionAsync_OpensConnectionAsynchronously()
        {
            var cn = new FakeDbConnection();
            var helper = new TransactionHelper(cn);

            var tx = await helper.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, TestContext.Current.CancellationToken);

            Assert.Equal(1, cn.OpenAsyncCount);
            Assert.Equal(0, cn.OpenCount);
            tx.Dispose();
            Assert.Equal(1, cn.CloseCount);
        }

        [Fact]
        public async Task BeginTransactionAsync_NestedDoesNotReopen()
        {
            var cn = new FakeDbConnection();
            var helper = new TransactionHelper(cn);

            using var outer = await helper.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, TestContext.Current.CancellationToken);
            var current = helper.CurrentTransaction;
            using var inner = await helper.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, TestContext.Current.CancellationToken);

            Assert.Equal(1, cn.OpenAsyncCount);
            Assert.Same(current, helper.CurrentTransaction);

            await inner.CommitAsync(TestContext.Current.CancellationToken);
            await outer.CommitAsync(TestContext.Current.CancellationToken);

            Assert.Equal(1, cn.LastTransaction.CommitAsyncCount);
        }

        [Fact]
        public async Task BeginTransactionAsync_ThrowsWhenTokenAlreadyCancelled()
        {
            var cn = new FakeDbConnection();
            var helper = new TransactionHelper(cn);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => helper.BeginTransactionAsync(IsolationLevel.ReadCommitted, new CancellationToken(canceled: true)));

            Assert.Equal(0, cn.OpenAsyncCount);
            Assert.Null(helper.CurrentTransaction);
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
