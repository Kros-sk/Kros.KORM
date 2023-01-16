﻿using FluentAssertions;
using Kros.KORM.Metadata;
using Kros.KORM.Metadata.Attribute;
using Kros.KORM.Query.Expressions;
using Kros.KORM.Query.Sql;
using System;
using System.Linq.Expressions;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Sql
{
    public class DefaultQuerySqlGeneratorShould
    {
        [Fact]
        public void GenerateSqlFromSqlExpression()
        {
            var originSql = @"Select Id, FirstName, LastName
                              FROM Person as p join Avatar as a on (p.Id = a.PersonId)
                              Where Id > @1".Replace(Environment.NewLine, string.Empty);
            var expression = new SqlExpression(originSql, 10);
            var generator = CreateQuerySqlGenerator();

            var queryInfo = generator.GenerateSql(expression);

            queryInfo.Query.Should().BeEquivalentTo(originSql);
        }

        [Fact]
        public void GenerateSqlWithSpecificColumns()
        {
            var expression = new SelectExpression(GetTableInfo());
            expression.SetColumnsExpression(new ColumnsExpression("Id, FirstName"));

            var generator = CreateQuerySqlGenerator();

            var queryInfo = generator.GenerateSql(expression);

            queryInfo.Query.Should().Be("SELECT Id, FirstName FROM TPerson");
        }

        [Fact]
        public void GenerateSqlWithDefaultColumns()
        {
            var expression = new SelectExpression(GetTableInfo());

            var generator = CreateQuerySqlGenerator();

            var queryInfo = generator.GenerateSql(expression);

            queryInfo.Query.Should().Be("SELECT Id, Name, LastName FROM TPerson");
        }

        [Fact]
        public void GenerateSqlWithJoin()
        {
            var expression = new SelectExpression(GetTableInfo());
            expression.SetColumnsExpression(new ColumnsExpression("Id, FirstName"));
            expression.SetTableExpression(new TableExpression("TPerson as p join Avatar as a on (p.Id = a.PersonId)"));

            var generator = CreateQuerySqlGenerator();

            var queryInfo = generator.GenerateSql(expression);

            queryInfo.Query.Should().Be("SELECT Id, FirstName FROM TPerson as p join Avatar as a on (p.Id = a.PersonId)");
        }

        [Fact]
        public void GenerateSqlWithTOP11()
        {
            var expression = new SelectExpression(GetTableInfo());
            expression.SetColumnsExpression(new ColumnsExpression("TOP 1 1"));
            expression.SetTableExpression(new TableExpression("TPerson"));

            var generator = CreateQuerySqlGenerator();

            var queryInfo = generator.GenerateSql(expression);

            queryInfo.Query.Should().Be("SELECT TOP 1 1 FROM TPerson");
        }

        [Fact]
        public void GenerateComplexSql()
        {
            var expression = new SelectExpression(GetTableInfo());
            expression.SetColumnsExpression(new ColumnsExpression("Id, FirstName"));
            expression.SetTableExpression(new TableExpression("TPerson as p join Avatar as a on (p.Id = a.PersonId)"));
            expression.SetWhereExpression(new WhereExpression("p.Id > @1 and p.Age > @2", 0, 18));
            expression.SetOrderByExpression(new OrderByExpression("FirstName, LastName desc"));

            var generator = CreateQuerySqlGenerator();

            var queryInfo = generator.GenerateSql(expression);

            queryInfo.Query.Should().Be(@"SELECT Id, FirstName FROM TPerson as p join Avatar as a on (p.Id = a.PersonId) " +
                "WHERE (p.Id > @1 and p.Age > @2) ORDER BY FirstName, LastName desc");
        }

        [Fact]
        public void GenerateSqlWithGroupBy()
        {
            var expression = new SelectExpression(GetTableInfo());
            expression.SetColumnsExpression(new ColumnsExpression("LastName, Min(Age)"));
            expression.SetTableExpression(new TableExpression("TPerson"));
            expression.SetGroupByExpression(new GroupByExpression("group by LastName"));

            var generator = CreateQuerySqlGenerator();

            var queryInfo = generator.GenerateSql(expression);

            queryInfo.Query.Should().Be(@"SELECT LastName, Min(Age) FROM TPerson GROUP BY LastName");
        }

        [Fact]
        public void GenerateWhereCondition()
        {
            DefaultQuerySqlGenerator generator = CreateQuerySqlGenerator();
            Expression<Func<Person, bool>> where = (p) => p.Id == 1 && p.FirstName.StartsWith("M");

            WhereExpression whereExpression = generator.GenerateWhereCondition(where);

            whereExpression.Sql.Should().Be("((Id = @1) AND (Name LIKE @2 + '%'))");
            whereExpression.Parameters.Should().BeEquivalentTo(new object[] { 1, "M" });
        }

        [Fact]
        public void GenerateWhereConditionWithSpecificParameterName()
        {
            DefaultQuerySqlGenerator generator = CreateQuerySqlGenerator();
            Expression<Func<Person, bool>> where = (p) => p.Id == 1 && p.FirstName.StartsWith("M");

            WhereExpression whereExpression = generator.GenerateWhereCondition(where, "Filter");

            whereExpression.Sql.Should().Be("((Id = @Filter1) AND (Name LIKE @Filter2 + '%'))");
        }

        [Fact]
        public void GenerateWhereConditionWithStringMethodsUsed()
        {
            DefaultQuerySqlGenerator generator = CreateQuerySqlGenerator();
            Expression<Func<Person, bool>> where = (p)
                => p.FirstName.ToUpper().EndsWith("M")
                && p.LastName.Trim().Substring(1, 2) == "er"
                && string.Compare(p.FirstName, p.LastName) == 1;

            WhereExpression whereExpression = generator.GenerateWhereCondition(where);

            whereExpression.Sql.Should().Be("(((UPPER(Name) LIKE '%' + @1) " +
                "AND (SUBSTRING(RTRIM(LTRIM(LastName)), @2 + 1, @3) = @4)) " +
                "AND (CASE WHEN Name = LastName THEN 0 WHEN Name < LastName THEN -1 ELSE 1 END = @5))");
            whereExpression.Parameters.Should().BeEquivalentTo(new object[] { "M", 1, 2, "er", 1 });
        }

        private static DefaultQuerySqlGenerator CreateQuerySqlGenerator()
            => new DefaultQuerySqlGenerator(new DatabaseMapper(new ConventionModelMapper()));

        private TableInfo GetTableInfo()
        {
            var tableInfo = new DatabaseMapper(new ConventionModelMapper()).GetTableInfo<Person>();

            return tableInfo;
        }

        [Alias("TPerson")]
        public class Person
        {
            public int Id { get; set; }

            [Alias("Name")]
            public string FirstName { get; set; }

            public string LastName { get; set; }

            [NoMap]
            public int Age { get; set; }
        }
    }
}
