using Kros.KORM.Data;
using Kros.KORM.Properties;
using Kros.KORM.Query.Providers;
using Kros.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kros.KORM.Materializer
{
    /// <summary>
    /// ModelBuilder, which know materialize data from Db to objects.
    /// </summary>
    /// <seealso cref="Kros.KORM.Materializer.IModelBuilder" />
    public class ModelBuilder : IModelBuilder
    {
        #region Nested classes

        internal class QueryDataReader
            : IDataReader
        {
            #region Fields

            private IDbCommand _command;
            private IDataReader _reader;
            private readonly bool _closeConnectionWhenFinished;

            #endregion

            #region Constructor

            public QueryDataReader(IDbCommand command, IDataReaderEnvelope readerEnvelope, bool closeConnectionWhenFinished)
                : this(
                    command,
                    WrapReader(Check.NotNull(command, nameof(command)).ExecuteReader(), readerEnvelope),
                    closeConnectionWhenFinished)
            {
            }

            private QueryDataReader(IDbCommand command, IDataReader executedReader, bool closeConnectionWhenFinished)
            {
                _command = command;
                _reader = executedReader;
                _closeConnectionWhenFinished = closeConnectionWhenFinished;
            }

            internal static async Task<QueryDataReader> CreateAsync(
                DbCommand command,
                IDataReaderEnvelope readerEnvelope,
                bool closeConnectionWhenFinished,
                CancellationToken cancellationToken)
            {
                Check.NotNull(command, nameof(command));
                DbDataReader executedReader = await command.ExecuteReaderAsync(cancellationToken);
                return new QueryDataReader(command, WrapReader(executedReader, readerEnvelope), closeConnectionWhenFinished);
            }

            private static IDataReader WrapReader(IDataReader executedReader, IDataReaderEnvelope readerEnvelope)
            {
                if (readerEnvelope == null)
                {
                    return executedReader;
                }
                readerEnvelope.SetInnerReader(executedReader);
                return readerEnvelope;
            }

            #endregion

            #region IDataReader

            public object this[string name] { get { return _reader[name]; } }
            public object this[int i] { get { return _reader[i]; } }
            public int Depth { get { return _reader.Depth; } }
            public int FieldCount { get { return _reader.FieldCount; } }
            public bool IsClosed { get { return _reader.IsClosed; } }
            public int RecordsAffected { get { return _reader.RecordsAffected; } }
            public bool GetBoolean(int i) { return _reader.GetBoolean(i); }

            public byte GetByte(int i) { return _reader.GetByte(i); }
            public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
            {
                return _reader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
            }

            public char GetChar(int i) { return _reader.GetChar(i); }
            public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
            {
                return _reader.GetChars(i, fieldoffset, buffer, bufferoffset, length);
            }

            public IDataReader GetData(int i) { return _reader.GetData(i); }
            public string GetDataTypeName(int i) { return _reader.GetDataTypeName(i); }
            public DateTime GetDateTime(int i) { return _reader.GetDateTime(i); }
            public decimal GetDecimal(int i) { return _reader.GetDecimal(i); }
            public double GetDouble(int i) { return _reader.GetDouble(i); }
            public Type GetFieldType(int i) { return _reader.GetFieldType(i); }
            public float GetFloat(int i) { return _reader.GetFloat(i); }
            public Guid GetGuid(int i) { return _reader.GetGuid(i); }
            public short GetInt16(int i) { return _reader.GetInt16(i); }
            public int GetInt32(int i) { return _reader.GetInt32(i); }
            public long GetInt64(int i) { return _reader.GetInt64(i); }
            public string GetName(int i) { return _reader.GetName(i); }
            public int GetOrdinal(string name) { return _reader.GetOrdinal(name); }
            public DataTable GetSchemaTable() { return _reader.GetSchemaTable(); }
            public string GetString(int i) { return _reader.GetString(i); }
            public object GetValue(int i) { return _reader.GetValue(i); }
            public int GetValues(object[] values) { return _reader.GetValues(values); }
            public bool IsDBNull(int i) { return _reader.IsDBNull(i); }
            public bool NextResult() { return _reader.NextResult(); }
            public bool Read() { return _reader.Read(); }
            public Task<bool> ReadAsync(CancellationToken cancellationToken) => _reader.ReadAsync(cancellationToken);

            public void Close()
            {
                IDbConnection connection = _command.Connection;
                _command.Cancel();
                _reader.Dispose();
                _reader = null;
                _command.Dispose();
                _command = null;
                if (_closeConnectionWhenFinished)
                {
                    connection.Close();
                }
            }

            #endregion

            #region IDisposable Support

            private bool disposedValue = false; // To detect redundant calls

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        Close();
                    }

                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
            }

            #endregion
        }

        /// <summary>
        /// Enumerable which support iteration over the materialized models.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <seealso cref="System.Collections.Generic.IEnumerable{T}" />
        public class ModelBuilderEnumerable<T>
            : IEnumerable<T>
        {
            #region Private fields

            private readonly Func<IDataReader, T> _modelFactory;
            private readonly IDataReader _reader;

            #endregion

            #region Constructor

            internal ModelBuilderEnumerable(
                Func<IDataReader, T> modelFactory,
                IDataReader reader)
            {
                Check.NotNull(modelFactory, nameof(modelFactory));
                Check.NotNull(reader, nameof(reader));
                _modelFactory = modelFactory;
                _reader = reader;
            }

            #endregion

            #region IEnumerator

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.Collections.Generic.IEnumerator`1" />
            /// that can be used to iterate through the collection.
            /// </returns>
            public IEnumerator<T> GetEnumerator()
            {
                return new ModelBuilderEnumerator<T>(_modelFactory, _reader);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion
        }

        /// <summary>
        ///  Enumerator that iterates through the materialized models.
        /// </summary>
        /// <typeparam name="T">Model Type</typeparam>
        /// <seealso cref="System.Collections.Generic.IEnumerator{T}" />
        public class ModelBuilderEnumerator<T>
            : IEnumerator<T>
        {
            #region Private fields

            private Func<IDataReader, T> _modelFactory;
            private IDataReader _reader;
            private T _currentItem = default;

            #endregion

            #region Constructor

            internal ModelBuilderEnumerator(
                Func<IDataReader, T> modelFactory,
                IDataReader reader)
            {
                Check.NotNull(modelFactory, nameof(modelFactory));
                Check.NotNull(reader, nameof(reader));

                _modelFactory = modelFactory;
                _reader = reader;
            }

            #endregion

            #region IEnumerator

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            public T Current => _currentItem;

            object IEnumerator.Current => _currentItem;

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// <see langword="true"/> if the enumerator was successfully advanced to the next element,
            /// <see langword="false"/> if the enumerator has passed the end of the collection.
            /// </returns>
            public bool MoveNext()
            {
                var result = _reader.Read();
                _currentItem = result ? _modelFactory(_reader) : default;
                return result;
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                if (_reader is QueryDataReader)
                {
                    _reader.Dispose();
                }
                _modelFactory = null;
                _reader = null;
                _currentItem = default;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="System.NotImplementedException"></exception>
            public void Reset()
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        /// <summary>
        /// Asynchronous enumerable which support iteration over the materialized models.
        /// It can be enumerated only once.
        /// </summary>
        /// <typeparam name="T">Model type.</typeparam>
        /// <seealso cref="System.Collections.Generic.IAsyncEnumerable{T}" />
        public class ModelBuilderAsyncEnumerable<T>
            : IAsyncEnumerable<T>
        {
            #region Private fields

            private readonly Func<IDataReader, T> _modelFactory;
            private readonly IDataReader _reader;
            private int _enumerated;

            #endregion

            #region Constructor

            internal ModelBuilderAsyncEnumerable(
                Func<IDataReader, T> modelFactory,
                IDataReader reader)
            {
                Check.NotNull(modelFactory, nameof(modelFactory));
                Check.NotNull(reader, nameof(reader));
                _modelFactory = modelFactory;
                _reader = reader;
            }

            #endregion

            #region IAsyncEnumerable

            /// <summary>
            /// Returns an enumerator that iterates asynchronously through the collection.
            /// </summary>
            /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
            /// <returns>
            /// A <see cref="IAsyncEnumerator{T}" /> that can be used to iterate asynchronously through the collection.
            /// </returns>
            /// <exception cref="InvalidOperationException">The sequence was already enumerated.</exception>
            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                if (Interlocked.Exchange(ref _enumerated, 1) == 1)
                {
                    throw new InvalidOperationException(Resources.ModelBuilderAsyncEnumerableAlreadyEnumerated);
                }
                return new ModelBuilderAsyncEnumerator<T>(_modelFactory, _reader, cancellationToken);
            }

            #endregion
        }

        /// <summary>
        /// Enumerator that iterates asynchronously through the materialized models.
        /// </summary>
        /// <typeparam name="T">Model type.</typeparam>
        /// <seealso cref="System.Collections.Generic.IAsyncEnumerator{T}" />
        public class ModelBuilderAsyncEnumerator<T>
            : IAsyncEnumerator<T>
        {
            #region Private fields

            private Func<IDataReader, T> _modelFactory;
            private IDataReader _reader;
            private readonly CancellationToken _cancellationToken;
            private T _currentItem = default;

            #endregion

            #region Constructor

            internal ModelBuilderAsyncEnumerator(
                Func<IDataReader, T> modelFactory,
                IDataReader reader,
                CancellationToken cancellationToken)
            {
                Check.NotNull(modelFactory, nameof(modelFactory));
                Check.NotNull(reader, nameof(reader));

                _modelFactory = modelFactory;
                _reader = reader;
                _cancellationToken = cancellationToken;
            }

            #endregion

            #region IAsyncEnumerator

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            public T Current => _currentItem;

            /// <summary>
            /// Advances the enumerator asynchronously to the next element of the collection.
            /// </summary>
            /// <returns>
            /// <see langword="true"/> if the enumerator was successfully advanced to the next element,
            /// <see langword="false"/> if the enumerator has passed the end of the collection.
            /// </returns>
            public async ValueTask<bool> MoveNextAsync()
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var result = await _reader.ReadAsync(_cancellationToken);
                _currentItem = result ? _modelFactory(_reader) : default;
                return result;
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
            /// asynchronously.
            /// </summary>
            /// <returns>A task that represents the asynchronous dispose operation.</returns>
            public ValueTask DisposeAsync()
            {
                if (_reader is QueryDataReader)
                {
                    _reader.Dispose();
                }
                _modelFactory = null;
                _reader = null;
                _currentItem = default;
                return ValueTask.CompletedTask;
            }

            #endregion
        }

        #endregion

        #region Private Fields

        private readonly IModelFactory _modelFactory;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelBuilder"/> class.
        /// </summary>
        /// <param name="modelFactory">The model factory.</param>
        /// <exception cref="ArgumentNullException">Argument 'modelFactory' is required.</exception>
        public ModelBuilder(IModelFactory modelFactory)
        {
            Check.NotNull(modelFactory, nameof(modelFactory));

            _modelFactory = modelFactory;
        }

        #endregion

        /// <summary>
        /// Materialize data from reader to instances of model type T.
        /// </summary>
        /// <typeparam name="T">Type of model.</typeparam>
        /// <param name="reader">The reader from which materialize data.</param>
        /// <returns>
        /// IEnumerable of models.
        /// </returns>
        /// <example>
        /// <code source="..\..\Documentation\Examples\Kros.KORM.Examples\IModelBuilderExample.cs"
        ///     title="Materialize data table"
        ///     region="ModelBuilderDataTableExample"
        ///     language="cs" />
        /// </example>
        /// <remarks>
        /// If disposeReader is <see langword="true"/> and connection is not null, then connection will be disposed.
        /// </remarks>
        public IEnumerable<T> Materialize<T>(IDataReader reader)
        {
            return new ModelBuilderEnumerable<T>(_modelFactory.GetFactory<T>(reader), reader);
        }

        /// <summary>
        /// Materialize data from reader to instances of model type <typeparamref name="T"/> asynchronously; rows are read
        /// with <see cref="DbDataReader.ReadAsync(CancellationToken)"/> when the reader supports it.
        /// </summary>
        /// <typeparam name="T">Type of model.</typeparam>
        /// <param name="reader">The reader from which materialize data.</param>
        /// <returns>
        /// Asynchronous sequence of models.
        /// </returns>
        /// <remarks>Doesn't call dispose over reader. The returned sequence can be enumerated only once.</remarks>
        public IAsyncEnumerable<T> MaterializeAsync<T>(IDataReader reader)
        {
            return new ModelBuilderAsyncEnumerable<T>(_modelFactory.GetFactory<T>(reader), reader);
        }

        /// <summary>
        /// Materialize data from data table to instances of model type T.
        /// </summary>
        /// <typeparam name="T">Type of model.</typeparam>
        /// <param name="table"></param>
        /// <returns>
        /// IEnumerable of models.
        /// </returns>
        public IEnumerable<T> Materialize<T>(DataTable table)
        {
            return Materialize<T>(new DataTableReader(table));
        }

        /// <summary>
        /// Materialize data from <paramref name="dataRow"/> to instances of model type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type of model.</typeparam>
        /// <param name="dataRow">Data row of the table.</param>
        /// <returns>
        /// Model.
        /// </returns>
        public T Materialize<T>(DataRow dataRow)
        {
            return Materialize<T>(new DataRowReader(dataRow)).FirstOrDefault();
        }
    }
}
