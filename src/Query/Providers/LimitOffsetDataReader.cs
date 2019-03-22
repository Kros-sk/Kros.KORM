using Kros.KORM.Properties;
using Kros.Utils;
using System;
using System.Data;

namespace Kros.KORM.Query.Providers
{
    /// <summary>
    /// Data reader over some other <see cref="IDataReader"/>. It can iterate just specified number of rows (<see cref="Limit"/>)
    /// and skip some rows at the begining (<see cref="Offset"/>).
    /// </summary>
    /// <remarks>
    /// So for example if inner reader has 20 rows (iterations) and <see cref="Limit"/> is set to 3 and <see cref="Offset"/>
    /// is set to 5, <see cref="LimitOffsetDataReader"/> will iterate just over rows 6, 7 and 8 (rows are counted from 1).
    /// So it will skip first 5 rows and returns just next 3 of them.
    /// </remarks>
    public class LimitOffsetDataReader : IDataReaderEnvelope
    {
        #region Fields

        private IDataReader _reader;
        private bool _offsetApplied;
        private int _readCount;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an instance wit specified <paramref name="limit"/>. <see cref="Offset"/> is set to 0.
        /// </summary>
        /// <param name="limit">Maximum number of rows returned. If value is 0, number of rows not limited.</param>
        public LimitOffsetDataReader(int limit)
            : this(limit, 0)
        {
        }

        /// <summary>
        /// Creates an instance wit specified <paramref name="limit"/> and <paramref name="offset"/>.
        /// </summary>
        /// <param name="limit">Maximum number of rows returned. If value is 0, number of rows not limited.</param>
        /// <param name="offset">Number of rows to skip from the begining.</param>
        public LimitOffsetDataReader(int limit, int offset)
        {
            Limit = Check.GreaterOrEqualThan(limit, 0, nameof(limit));
            Offset = Check.GreaterOrEqualThan(offset, 0, nameof(offset));
            _offsetApplied = Offset == 0;
        }

        #endregion

        #region Common

        /// <summary>
        /// Maximum number of rows returned. If value is 0, number of rows not limited.
        /// </summary>
        public int Limit { get; }

        /// <summary>
        /// Number of rows to skip from the begining.
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// Sets the inner reader, to which <see cref="Limit"/> and <see cref="Offset"/> are applied.
        /// Inner reader is closed when this reader is closed.
        /// </summary>
        /// <param name="innerReader">Inner reader.</param>
        /// <exception cref="ArgumentNullException">Value of <paramref name="innerReader"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Inner reader is already set.</exception>
        public void SetInnerReader(IDataReader innerReader)
        {
            if (_reader != null)
            {
                throw new InvalidOperationException(Resources.LimitOffsetDataReaderInnerReaderAlreadySet);
            }
            _reader = Check.NotNull(innerReader, nameof(innerReader));
        }

        #endregion

        #region IDataReader

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public object this[string name] => _reader[name];
        public object this[int i] => _reader[i];
        public int Depth => _reader.Depth;
        public int FieldCount => _reader.FieldCount;
        public bool IsClosed => _reader.IsClosed;
        public int RecordsAffected => _reader.RecordsAffected;
        public bool GetBoolean(int i) => _reader.GetBoolean(i);

        public byte GetByte(int i) => _reader.GetByte(i);
        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
            => _reader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);

        public char GetChar(int i) => _reader.GetChar(i);
        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
            => _reader.GetChars(i, fieldoffset, buffer, bufferoffset, length);

        public IDataReader GetData(int i) => _reader.GetData(i);
        public string GetDataTypeName(int i) => _reader.GetDataTypeName(i);
        public DateTime GetDateTime(int i) => _reader.GetDateTime(i);
        public decimal GetDecimal(int i) => _reader.GetDecimal(i);
        public double GetDouble(int i) => _reader.GetDouble(i);
        public Type GetFieldType(int i) => _reader.GetFieldType(i);
        public float GetFloat(int i) => _reader.GetFloat(i);
        public Guid GetGuid(int i) => _reader.GetGuid(i);
        public short GetInt16(int i) => _reader.GetInt16(i);
        public int GetInt32(int i) => _reader.GetInt32(i);
        public long GetInt64(int i) => _reader.GetInt64(i);
        public string GetName(int i) => _reader.GetName(i);
        public int GetOrdinal(string name) => _reader.GetOrdinal(name);
        public DataTable GetSchemaTable() => _reader.GetSchemaTable();
        public string GetString(int i) => _reader.GetString(i);
        public object GetValue(int i) => _reader.GetValue(i);
        public int GetValues(object[] values) => _reader.GetValues(values);
        public bool IsDBNull(int i) => _reader.IsDBNull(i);
        public bool NextResult() => throw new NotSupportedException();

        public bool Read()
        {
            if (!_offsetApplied)
            {
                for (var i = 0; i < Offset; i++)
                {
                    if (!_reader.Read())
                    {
                        return false;
                    }
                }
                _offsetApplied = true;
            }

            if ((Limit > 0) && (_readCount >= Limit))
            {
                return false;
            }
            _readCount++;
            return _reader.Read();
        }

        public void Close() => _reader?.Close();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        #endregion

        #region IDisposable Support

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public void Dispose() => Close();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        #endregion
    }
}
