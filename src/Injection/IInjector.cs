using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kros.KORM.Injection
{
    /// <summary>
    /// Interface, which describe service injector, which know get injected values.
    /// </summary>
    public interface IInjector
    {
        /// <summary>
        /// Get injected value for property.
        /// </summary>
        /// <param name="propertyName">Property, which want resolve.</param>
        /// <returns>
        /// Value for injection.
        /// </returns>
        object GetValue(string propertyName);

        /// <summary>
        /// Can by property injected?
        /// </summary>
        /// <param name="propertyName">Property name.</param>
        /// <returns><see langword="true"/> if can by injected, otherwise <see langword="false"/>.</returns>
        bool IsInjectable(string propertyName);
    }
}
