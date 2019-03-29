using Kros.KORM.Query;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Kros.KORM.Examples
{
    internal class ExecuteStoredProcedureExamples
    {
        #region Init

        public class Person
        {
            public int Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public DateTime BDay { get; set; }
        }

        private Database _database = new Database(new SqlConnection("connection string"));
        #endregion

        public void ExecuteStoredProcedureExample()
        {
            #region Examples

            // Stored procedure returns a scalar value.
            int intResult = _database.ExecuteStoredProcedure<int>("ProcedureName");
            DateTime dateResult = _database.ExecuteStoredProcedure<DateTime>("ProcedureName");

            // Stored procedure sets the value of output parameter.
            var parameters = new CommandParameterCollection();
            parameters.Add("@param1", 10);
            parameters.Add("@param2", DateTime.Now);
            parameters.Add("@outputParam", null, DbType.String, ParameterDirection.Output);

            _database.ExecuteStoredProcedure<string>("ProcedureName", parameters);

            Console.WriteLine(parameters["@outputParam"].Value);

            // Stored procedure returns complex object.
            Person person = _database.ExecuteStoredProcedure<Person>("ProcedureName");

            // Stored procedure returns list of complex objects.
            IEnumerable<Person> persons = _database.ExecuteStoredProcedure<IEnumerable<Person>>("ProcedureName");
            #endregion
        }
    }
}
