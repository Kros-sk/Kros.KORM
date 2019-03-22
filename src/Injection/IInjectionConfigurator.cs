using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Kros.KORM.Injection
{
    /// <summary>
    /// Interface, which describe configurator, for configurate model property injection.
    /// </summary>    
    /// <typeparam name="TModel">Model type.</typeparam>
    public interface IInjectionConfigurator<TModel>
    {
        /// <summary>
        /// Fill model property with injector.
        /// </summary>
        /// <typeparam name="TValue">Property type.</typeparam>
        /// <param name="modelProperty">Expression for defined property for injection.</param>
        /// <param name="injector">Function which is used for injection value to property.</param>
        /// <returns>Configurator, for next configurations.</returns>
        IInjectionConfigurator<TModel> FillProperty<TValue>(
            Expression<Func<TModel, TValue>> modelProperty,
            Func<TValue> injector);
    }
}
