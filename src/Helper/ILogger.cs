using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Kros.KORM.Helper
{
    /// <summary>
    /// Interface, which describe logger.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs the command.
        /// </summary>
        /// <param name="command">The command.</param>
        void LogCommand(IDbCommand command);
    }
}
