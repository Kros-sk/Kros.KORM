using Kros.KORM.Migrations;
using NSubstitute;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kros.KORM.UnitTests.Migrations
{
    public class MigrationOptionsShould
    {
        [Fact]
        public async Task RegisterAfterMigrationActionWithoutCancellationToken()
        {
            var options = new MigrationOptions();
            IDatabase database = Substitute.For<IDatabase>();
            IDatabase receivedDatabase = null;
            long receivedId = 0;

#pragma warning disable CS0618 // the obsolete overload is the subject of this test
            options.AddAfterMigrationAction((db, id) =>
            {
                receivedDatabase = db;
                receivedId = id;
                return Task.CompletedTask;
            });
#pragma warning restore CS0618

            await options.Actions.Single()(database, 20250101001, TestContext.Current.CancellationToken);

            Assert.Same(database, receivedDatabase);
            Assert.Equal(20250101001, receivedId);
        }

        [Fact]
        public async Task PassCancellationTokenToAfterMigrationAction()
        {
            var options = new MigrationOptions();
            using var cts = new CancellationTokenSource();
            CancellationToken receivedToken = default;

            options.AddAfterMigrationAction((_, _, cancellationToken) =>
            {
                receivedToken = cancellationToken;
                return Task.CompletedTask;
            });

            await options.Actions.Single()(Substitute.For<IDatabase>(), 1, cts.Token);

            Assert.Equal(cts.Token, receivedToken);
        }
    }
}
