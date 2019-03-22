using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Kros.KORM.Helper
{
    /// <summary>
    /// Logger for logging provider activities.
    /// </summary>
    /// <seealso cref="Kros.KORM.Helper.ILogger" />
    public class Logger : ILogger
    {
        /// <summary>
        /// Logs the command.
        /// </summary>
        /// <param name="command">The command.</param>
        public void LogCommand(IDbCommand command)
        {
            if (Database.Log != null)
            {
                Database.Log($"{DateTime.Now.ToString("hh.mm.ss.ffff")} - {command.CommandText}");
                if (command.Parameters.Count > 0)
                {
                    Database.Log($"  WITH PARAMETERS ({string.Join(", ", command.Parameters.Cast<IDbDataParameter>().Select(p => p.Value))})");
                }
        }
    }
}
}
