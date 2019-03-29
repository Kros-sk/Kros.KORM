using System;
using System.Data;
using System.Data.Common;

namespace Kros.KORM.Tests.Performance.Doc
{
    internal class IModelBuilderExample
    {
        private void ModelBuilderExample()
        {
            DataTable dataTable = null;
            Database database = null;

            #region ModelBuilderExample
            var people = database.ModelBuilder.Materialize<Person>(dataTable);

            foreach (var person in people)
            {
                Console.WriteLine(person.FirstName);
            }
            #endregion
        }

        private void ModelBuilderDataTableExample()
        {
            DataTable dataTable = null;
            Database database = null;

            #region ModelBuilderDataTableExample
            var people = database.ModelBuilder.Materialize<Person>(dataTable);
            #endregion
        }

        private void ModelBuilderReaderExample()
        {
            DbDataReader reader = null;
            Database database = null;

            #region ModelBuilderReaderExample
            var people = database.ModelBuilder.Materialize<Person>(reader);
            #endregion
        }

        private void ModelBuilderDataRowExample()
        {
            DataRow dataRow = null;
            Database database = null;

            #region ModelBuilderDataRowExample
            var person = database.ModelBuilder.Materialize<Person>(dataRow);
            #endregion
        }

        private class Person
        {
            public string FirstName { get; set; }
        }
    }
}
