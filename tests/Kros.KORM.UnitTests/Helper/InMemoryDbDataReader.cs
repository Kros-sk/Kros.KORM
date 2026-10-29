using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Kros.KORM.UnitTests.Helper
{
    /// <summary>
    /// Fake <see cref="DbDataReader"/> over <see cref="InMemoryDataReader"/>, for unit tests. It supports only asynchronous
    /// reading, so it can verify that rows are read with <see cref="ReadAsync(CancellationToken)"/>.
    /// </summary>
    internal class InMemoryDbDataReader : DbDataReader
    {
        private readonly InMemoryDataReader _reader;

        public InMemoryDbDataReader(IEnumerable<Dictionary<string, object>> data)
        {
            _reader = new InMemoryDataReader(data);
        }

        public int ReadAsyncCallCount { get; private set; }

        public bool IsDisposed { get; private set; }

        public override Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReadAsyncCallCount++;
            return Task.FromResult(_reader.Read());
        }

        public override bool Read() => throw new InvalidOperationException("Synchronous Read must not be used");

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                IsDisposed = true;
                _reader.Dispose();
            }
            base.Dispose(disposing);
        }

        public override object this[int ordinal] => _reader[ordinal];
        public override object this[string name] => _reader[name];
        public override int Depth => 0;
        public override int FieldCount => _reader.FieldCount;
        public override bool HasRows => true;
        public override bool IsClosed => IsDisposed;
        public override int RecordsAffected => -1;
        public override bool GetBoolean(int ordinal) => _reader.GetBoolean(ordinal);
        public override byte GetByte(int ordinal) => _reader.GetByte(ordinal);
        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
            => _reader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
        public override char GetChar(int ordinal) => _reader.GetChar(ordinal);
        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
            => _reader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
        public override string GetDataTypeName(int ordinal) => _reader.GetDataTypeName(ordinal);
        public override DateTime GetDateTime(int ordinal) => _reader.GetDateTime(ordinal);
        public override decimal GetDecimal(int ordinal) => _reader.GetDecimal(ordinal);
        public override double GetDouble(int ordinal) => _reader.GetDouble(ordinal);
        public override IEnumerator GetEnumerator() => throw new NotSupportedException();
        public override Type GetFieldType(int ordinal) => _reader.GetFieldType(ordinal);
        public override float GetFloat(int ordinal) => _reader.GetFloat(ordinal);
        public override Guid GetGuid(int ordinal) => _reader.GetGuid(ordinal);
        public override short GetInt16(int ordinal) => _reader.GetInt16(ordinal);
        public override int GetInt32(int ordinal) => _reader.GetInt32(ordinal);
        public override long GetInt64(int ordinal) => _reader.GetInt64(ordinal);
        public override string GetName(int ordinal) => _reader.GetName(ordinal);
        public override int GetOrdinal(string name) => _reader.GetOrdinal(name);
        public override string GetString(int ordinal) => _reader.GetString(ordinal);
        public override object GetValue(int ordinal) => _reader.GetValue(ordinal);
        public override int GetValues(object[] values) => _reader.GetValues(values);
        public override bool IsDBNull(int ordinal) => _reader.IsDBNull(ordinal);
        public override bool NextResult() => false;
    }
}
