using FluentAssertions;
using Kros.Data;
using Kros.KORM.Metadata;
using Kros.KORM.Metadata.Attribute;
using Kros.KORM.UnitTests.Base;
using Xunit;

namespace Kros.KORM.UnitTests.Integration
{
    public class TriggerTests : DatabaseTestBase
    {
        #region Helpers

        private const string Table_AutoIncrementId = "AutoIncrementWithTrigger";
        private const string Table_ManualId = "ManualWithTrigger";

        private static readonly string Script_TableWithAutoIncrementId =
@"CREATE TABLE [dbo].[{0}] (
    [Id] [int] IDENTITY(1, 1) NOT NULL,
    [Age] [int] NULL,
    [FirstName] [nvarchar](50) NULL,
    [AutoValue] [int] NULL

    CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED ([Id])
) ON [PRIMARY]";

        private static readonly string Script_TableWithManualId =
@"CREATE TABLE [dbo].[{0}] (
    [Id] [int] NOT NULL,
    [Age] [int] NULL,
    [FirstName] [nvarchar](50) NULL,
    [AutoValue] [int] NULL

    CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED ([Id])
) ON [PRIMARY]";

        private static readonly string Script_InsertTrigger =
@"CREATE OR ALTER TRIGGER Table_{0}_InsertTrigger
    ON [dbo].[{0}]
    AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [dbo].[{0}]
    SET [AutoValue] = ins.Age * 10
    FROM INSERTED AS ins
    INNER JOIN [dbo].[{0}] AS tab ON tab.Id = ins.Id AND ins.AutoValue IS NULL
END";

        private static readonly string Script_UpdateTrigger =
@"CREATE OR ALTER TRIGGER Table_{0}_UpdateTrigger
    ON [dbo].[{0}]
    AFTER UPDATE
AS
IF NOT(UPDATE([AutoValue]))
BEGIN
    SET NOCOUNT ON;

    UPDATE [dbo].[{0}]
    SET [AutoValue] = ins.Age * 100
    FROM INSERTED AS ins
    INNER JOIN [dbo].[{0}] AS tab ON tab.Id = ins.Id
