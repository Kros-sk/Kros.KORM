using Kros.Utils;
using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Kros.KORM.Data
{
    /// <summary>
    /// The <see cref="DataRowReader" /> obtains the contents of one <see cref="T:System.Data.DataRow" />
    /// object in the form of one read-only, forward-only result set.
    /// </summary>
    [ExcludeFromCodeCoverage()]
    internal class DataRowReader : DbDataReader
    {
        private DataTable _dataTable;
        private DataRow _dataRow;
        private bool _isOpen = true;
        private bool _reachEORows = false;
        private DataTable schemaTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataRowReader"/> class.
        /// </summary>
        /// <param name="dataRow">Data row of the table.</param>
        public DataRowReader(DataRow dataRow)
        {
            Check.NotNull(dataRow, nameof(dataRow));

            _dataRow = dataRow;
            _dataTable = _dataRow.Table;
        }

        /// <summary>
        /// Gets the value of the specified column in its native format given the column name.
        /// </summary>
        /// <returns>The value of the specified column in its native format.</returns>
        /// <param name="name">The name of the column. </param>
        /// <exception cref="T:System.ArgumentException">
        /// The name specified is not a valid column name.
        /// </exception>
        public override object this[string name]
        {
            get
            {
                if (_dataRow.RowState == DataRowState.Deleted)
                {
                    throw new ArgumentOutOfRangeException("ordinal");
                }
                return _dataRow[name];
            }
        }

        /// <summary>
        /// Gets the value of the specified column in its native format given the column ordinal.
        /// </summary>
        /// <returns>The value of the specified column in its native format.</returns>
        /// <param name="ordinal">The zero-based column ordinal. </param>
        /// <exception cref="T:System.IndexOutOfRangeException">
        /// The index passed was outside the range of 0 to <see cref="P:System.Data.DataTableReader.FieldCount" /> - 1.
        /// </exception>
        public override object this[int ordinal]
        {
            get
            {
                if (_dataRow.RowState != DataRowState.Deleted)
                {
                    try
                    {
                        return _dataRow[ordinal];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new ArgumentOutOfRangeException("ordinal");
                    }
                }
                throw new InvalidOperationException("Cannot process deleted row.");
            }
        }

        /// <summary>
        /// The depth of nesting for the current row of the <see cref="DataRowReader" />.
        /// </summary>
        /// <returns>
        /// The depth of nesting for the current row; always zero.
        /// </returns>
        public override int Depth
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Returns the number of columns in the current row.
        /// </summary>
        /// <returns>
        /// The number of columns in the current row.
        /// </returns>
        public override int FieldCount
        {
            get
            {
                return _dataTable.Columns.Count;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="DataRowReader" /> contains valid row.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the <see cref="DataRowReader" /> contains valid row.
        /// Property always returns <see langword="true"/>.
        /// </returns>
        public override bool HasRows
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="DataRowReader" /> is closed.
        /// </summary>
        /// <returns>
        /// Returns <see langword="true"/> if the <see cref="DataRowReader" /> is closed,
        /// otherwise returns <see langword="false"/>.
        /// </returns>
        public override bool IsClosed
        {
            get
            {
                return !_isOpen;
            }
        }

        /// <summary>
        /// Gets the number of rows inserted, changed, or deleted by execution of the SQL statement.
        /// </summary>
        /// <returns>
        /// The <see cref="DataRowReader" /> does not support this property and always returns 0.
        /// </returns>
        public override int RecordsAffected
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Closes the current <see cref="DataRowReader" />.
        /// </summary>
        public override void Close()
        {
            if (!_isOpen)
            {
                return;
            }

            _isOpen = false;
        }

        /// <summary>
        /// Gets the value of the specified column as a <see cref="T:System.Boolean" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The index passed was outside the range of 0 to <see cref="DataRowReader.FieldCount" /> - 1.
        /// </exception>
        public override bool GetBoolean(int ordinal)
        {
            try
            {
                return (bool)_dataRow[ordinal];
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException("ordinal");
            }
        }

        /// <summary>
        /// Gets the value of the specified column as a byte.
        /// </summary>
        /// <returns>The value of the specified column.</returns>
        /// <param name="ordinal">The zero-based column ordinal. </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The index passed was outside the range of 0 to <see cref="DataRowReader.FieldCount" /> - 1.
        /// </exception>
        public override byte GetByte(int ordinal)
        {
            try
            {
                return (byte)_dataRow[ordinal];
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException("ordinal");
            }
        }

        /// <summary>
        /// Reads a stream of bytes starting at the specified column offset into the buffer as an array starting at
        /// the specified buffer offset.
        /// </summary>
        /// <returns>The actual number of bytes read.</returns>
        /// <param name="ordinal">The zero-based column ordinal. </param>
        /// <param name="dataOffset">The index within the field from which to start the read operation. </param>
        /// <param name="buffer">The buffer into which to read the stream of bytes. </param>
        /// <param name="bufferOffset">The index within the buffer at which to start placing the data. </param>
        /// <param name="length">The maximum length to copy into the buffer. </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The index passed was outside the range of 0 to <see cref="DataRowReader.FieldCount" /> - 1.
        /// </exception>
        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            byte[] numArray;
            try
            {
                numArray = (byte[])_dataRow[ordinal];
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException("ordinal");
            }
            if (buffer == null)
                return (long)numArray.Length;
            int num1 = (int)dataOffset;
            int num2 = Math.Min(numArray.Length - num1, length);
            if (num1 < 0)
            {
                throw new ArgumentOutOfRangeException("dataOffset");
            }
            if (bufferOffset < 0 || bufferOffset > 0 && bufferOffset >= buffer.Length)
            {
                throw new ArgumentOutOfRangeException("bufferOffset");
            }
            if (0 < num2)
            {
                Array.Copy((Array)numArray, dataOffset, (Array)buffer, (long)bufferOffset, (long)num2);
            }
            else
            {
                if (length < 0)
                {
                    throw new ArgumentOutOfRangeException("length");
                }
                num2 = 0;
            }
            return (long)num2;
        }

        /// <summary>
        /// Gets the value of the specified column as a character.
        /// </summary>
        /// <returns>The value of the column.</returns>
        /// <param name="ordinal">The zero-based column ordinal. </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The index passed was outside the range of 0 to <see cref="DataRowReader.FieldCount" /> - 1.
        /// </exception>
        public override char GetChar(int ordinal)
        {
            try
            {
                return (char)_dataRow[ordinal];
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException("ordinal");
            }
        }

        /// <summary>
        /// Returns the value of the specified column as a character array.
        /// </summary>
        /// <returns>The actual number of characters read.</returns>
        /// <param name="ordinal">The zero-based column ordinal. </param>
        /// <param name="dataOffset">The index within the field from which to start the read operation. </param>
        /// <param name="buffer">The buffer into which to read the stream of chars. </param>
        /// <param name="bufferOffset">The index within the buffer at which to start placing the data. </param>
        /// <param name="length">The maximum length to copy into the buffer. </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The index passed was outside the range of 0 to <see cref="DataRowReader.FieldCount" /> - 1.
        /// </exception>
        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            char[] chArray;
            try
            {
                chArray = (char[])_dataRow[ordinal];
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException("ordinal");
            }
            if (buffer == null)
                return (long)chArray.Length;
            int num1 = (int)dataOffset;
            int num2 = Math.Min(chArray.Length - num1, length);
            if (num1 < 0)
            {
                throw new ArgumentOutOfRangeException("dataOffset");
            }
            if (bufferOffset < 0 || bufferOffset > 0 && bufferOffset >= buffer.Length)
            {
                throw new ArgumentOutOfRangeException("bufferOffset");
            }
            if (0 < num2)
            {
                Array.Copy((Array)chArray, dataOffset, (Array)buffer, (long)bufferOffset, (long)num2);
            }
            else
            {
                if (length < 0)
                {
                    throw new ArgumentOutOfRangeException("length");
                }
                num2 = 0;
            }
            return (long)num2;
        }

        /// <summary>
        /// Gets a string representing the data type of the specified column.
        /// </summary>
        /// <returns>A string representing the column's data type.</returns>
        /// <param name="ordinal">The zero-based column ordinal. </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The index passed was outside the range of 0 to <see cref="DataRowReader.FieldCount" /> - 1.
        /// </exception>
        public override string GetDataTypeName(int ordinal)
        {
            return GetFieldType(ordinal).Name;
        }

        /// <summary>
        /// Gets the value of the specified column as a <see cref="T:System.DateTime" /> object.
        /// </summary>
        /// <returns>The value of the specified column.</returns>
        /// <param name="ordinal">The zero-based column ordinal. </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The index passed was outside the range of 0 to <see cref="DataRowReader.FieldCount" /> - 1.
        /// </exception>
        public override DateTime GetDateTime(int ordinal)
        {
            try
            {
                return (DateTime)_dataRow[ordinal];
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException("ordinal");
            }
        }

        /// <summary>
        /// Gets the value of the specified column as a <see cref="T:System.Decimal" />.
        /// </summary>
        /// <returns>The value of the specified column.</returns>
        /// <param name="ordinal">The zero-based column ordinal. </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The index passed was outside the range of 0 to <see cref="DataRowReader.FieldCount" /> - 1.
        /// </exception>
        public override decimal GetDecimal(int ordinal)
        {
            try
            {
                return (decimal)_dataRow[ordinal];
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException("ordinal");
            }
        }

        /// <summary>
        /// Gets the value of the column as a double-precision floating point number.
        /// </summary>
        /// <returns>The value of the specified column.</returns>
        /// <param name="ordinal">The zero-based ordinal of the column. </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The index passed was outside the range of 0 to <see cref="DataRowReader.FieldCount" /> - 1.
        /// </exception>
        public override double GetDouble(int ordinal)
        {
            try
            {
                return (double)_dataRow[ordinal];
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException("ordinal");
            }
        }

        /// <summary>
        /// Returns an enumerator that can be used to iterate through the item collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that represents the item collection.</returns>
        /// <exception cref="T:System.InvalidOperationException">
        /// An attempt was made to read or access a column in a closed <see cref="DataRowReader" />.
        /// </exception>
        public override IEnumerator GetEnumerator()
        {
            return (IEnumerator)new DbEnumerator((IDataReader)this);
        }

        /// <summary>
        /// Gets the <see cref="T:System.Type" /> that is the data type of the object.
        /// </summary>
        /// <returns>The <see cref="T:System.Type" /> that is the data type of the object.</returns>
        /// <param name="ordinal">The zero-based column ordinal. </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The index passed was outside the range of 0 to <see cref="DataRowReader.FieldCount" /> - 1.
        /// </exception>
        public override Type GetFieldType(int ordinal)
        {
            try
            {
                return _dataTable.Columns[ordinal].DataType;
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException("ordinal");
            }
        }

        /// <summary>
        /// Gets the value of the specified column as a single-precision floating point number.
        /// </summary>
        /// <returns>The value of the column.</returns>
        /// <param name="ordinal">The zero-based column ordinal. </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The index passed was outside the range of 0 to <see cref="DataRowReader.FieldCount" /> - 1.
        /// </exception>
        public override float GetFloat(int ordinal)
        {
            try
            {
                return (float)_dataRow[ordinal];
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException("ordinal");
            }
        }

        /// <summary>
        /// Gets the value of the specified column as a globally-unique identifier (GUID).
        /// </summary>
        /// <returns>The value of the specified column.</returns>
        /// <param name="ordinal">The zero-based column ordinal. </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The index passed was outside the range of 0 to <see cref="DataRowReader.FieldCount" /> - 1.
        /// </exception>
        public override Guid GetGuid(int ordinal)
        {
            try
            {
                return (Guid)_dataRow[ordinal];
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException("ordinal");
            }
        }

        /// <summary>
        /// Gets the value of the specified column as a 16-bit signed integer.
        /// </summary>
        /// <returns>The value of the specified column.</returns>
        /// <param name="ordinal">The zero-based column ordinal </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The index passed was outside the range of 0 to <see cref="DataRowReader.FieldCount" /> - 1.
        /// </exception>
        public override short GetInt16(int ordinal)
        {
            try
            {
                return (short)_dataRow[ordinal];
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException("ordinal");
            }
        }

        /// <summary>
        /// Gets the value of the specified column as a 32-bit signed integer.
        /// </summary>
        /// <returns>The value of the specified column.</returns>
        /// <param name="ordinal">The zero-based column ordinal </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The index passed was outside the range of 0 to <see cref="DataRowReader.FieldCount" /> - 1.
        /// </exception>
        public override int GetInt32(int ordinal)
        {
            try
            {
                return (int)_dataRow[ordinal];
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException("ordinal");
            }
        }

        /// <summary>
        /// Gets the value of the specified column as a 64-bit signed integer.
        /// </summary>
        /// <returns>The value of the specified column.</returns>
        /// <param name="ordinal">The zero-based column ordinal </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The index passed was outside the range of 0 to <see cref="DataRowReader.FieldCount" /> - 1.
        /// </exception>
        public override long GetInt64(int ordinal)
        {
            try
            {
                return (long)_dataRow[ordinal];
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException("ordinal");
            }
        }

        /// <summary>
        /// Gets the value of the specified column as a <see cref="T:System.String" />.
        /// </summary>
        /// <returns>The name of the specified column.</returns>
        /// <param name="ordinal">The zero-based column ordinal </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The index passed was outside the range of 0 to <see cref="DataRowReader.FieldCount" /> - 1.
        /// </exception>
        public override string GetName(int ordinal)
        {
            try
            {
                return _dataTable.Columns[ordinal].ColumnName;
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException("ordinal");
            }
        }

        /// <summary>
        /// Gets the column ordinal, given the name of the column.
        /// </summary>
        /// <returns>The zero-based column ordinal.</returns>
        /// <param name="name">The name of the column. </param>
        /// <exception cref="NotSupportedException">The name specified is not a valid column name.</exception>
        public override int GetOrdinal(string name)
        {
            DataColumn dataColumn = _dataTable.Columns[name];
            if (dataColumn != null)
            {
                return dataColumn.Ordinal;
            }
            throw new NotSupportedException($"Column '{name}' is not in the table '{_dataTable.TableName}'.");
        }

        /// <summary>
        /// Returns a <see cref="T:System.Data.DataTable" /> that describes the column metadata of the <see cref="DataRowReader" />.
        /// </summary>
        /// <returns>A <see cref="T:System.Data.DataTable" /> that describes the column metadata.</returns>
        /// <exception cref="T:System.InvalidOperationException">
        /// The <see cref="T:System.Data.DataTableReader" /> is closed.
        /// </exception>
        public override DataTable GetSchemaTable()
        {
            if (this.schemaTable == null)
            {
                this.schemaTable = DataRowReader.GetSchemaTableFromDataTable(_dataTable);
            }
            return this.schemaTable;
        }

        /// <summary>
        /// Gets the value of the specified column as a string.
        /// </summary>
        /// <returns>The value of the specified column.</returns>
        /// <param name="ordinal">The zero-based column ordinal </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The index passed was outside the range of 0 to <see cref="DataRowReader.FieldCount" /> - 1.
        /// </exception>
        public override string GetString(int ordinal)
        {
            try
            {
                return (string)_dataRow[ordinal];
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException("ordinal");
            }
        }

        /// <summary>
        /// Gets the value of the specified column in its native format.
        /// </summary>
        /// <returns>The value of the specified column. This method returns DBNull for null columns.</returns>
        /// <param name="ordinal">The zero-based column ordinal </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The index passed was outside the range of 0 to <see cref="DataRowReader.FieldCount" /> - 1.
        /// </exception>
        public override object GetValue(int ordinal)
        {
            try
            {
                return _dataRow[ordinal];
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException("ordinal");
            }
        }

        /// <summary>
        /// Populates an array of objects with the column values of the current row.
        /// </summary>
        /// <returns>The number of column values copied into the array.</returns>
        /// <param name="values">An array of <see cref="T:System.Object" /> into which to copy the column values from the <see cref="DataRowReader" />.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="values"/> cannot be null.
        /// </exception>
        public override int GetValues(object[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            Array.Copy((Array)_dataRow.ItemArray,
                (Array)values, _dataRow.ItemArray.Length > values.Length ? values.Length : _dataRow.ItemArray.Length);
            if (_dataRow.ItemArray.Length <= values.Length)
            {
                return _dataRow.ItemArray.Length;
            }
            return values.Length;
        }

        /// <summary>
        /// Gets a value that indicates whether the column contains non-existent or missing values.
        /// </summary>
        /// <returns><see langword="true"/> if the specified column value is equivalent to <see cref="T:System.DBNull" />,
        /// otherwise <see langword="false"/>.</returns>
        /// <param name="ordinal">The zero-based column ordinal </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The index passed was outside the range of 0 to <see cref="DataRowReader.FieldCount" /> - 1.
        /// </exception>
        public override bool IsDBNull(int ordinal)
        {
            try
            {
                return _dataRow.IsNull(ordinal);
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException("ordinal");
            }
        }

        /// <summary>
        /// Advances the <see cref="DataRowReader" /> to the next result set, if any.
        /// </summary>
        /// <returns><see langword="true"/> if there was another result set, otherwise <see langword="false"/>.</returns>
        public override bool NextResult()
        {
            return false;
        }

        /// <summary>
        /// Advances the <see cref="DataRowReader" /> to the next record.
        /// </summary>
        /// <returns><see langword="true"/> if there was another row to read, otherwise <see langword="false"/>.</returns>
        public override bool Read()
        {
            if (!_reachEORows)
            {
                _reachEORows = true;
                return true;
            }
            return false;
        }

        internal static DataTable GetSchemaTableFromDataTable(DataTable table)
        {
            if (table == null)
            {
                throw new ArgumentNullException("DataTable");
            }
            return new DataTableReader(table).GetSchemaTable();
        }
    }
}
