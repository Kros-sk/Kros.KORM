using Kros.Data;
using Kros.KORM.CommandGenerator;
using Kros.KORM.Metadata;
using System.Collections.Generic;
using System.Linq;

namespace Kros.KORM.Data
{
    internal class KormBulkInsertDataReader<T> : KormDataReader<T>
    {
        private IIdGenerator _idGenerator;
        private readonly TableInfo _tableInfo;
        private readonly ColumnInfo _primaryKey;
        private readonly int _primaryKeyOrdinal = -1;

        public KormBulkInsertDataReader(IEnumerable<T> data,
            ICommandGenerator<T> generator,
            IIdGenerator idGenerator,
            TableInfo tableInfo)
            : base(data, generator)
        {
            _tableInfo = tableInfo;
            _idGenerator = idGenerator;

            if (_idGenerator is not null)
            {
                _primaryKey = _tableInfo.PrimaryKey.Single(p => p.AutoIncrementMethodType == AutoIncrementMethodType.Custom);
                _primaryKeyOrdinal = GetOrdinal(_primaryKey.Name);
            }
        }

        public override object GetValue(int i)
        {
            var value = base.GetValue(i);

            if (CanGenerateId(i, value))
            {
                return GenerateId();
            }
            else
            {
                return value;
            }
        }

        private object GenerateId()
        {
            var id = _idGenerator.GetNext();
            _primaryKey.SetValue(DataEnumerator.Current, id);

            return id;
        }

        private bool CanGenerateId(int i, object value)
            => (i == _primaryKeyOrdinal) && (value is null || value.Equals(_primaryKey.DefaultValue));

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _idGenerator?.Dispose();
            _idGenerator = null;
        }
    }
}