END";

        [Alias(Table_AutoIncrementId)]
        public class Data_AutoIncrementId_WithoutTriggerValue
        {
            [Key(AutoIncrementMethodType.Identity)]
            public int Id { get; set; }
            public int Age { get; set; }
            public string FirstName { get; set; }
        }

        [Alias(Table_AutoIncrementId)]
        public class Data_AutoIncrementId_WithTriggerValue
        {
            [Key(AutoIncrementMethodType.Identity)]
            public int Id { get; set; }
            public int Age { get; set; }
            public string FirstName { get; set; }
            public int? AutoValue { get; set; }
        }

        [Alias(Table_ManualId)]
        public class Data_ManualId_WithoutTriggerValue
        {
            [Key(AutoIncrementMethodType.Custom)]
            public int Id { get; set; }
            public int Age { get; set; }
            public string FirstName { get; set; }
        }

        [Alias(Table_ManualId)]
        public class Data_ManualId_WithTriggerValue
        {
            [Key(AutoIncrementMethodType.Custom)]
            public int Id { get; set; }
            public int Age { get; set; }
            public string FirstName { get; set; }
            public int? AutoValue { get; set; }
        }

        private TestDatabase CreateTestDatabase()
        {
            string[] scripts = {
                string.Format(Script_TableWithAutoIncrementId, Table_AutoIncrementId),
                string.Format(Script_InsertTrigger, Table_AutoIncrementId),
                string.Format(Script_UpdateTrigger, Table_AutoIncrementId),
                string.Format(Script_TableWithManualId, Table_ManualId),
                string.Format(Script_InsertTrigger, Table_ManualId),
                string.Format(Script_UpdateTrigger, Table_ManualId),
            };

            TestDatabase db = CreateDatabase(scripts);
            foreach (IIdGenerator idGenerator in IdGeneratorFactories.GetGeneratorsForDatabaseInit(db.Connection))
            {
                idGenerator.InitDatabaseForIdGenerator();
            }
            return db;
        }

        #endregion

        [Fact]
        public void ReturnCorrectAutoIncrementIdsAfterInsertTrigger_ValueSetByTrigger()
        {
            using TestDatabase korm = CreateTestDatabase();
            var dbSet = korm.Query<Data_AutoIncrementId_WithoutTriggerValue>().AsDbSet();

            var item1 = new Data_AutoIncrementId_WithoutTriggerValue { FirstName = "Lorem 10", Age = 10 };
            var item2 = new Data_AutoIncrementId_WithoutTriggerValue { FirstName = "Lorem 20", Age = 20 };

            dbSet.Add(item1);
            dbSet.Add(item2);
            dbSet.CommitChanges();

            item1.Id.Should().Be(1);
            item2.Id.Should().Be(2);

            foreach (var item in korm.Query<Data_AutoIncrementId_WithTriggerValue>())
            {
                item.AutoValue.Should().Be(item.Age * 10);
            }
        }

        [Fact]
        public void ReturnCorrectAutoIncrementIdsAfterInsertTrigger_ValueSetManually()
        {
            using TestDatabase korm = CreateTestDatabase();
            var dbSet = korm.Query<Data_AutoIncrementId_WithTriggerValue>().AsDbSet();

            var item1 = new Data_AutoIncrementId_WithTriggerValue { FirstName = "Lorem 10", Age = 10, AutoValue = -10 };
            var item2 = new Data_AutoIncrementId_WithTriggerValue { FirstName = "Lorem 20", Age = 20, AutoValue = -20 };

            dbSet.Add(item1);
            dbSet.Add(item2);
            dbSet.CommitChanges();

            item1.Id.Should().Be(1);
            item2.Id.Should().Be(2);

            foreach (var item in korm.Query<Data_AutoIncrementId_WithTriggerValue>())
            {
                item.AutoValue.Should().Be(item.Age * -1);
            }
        }

        [Fact]
        public void ReturnCorrectManualIdsAfterInsertTrigger_ValueSetByTrigger()
        {
            using TestDatabase korm = CreateTestDatabase();
            var dbSet = korm.Query<Data_ManualId_WithoutTriggerValue>().AsDbSet();

            var item1 = new Data_ManualId_WithoutTriggerValue { FirstName = "Lorem 10", Age = 10 };
            var item2 = new Data_ManualId_WithoutTriggerValue { FirstName = "Lorem 20", Age = 20 };

            dbSet.Add(item1);
            dbSet.Add(item2);
            dbSet.CommitChanges();

            item1.Id.Should().Be(1);
            item2.Id.Should().Be(2);

            foreach (var item in korm.Query<Data_ManualId_WithTriggerValue>())
            {
                item.AutoValue.Should().Be(item.Age * 10);
            }
        }

        [Fact]
        public void ReturnCorrectManualIdsAfterInsertTrigger_ValueSetManually()
        {
            using TestDatabase korm = CreateTestDatabase();
            var dbSet = korm.Query<Data_ManualId_WithTriggerValue>().AsDbSet();

            var item1 = new Data_ManualId_WithTriggerValue { FirstName = "Lorem 10", Age = 10, AutoValue = -10 };
            var item2 = new Data_ManualId_WithTriggerValue { FirstName = "Lorem 20", Age = 20, AutoValue = -20 };

            dbSet.Add(item1);
            dbSet.Add(item2);
            dbSet.CommitChanges();

            item1.Id.Should().Be(1);
            item2.Id.Should().Be(2);

            foreach (var item in korm.Query<Data_ManualId_WithTriggerValue>())
            {
                item.AutoValue.Should().Be(item.Age * -1);
            }
        }

        [Fact]
        public void TriggerMustSetVallueWhenNotSetManuallyOnUpdate()
        {
            using TestDatabase korm = CreateTestDatabase();
            var dbSet = korm.Query<Data_AutoIncrementId_WithoutTriggerValue>().AsDbSet();

            var item1 = new Data_AutoIncrementId_WithoutTriggerValue { FirstName = "Lorem 10", Age = 10 };
            var item2 = new Data_AutoIncrementId_WithoutTriggerValue { FirstName = "Lorem 20", Age = 20 };

            dbSet.Add(item1);
            dbSet.Add(item2);
            dbSet.CommitChanges();
            dbSet.Clear();

            item1.Age = item1.Id * 20;
            item2.Age = item2.Id * 20;
            dbSet.Edit(item1);
            dbSet.Edit(item2);
            dbSet.CommitChanges();

            foreach (var item in korm.Query<Data_AutoIncrementId_WithTriggerValue>())
            {
                item.Age.Should().Be(item.Id * 20);
                item.AutoValue.Should().Be(item.Age * 100);
            }
        }

        [Fact]
        public void TriggerMustNotSetVallueWhenSetManuallyOnUpdate()
        {
            using TestDatabase korm = CreateTestDatabase();
            var dbSet = korm.Query<Data_AutoIncrementId_WithTriggerValue>().AsDbSet();

            var item1 = new Data_AutoIncrementId_WithTriggerValue { FirstName = "Lorem 10", Age = 150, AutoValue = 111 };
            var item2 = new Data_AutoIncrementId_WithTriggerValue { FirstName = "Lorem 20", Age = 500, AutoValue = 222 };

            dbSet.Add(item1);
            dbSet.Add(item2);
            dbSet.CommitChanges();
            dbSet.Clear();

            item1.Age = item1.Id * 200;
            item2.Age = item2.Id * 200;
            item1.AutoValue = item1.Age / 2;
            item2.AutoValue = item2.Age / 2;
            dbSet.Edit(item1);
            dbSet.Edit(item2);
            dbSet.CommitChanges();

            foreach (var item in korm.Query<Data_AutoIncrementId_WithTriggerValue>())
            {
                item.Age.Should().Be(item.Id * 200);
                item.AutoValue.Should().Be(item.Age / 2);
            }
        }
    }
}
