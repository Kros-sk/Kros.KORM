using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Kros.KORM.UnitTests.Helper
{
    /// <summary>
    /// Fake IDataReader for unittesting
    /// </summary>
    /// <seealso cref="System.Data.IDataReader" />
    public class InMemoryDataReader : IDataReader
    {
        private IEnumerator<Dictionary<string, object>> _data;
        private List<string> _keys;
        private List<object> _values;
        private List<Type> _types;
        private IEnumerable<Dictionary<string, object>> _originData;
        private int _fieldCount = 0;

        public InMemoryDataReader(IEnumerable<Dictionary<string, object>> data)
        {
            _data = data.GetEnumerator();
            _originData = data;
            if (_originData.Count() > 0)
            {
                _keys = _originData.First().Keys.ToList();
                _types = _originData.First().Values.Select(p => p.GetType()).ToList();
                _fieldCount = _originData.First().Count;
            }
        }

        public List<object> CurrentValues { get { return _values; } }

        public List<Type> CurrentTypes { get { return _types; } }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public int Depth
        {
            get { throw new NotImplementedException(); }
        }

        public DataTable GetSchemaTable()
        {
            throw new NotImplementedException();
        }

        public bool IsClosed
        {
            get { throw new NotImplementedException(); }
        }

        public bool NextResult()
        {
            throw new NotImplementedException();
        }

        public bool Read()
        {
            bool ret = _data.MoveNext();

            if (ret)
            {
                _values = _data.Current.Values.ToList();
            }

            return ret;
        }

        public int RecordsAffected
        {
            get { throw new NotImplementedException(); }
        }

        public void Dispose()
        {
        }

        public int FieldCount
        {
            get { return _fieldCount; }
        }

        public bool GetBoolean(int i)
        {
            return (bool)this.GetValue(i);
        }

        public byte GetByte(int i)
        {
            return (byte)this.GetValue(i);
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public char GetChar(int i)
        {
            return (char)this.GetValue(i);
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public IDataReader GetData(int i)
        {
            throw new NotImplementedException();
        }

        public string GetDataTypeName(int i)
        {
            return _types[i].Name;
        }

        public DateTime GetDateTime(int i)
        {
            return (DateTime)this.GetValue(i);
        }

        public decimal GetDecimal(int i)
        {
            return (decimal)this.GetValue(i);
        }

        public double GetDouble(int i)
        {
            return (double)this.GetValue(i);
        }

        public Type GetFieldType(int i)
        {
            return _types[i];
        }

        public float GetFloat(int i)
        {
            return (float)this.GetValue(i);
        }

        public Guid GetGuid(int i)
        {
            return (Guid)this.GetValue(i);
        }

        public short GetInt16(int i)
        {
            return (short)this.GetValue(i);
        }

        public int GetInt32(int i)
        {
            return (int)this.GetValue(i);
        }

        public long GetInt64(int i)
        {
            return (long)this.GetValue(i);
        }

        public string GetName(int i)
        {
            return _keys[i];
        }

        public int GetOrdinal(string name)
        {
            return _keys.IndexOf(name);
        }

        public string GetString(int i)
        {
            return (string)this.GetValue(i);
        }

        public object GetValue(int i)
        {
            return _values[i];
        }

        public int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public bool IsDBNull(int i)
        {
            return this.GetValue(i) == null || this.GetValue(i) == DBNull.Value;
        }

        public object this[string name]
        {
            get { return _data.Current[name]; }
        }

        public object this[int i]
        {
            get { return this.GetValue(i); }
        }
    }
}
