using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kros.KORM.Helper;

namespace KORM.Test.Performance.Doc
{
    public class PropertyNameExample
    {
        public void GetPropertyName()
        {
            #region GetPropertyName
            var propName = PropertyName<Person>.GetPropertyName((p) => p.FirstName);
            #endregion
        }

        private class Person
        {
            public string FirstName { get; set; }
        }
    }
}
