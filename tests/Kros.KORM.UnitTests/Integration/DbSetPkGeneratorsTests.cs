using FluentAssertions;
using Kros.Data;
using Kros.KORM.Metadata;
using Kros.KORM.Metadata.Attribute;
using Kros.KORM.Query;
using Kros.KORM.UnitTests.Base;
using System;
using Xunit;

namespace Kros.KORM.UnitTests.Integration
{
    public class DbSetPkGeneratorsTests : DatabaseTestBase
    {
        #region Helpers

        private const string AbGenerator = "AbGenerator";
        private const string TestTableNameA = "TestTableA";
        private const string TestTableNameB = "TestTableB";

        private static readonly string CreateTable_A =
$@"CREATE TABLE [dbo].[{TestTableNameA}] (
    [IdA] [int] NOT NULL,
    [Name] [nvarchar](50) NULL
) ON [PRIMARY];";

        private static readonly string CreateTable_B =
$@"CREATE TABLE [dbo].[{TestTableNameB}] (
    [IdB] [int] NOT NULL,
    [Name] [nvarchar](50) NULL
) ON [PRIMARY];";

        [Alias(TestTableNameA)]
        public class PersonA
        {
            [Key(autoIncrementMethodType: AutoIncrementMethodType.Custom, generatorName: AbGenerator)]
            public int IdA { get; set; }
            public string Name { get; set; }
        }

        [Alias(TestTableNameB)]
        public class PersonB
        {
            [Alias("IdB")]
            [Key(autoIncrementMethodType: AutoIncrementMethodType.Custom, generatorName: AbGenerator)]
            public int Pk { get; set; }
            public string Name { get; set; }
        }

        [Alias(TestTableNameA)]
        public class PersonMain
        {
            [Key(autoIncrementMethodType: AutoIncrementMethodType.Custom)]
            public int IdA { get; set; }
            public string Name { get; set; }
        }

        [Alias(TestTableNameB)]
        public class PersonTemp
        {
            [Key(autoIncrementMethodType: AutoIncrementMethodType.Custom, generatorName: TestTableNameA)]
            public int IdB { get; set; }
            public string Name { get; set; }
        }

        public class PersonInvalid
        {
            [Key(autoIncrementMethodType: AutoIncrementMethodType.Identity, generatorName: TestTableNameA)]
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private TestDatabase CreateTestDatabase()
        {
            TestDatabase db = CreateDatabase(new[] { CreateTable_A, CreateTable_B });
            foreach (IIdGenerator idGenerator in IdGeneratorFactories.GetGeneratorsForDatabaseInit(db.Connection))
            {
                idGenerator.InitDatabaseForIdGenerator();
            }
            return db;
        }

        private static void InsertItems<T>(IDbSet<T> set, params T[] items)
        {
            foreach (T item in items)
            {
                set.Add(item);
            }
            set.CommitChanges();
        }

        #endregion

        [Fact]
        public void GeneratorNameIsAllowedOnlyWithCustomAutoIncrement()
        {
            using TestDatabase db = CreateTestDatabase();
            Action action = () => { IDbSet<PersonInvalid> setA = db.Query<PersonInvalid>().AsDbSet(); };
            action.Should().Throw<InvalidOperationException>().WithMessage("*Custom*");
        }

        [Fact]
        public void DifferentTablesUseSamePkGenerator_InsertSingleItem()
        {
            using TestDatabase db = CreateTestDatabase();

            IDbSet<PersonA> setA = db.Query<PersonA>().AsDbSet();
            var personA1 = new PersonA { Name = "Alice A" };
            var personA2 = new PersonA { Name = "Bob A" };
            var personA3 = new PersonA { Name = "Connor A" };

            IDbSet<PersonB> setB = db.Query<PersonB>().AsDbSet();
            var personB1 = new PersonB { Name = "Alice B" };
            var personB2 = new PersonB { Name = "Bob B" };
            var personB3 = new PersonB { Name = "Connor B" };

            InsertItems(setA, personA1);
            InsertItems(setB, personB1);
            InsertItems(setA, personA2);
            InsertItems(setB, personB2);
            InsertItems(setA, personA3);
            InsertItems(setB, personB3);

            personA1.IdA.Should().Be(1);
            personA2.IdA.Should().Be(3);
            personA3.IdA.Should().Be(5);
            personB1.Pk.Should().Be(2);
            personB2.Pk.Should().Be(4);
            personB3.Pk.Should().Be(6);
        }

        [Fact]
        public void DifferentTablesUseSamePkGenerator_InsertMultipleItems()
        {
            using TestDatabase db = CreateTestDatabase();

            IDbSet<PersonA> setA = db.Query<PersonA>().AsDbSet();
            var personA1 = new PersonA { Name = "Alice A" };
            var personA2 = new PersonA { Name = "Bob A" };
            var personA3 = new PersonA { Name = "Connor A" };
            var personA4 = new PersonA { Name = "Drew A" };
            var personA5 = new PersonA { Name = "Eve A" };
            var personA6 = new PersonA { Name = "Fiona A" };

            IDbSet<PersonB> setB = db.Query<PersonB>().AsDbSet();
            var personB1 = new PersonB { Name = "Alice B" };
            var personB2 = new PersonB { Name = "Bob B" };
            var personB3 = new PersonB { Name = "Connor B" };
            var personB4 = new PersonB { Name = "Drew B" };
            var personB5 = new PersonB { Name = "Eve B" };
            var personB6 = new PersonB { Name = "Fiona B" };

            InsertItems(setA, new[] { personA1, personA2, personA3 });
            InsertItems(setB, new[] { personB1, personB2, personB3 });
            InsertItems(setA, new[] { personA4, personA5, personA6 });
            InsertItems(setB, new[] { personB4, personB5, personB6 });

            personA1.IdA.Should().Be(1);
            personA2.IdA.Should().Be(2);
            personA3.IdA.Should().Be(3);
            personA4.IdA.Should().Be(7);
            personA5.IdA.Should().Be(8);
            personA6.IdA.Should().Be(9);

            personB1.Pk.Should().Be(4);
            personB2.Pk.Should().Be(5);
            personB3.Pk.Should().Be(6);
            personB4.Pk.Should().Be(10);
            personB5.Pk.Should().Be(11);
            personB6.Pk.Should().Be(12);
        }

        [Fact]
        public void DifferentTablesUseSamePkGenerator_DefaultGeneratorInMainTable()
        {
            using TestDatabase db = CreateTestDatabase();

            IDbSet<PersonMain> setMain = db.Query<PersonMain>().AsDbSet();
            var personMain1 = new PersonMain { Name = "Alice Main" };
            var personMain2 = new PersonMain { Name = "Bob Main" };
            var personMain3 = new PersonMain { Name = "Connor Main" };

            IDbSet<PersonTemp> setTemp = db.Query<PersonTemp>().AsDbSet();
            var personTemp1 = new PersonTemp { Name = "Alice Temp" };
            var personTemp2 = new PersonTemp { Name = "Bob Temp" };
            var personTemp3 = new PersonTemp { Name = "Connor Temp" };

            InsertItems(setMain, personMain1);
            InsertItems(setTemp, personTemp1);
            InsertItems(setMain, personMain2);
            InsertItems(setTemp, personTemp2);
            InsertItems(setMain, personMain3);
            InsertItems(setTemp, personTemp3);

            personMain1.IdA.Should().Be(1);
            personMain2.IdA.Should().Be(3);
            personMain3.IdA.Should().Be(5);
            personTemp1.IdB.Should().Be(2);
            personTemp2.IdB.Should().Be(4);
            personTemp3.IdB.Should().Be(6);
        }
    }
}
