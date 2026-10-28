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
        public async Task BeginTransactionAsync_ThrowsWhenTokenAlreadyCancelled_OnOpenConnection()
        {
            var cn = new FakeDbConnection();
            cn.Open();
            var helper = new TransactionHelper(cn);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => helper.BeginTransactionAsync(IsolationLevel.ReadCommitted, new CancellationToken(canceled: true)));

            Assert.Null(helper.CurrentTransaction);
        }

        [Fact]
        public async Task RollbackAsync_WithCancelledToken_StillEndsTransaction()
        {
            var cn = new FakeDbConnection();
            var helper = new TransactionHelper(cn);
            using var tx = helper.BeginTransaction();
            _ = helper.CurrentTransaction;

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => tx.RollbackAsync(new CancellationToken(canceled: true)));

            Assert.Null(helper.CurrentTransaction);
        }

        [Fact]
        public async Task CommitAsync_WithCancelledToken_StillEndsTransaction()
        {
            var cn = new FakeDbConnection();
            var helper = new TransactionHelper(cn);
            using var tx = helper.BeginTransaction();
            _ = helper.CurrentTransaction;

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => tx.CommitAsync(new CancellationToken(canceled: true)));

            Assert.Null(helper.CurrentTransaction);
            Assert.Equal(0, cn.LastTransaction.CommitAsyncCount);
        }

        [Fact]
        public async Task CommitAsync_WithCancelledToken_ThenRollbackAsync_SurfacesCancellationAndEndsOnce()
        {
            var cn = new FakeDbConnection();
            var helper = new TransactionHelper(cn);
            using (var tx = helper.BeginTransaction())
            {
                _ = helper.CurrentTransaction;

                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => tx.CommitAsync(new CancellationToken(canceled: true)));
                await tx.RollbackAsync(CancellationToken.None);

                Assert.Equal(1, cn.LastTransaction.RollbackAsyncCount);
                Assert.Null(helper.CurrentTransaction);
            }
            Assert.Equal(1, cn.CloseCount);

            using var next = helper.BeginTransaction();
            Assert.NotNull(helper.CurrentTransaction);
            Assert.Equal(2, cn.OpenCount);
            await next.CommitAsync(TestContext.Current.CancellationToken);
            Assert.Equal(1, cn.LastTransaction.CommitAsyncCount);
            Assert.Null(helper.CurrentTransaction);
        }

        [Fact]
        public void Commit_WhenDbCommitFails_ThenRollback_SurfacesOriginalExceptionAndEndsOnce()
        {
            var cn = new FakeDbConnection();
            var helper = new TransactionHelper(cn);
            using var tx = helper.BeginTransaction();
            _ = helper.CurrentTransaction;
            var commitException = new InvalidOperationException("Commit failed.");
            cn.LastTransaction.ThrowOnCommit = commitException;

            var ex = Assert.Throws<InvalidOperationException>(() => tx.Commit());
            Assert.Same(commitException, ex);
            tx.Rollback();

            Assert.Equal(1, cn.LastTransaction.RollbackCount);
            Assert.Null(helper.CurrentTransaction);
        }

        [Fact]
        public void Dispose_WithoutAnyCommand_DoesNotCreateDbTransaction()
        {
            var cn = new FakeDbConnection();
            var helper = new TransactionHelper(cn);
            var tx = helper.BeginTransaction();

            tx.Dispose();

            Assert.Null(cn.LastTransaction);
            Assert.Null(helper.CurrentTransaction);
            Assert.Equal(1, cn.CloseCount);
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

        [Fact]
        public void Commit_AfterNestedRollback_RollsBackAndEndsTransaction()
        {
            var cn = new FakeDbConnection();
            var helper = new TransactionHelper(cn);
            var outer = helper.BeginTransaction();
            var inner = helper.BeginTransaction();
            _ = helper.CurrentTransaction;

            inner.Rollback();
            outer.Commit();

            Assert.Equal(0, cn.LastTransaction.CommitCount);
            Assert.Equal(1, cn.LastTransaction.RollbackCount);
            Assert.Null(helper.CurrentTransaction);
            inner.Dispose();
            outer.Dispose();
            Assert.Equal(1, cn.CloseCount);

            using var next = helper.BeginTransaction();
            Assert.Equal(2, cn.OpenCount);
            Assert.NotNull(helper.CurrentTransaction);
        }

        [Fact]
        public async Task CommitAsync_AfterNestedRollback_RollsBackAndEndsTransaction()
        {
            var cn = new FakeDbConnection();
            var helper = new TransactionHelper(cn);
            var outer = helper.BeginTransaction();
            var inner = helper.BeginTransaction();
            _ = helper.CurrentTransaction;

            await inner.RollbackAsync(TestContext.Current.CancellationToken);
            await outer.CommitAsync(TestContext.Current.CancellationToken);

            Assert.Equal(0, cn.LastTransaction.CommitAsyncCount);
            Assert.Equal(1, cn.LastTransaction.RollbackAsyncCount);
            Assert.Equal(0, cn.LastTransaction.RollbackCount);
            Assert.Null(helper.CurrentTransaction);
            inner.Dispose();
            outer.Dispose();
            Assert.Equal(1, cn.CloseCount);

            using var next = helper.BeginTransaction();
            Assert.Equal(2, cn.OpenCount);
            Assert.NotNull(helper.CurrentTransaction);
        }

        [Fact]
        public void Commit_AfterNestedRollback_WithoutAnyCommand_DoesNotCreateDbTransaction()
        {
            var cn = new FakeDbConnection();
            var helper = new TransactionHelper(cn);
            var outer = helper.BeginTransaction();
            var inner = helper.BeginTransaction();

            inner.Rollback();
            outer.Commit();

            Assert.Null(cn.LastTransaction);
            Assert.Null(helper.CurrentTransaction);
            inner.Dispose();
            outer.Dispose();
            Assert.Null(cn.LastTransaction);
            Assert.Equal(1, cn.CloseCount);
        }

        [Fact]
        public void Rollback_SkipsDbRollbackWhenTransactionAlreadyCompleted()
        {
            var cn = new FakeDbConnection();
            var helper = new TransactionHelper(cn);
            using var tx = helper.BeginTransaction();
            _ = helper.CurrentTransaction;
            var commitException = new InvalidOperationException("commit failed");
            cn.LastTransaction.ThrowOnCommit = commitException;
            cn.LastTransaction.IsCompleted = true;

            var ex = Assert.Throws<InvalidOperationException>(() => tx.Commit());
            Assert.Same(commitException, ex);
            tx.Rollback();

            Assert.Equal(0, cn.LastTransaction.RollbackCount);
            Assert.Null(helper.CurrentTransaction);
        }

        [Fact]
        public async Task RollbackAsync_SkipsDbRollbackWhenTransactionAlreadyCompleted()
        {
            var cn = new FakeDbConnection();
            var helper = new TransactionHelper(cn);
            using var tx = helper.BeginTransaction();
            _ = helper.CurrentTransaction;
            var commitException = new InvalidOperationException("commit failed");
            cn.LastTransaction.ThrowOnCommit = commitException;
            cn.LastTransaction.IsCompleted = true;

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => tx.CommitAsync(TestContext.Current.CancellationToken));
            Assert.Same(commitException, ex);
            await tx.RollbackAsync(TestContext.Current.CancellationToken);

            Assert.Equal(0, cn.LastTransaction.RollbackAsyncCount);
            Assert.Null(helper.CurrentTransaction);
        }

        [Fact]
        public void Dispose_SkipsDbRollbackWhenTransactionAlreadyCompleted()
        {
            var cn = new FakeDbConnection();
            var helper = new TransactionHelper(cn);
            var tx = helper.BeginTransaction();
            _ = helper.CurrentTransaction;
            cn.LastTransaction.IsCompleted = true;

            tx.Dispose();

            Assert.Equal(0, cn.LastTransaction.RollbackCount);
            Assert.Equal(1, cn.CloseCount);
            Assert.Null(helper.CurrentTransaction);
        }
    }
}
