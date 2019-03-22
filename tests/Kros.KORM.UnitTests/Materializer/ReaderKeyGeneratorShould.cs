using FluentAssertions;
using Kros.KORM.Materializer;
using System;
using System.Collections.Generic;
using System.Data;
using Xunit;

namespace Kros.KORM.UnitTests.Materializer
{
    public class ReaderKeyGeneratorShould
    {
        [Fact]
        public void ReturnKeyForReader()
        {
            Reader reader = new Reader(CreateListOfColumns());
            ReaderKeyGenerator generator = new ReaderKeyGenerator();

            var expected = "SYSTEM.STRINGIDINT32FIRSTNAMESTRINGLASTNAMESTRING".GetHashCode();
            var actual = generator.GenerateKey<string>(reader);

            actual.Should().Be(expected);
        }

        [Fact]
        public void ReturnSameKeyForReadersWhitSameColumns()
        {
            Reader reader1 = new Reader(CreateListOfColumns());
            Reader reader2 = new Reader(CreateListOfColumns());
            ReaderKeyGenerator generator = new ReaderKeyGenerator();

            var key1 = generator.GenerateKey<string>(reader1);
            var key2 = generator.GenerateKey<string>(reader2);

            key1.Should().Be(key2);
        }

        [Fact]
        public void ReturnDifferentKeyForReadersWhitDifferentColumns()
        {
            Reader reader1 = new Reader(CreateListOfColumns());
            Reader reader2 = new Reader(new List<Tuple<string, Type>>() { new Tuple<string, Type>("FirstName", typeof(string)),
                new Tuple<string, Type>("Id", typeof(Int32)),
                new Tuple<string, Type>("LastName", typeof(string))});
            ReaderKeyGenerator generator = new ReaderKeyGenerator();

            var key1 = generator.GenerateKey<string>(reader1);
            var key2 = generator.GenerateKey<string>(reader2);

            key1.Should().NotBe(key2);
        }

        [Fact]
        public void ReturnSameKeyForReadersWhitSameColumnsWhenChangeLetterCase()
        {
            Reader reader1 = new Reader(CreateListOfColumns());
            Reader reader2 = new Reader(new List<Tuple<string, Type>>() { new Tuple<string, Type>("ID", typeof(Int32)),
                new Tuple<string, Type>("FIRSTNAME", typeof(string)),
                new Tuple<string, Type>("LastName", typeof(string))});
            ReaderKeyGenerator generator = new ReaderKeyGenerator();

            var key1 = generator.GenerateKey<Int32>(reader1);
            var key2 = generator.GenerateKey<Int32>(reader2);

            key1.Should().Be(key2);
        }

        [Fact]
        public void ReturnDifferentKeysForReadersWhitSameColumnsAndOtherDataTypes()
        {
            Reader reader1 = new Reader(CreateListOfColumns());
            Reader reader2 = new Reader(CreateListOfColumns());
            ReaderKeyGenerator generator = new ReaderKeyGenerator();

            var key1 = generator.GenerateKey<Int32>(reader1);
            var key2 = generator.GenerateKey<string>(reader2);

            key1.Should().NotBe(key2);
        }

        [Fact]
        public void ReturnDifferentKeysForReadersWithSameColumnsWhenTypesAreDifferent()
        {
            Reader reader1 = new Reader(CreateListOfColumns());
            Reader reader2 = new Reader(new List<Tuple<string, Type>>() { new Tuple<string, Type>("Id", typeof(double)),
                new Tuple<string, Type>("FirstName", typeof(string)),
                new Tuple<string, Type>("LastName", typeof(string))});
            ReaderKeyGenerator generator = new ReaderKeyGenerator();

            var key1 = generator.GenerateKey<Int32>(reader1);
            var key2 = generator.GenerateKey<Int32>(reader2);

            key1.Should().NotBe(key2);
        }

        private static List<Tuple<string, Type>> CreateListOfColumns()
        {
            return new List<Tuple<string, Type>>() { new Tuple<string, Type>("Id", typeof(Int32)),
                new Tuple<string, Type>("FirstName", typeof(string)),
                new Tuple<string, Type>("LastName", typeof(string))};
        }

        private class Reader : IDataReader
        {
            private List<Tuple<string, Type>> _columns;

            public Reader(List<Tuple<string, Type>> columns)
            {
                _columns = columns;
            }

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
                throw new NotImplementedException();
            }

            public int RecordsAffected
            {
                get { throw new NotImplementedException(); }
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public int FieldCount
            {
                get { return _columns.Count; }
            }

            public bool GetBoolean(int i)
            {
                throw new NotImplementedException();
            }

            public byte GetByte(int i)
            {
                throw new NotImplementedException();
            }

            public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
            {
                throw new NotImplementedException();
            }

            public char GetChar(int i)
            {
                throw new NotImplementedException();
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
                throw new NotImplementedException();
            }

            public DateTime GetDateTime(int i)
            {
                throw new NotImplementedException();
            }

            public decimal GetDecimal(int i)
            {
                throw new NotImplementedException();
            }

            public double GetDouble(int i)
            {
                throw new NotImplementedException();
            }

            public Type GetFieldType(int i)
            {
                return _columns[i].Item2;
            }

            public float GetFloat(int i)
            {
                throw new NotImplementedException();
            }

            public Guid GetGuid(int i)
            {
                throw new NotImplementedException();
            }

            public short GetInt16(int i)
            {
                throw new NotImplementedException();
            }

            public int GetInt32(int i)
            {
                throw new NotImplementedException();
            }

            public long GetInt64(int i)
            {
                throw new NotImplementedException();
            }

            public string GetName(int i)
            {
                return _columns[i].Item1;
            }

            public int GetOrdinal(string name)
            {
                throw new NotImplementedException();
            }

            public string GetString(int i)
            {
                throw new NotImplementedException();
            }

            public object GetValue(int i)
            {
                throw new NotImplementedException();
            }

            public int GetValues(object[] values)
            {
                throw new NotImplementedException();
            }

            public bool IsDBNull(int i)
            {
                throw new NotImplementedException();
            }

            public object this[string name]
            {
                get { throw new NotImplementedException(); }
            }

            public object this[int i]
            {
                get { throw new NotImplementedException(); }
            }
        }
    }
}
