using Kros.Data;
using Kros.KORM.CommandGenerator;
using Kros.KORM.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kros.KORM.Data
{
    internal class KormBulkInsertDataReader<T> : KormDataReader<T>
    {
        private IIdGenerator _idGenerator;
        private readonly TableInfo _tableInfo;
        private readonly Lazy<ColumnInfo> _primaryKey;

        public KormBulkInsertDataReader(IEnumerable<T> data,
            ICommandGenerator<T> generator,
            IIdGenerator idGenerator,
            TableInfo tableInfo)
            : base(data, generator)
        {
            _tableInfo = tableInfo;
            _idGenerator = idGenerator;

            _primaryKey = new Lazy<ColumnInfo>(() =>
            {
                return _tableInfo.PrimaryKey.Single(p => p.AutoIncrementMethodType == AutoIncrementMethodType.Custom);
            });
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

        private int GenerateId()
        {
            var id = _idGenerator.GetNext();
            _primaryKey.Value.SetValue(DataEnumerator.Current, id);

            return id;
        }

        private bool CanGenerateId(int i, object value) =>
            _idGenerator != null && IsPrimaryKey(i) && (int)value == 0;

        private bool IsPrimaryKey(int i) =>
            i == GetOrdinal(_primaryKey.Value.Name);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _idGenerator?.Dispose();
                _idGenerator = null;
            }
        }
    }
}
