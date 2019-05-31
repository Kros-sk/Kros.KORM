using Kros.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kros.KORM.Query.Sql
{
    internal class ParamEnumerator : IEnumerator<string>
    {
        private readonly string _sql;
        private string _current;
        private int _position = 0;
        private const string ParamPrefix = "@";

        public ParamEnumerator(string sql)
        {
            Check.NotNullOrWhiteSpace(sql, nameof(sql));
            _sql = sql.Replace(Environment.NewLine, " ");
            _sql = _sql.Replace("(", " ");
            _sql = _sql.Replace(")", " ");
        }

        public string Current => _current;

        public void Dispose()
        {
        }

        object System.Collections.IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_position < _sql.Length)
            {
                var start = _sql.IndexOf(ParamPrefix, _position);
                if (start > -1)
                {
                    var ends = new List<int>() {
                            _sql.IndexOf(" ", start) ,
                            _sql.IndexOf(",", start),
                            _sql.IndexOf(")", start)}.Where(p => p > -1);

                    var end = _sql.Length;

                    if (ends.Any())
                    {
                        end = ends.Min();
                    }

                    _current = _sql.Substring(start, end - start).TrimStart().TrimEnd();
                    _position = end;

                    return true;
                }

                return false;
            }

            return false;
        }

        public void Reset()
        {
            _position = 0;
        }
    }
}
