using Kros.Data.BulkActions;
using Kros.KORM.CommandGenerator;
using Kros.KORM.Metadata;
using Kros.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Kros.KORM.Data
{
    internal class KormDataReader<T> : IBulkActionDataReader
    {
        private IEnumerable<T> _data;
        private ICommandGenerator<T> _generator;
        private List<ColumnInfo> _columns;

        public KormDataReader(IEnumerable<T> data, ICommandGenerator<T> generator)
        {
            Check.NotNull(data, nameof(data));
            Check.NotNull(generator, nameof(generator));

            _data = data;
            DataEnumerator = _data.GetEnumerator();
            _generator = generator;
            _columns = _generator.GetQueryColumns().ToList();
        }

        protected IEnumerator<T> DataEnumerator { get; private set; }

        public int FieldCount
        {
            get {
                return _columns.Count();
            }
        }

        public string GetName(int i)
        {
            return _columns[i].Name;
        }

        public int GetOrdinal(string name)
        {
            return _columns.IndexOf(_columns.First((item) => item.Name == name));
        }

        public virtual object GetValue(int i)
        {
            return _generator.GetColumnValue(_columns[i], DataEnumerator.Current);
        }

        public string GetString(int i) => (string)GetValue(i);

        public bool IsDBNull(int i)
        {
            object value = GetValue(i);
            return (value == null) || (value == System.DBNull.Value);
        }

        public bool Read()
        {
            return DataEnumerator.MoveNext();
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    DataEnumerator.Dispose();
                    DataEnumerator = null;
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
