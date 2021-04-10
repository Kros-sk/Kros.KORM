using BenchmarkDotNet.Attributes;
using Microsoft.Data.SqlClient;
using MMLib.RapidPrototyping.Generators;
using MMLib.RapidPrototyping.Models;
using System;
using System.Data;

namespace Kros.KORM.PerformanceTests
{
    public class MaterializeToClassVsRecordTest
    {
        private readonly DataTable _table;
        private readonly IDatabase _database;

        public MaterializeToClassVsRecordTest()
        {
            _database = new Database(new SqlConnection());
            _table = new DataTable();
            _table.Columns.Add("Id", typeof(int));
            _table.Columns.Add("FirstName", typeof(string));
            _table.Columns.Add("LastName", typeof(string));
            _table.Columns.Add("Salary", typeof(double));
            _table.Columns.Add("IsEmployed", typeof(bool));

            var pg = new PersonGenerator(22);
            var random = new Random(22);
            int id = 0;

            foreach (IPerson person in pg.Next(1000))
            {
                _table.Rows.Add(++id, person.FirstName, person.LastName, random.NextDouble() * 1000,
                    Convert.ToBoolean(random.Next(-1, 1)));
            }
        }

        [Benchmark]
        public void RecordTypes()
        {
            foreach (RecordType employee in _database.ModelBuilder.Materialize<RecordType>(_table))
            {
            }
        }

        [Benchmark]
        public void ClassTypes()
        {
            foreach (ClassType employee in _database.ModelBuilder.Materialize<ClassType>(_table))
            {
            }
        }

        public record RecordType(int Id, string FirstName, string LastName, double Salary, bool IsEmployed);

        public class ClassType
        {
            public int Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public double Salary { get; set; }
            public bool IsEmployed { get; set; }
        }
    }
}
