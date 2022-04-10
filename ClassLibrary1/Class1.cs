using System.Data;

namespace ClassLibrary1
{
    public class Class1
    {
        int _fieldIndex;
        IConverter _converter;

        public void Test(IDataReader reader, ColumnInfo target)
        {
            var data = new DataItem();

            data.Name = null;

            //if (reader.IsDBNull(_fieldIndex))
            //{
            //    target.Data = reader.GetValue(_fieldIndex);
            //}
            //else
            //{
            //    target.Data = null;
            //}

            //return _converter.Convert(value);
        }
    }
}
