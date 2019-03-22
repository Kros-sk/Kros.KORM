using Kros.KORM.Properties;
using Kros.Utils;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Kros.KORM.Query
{
    /// <summary>
    /// Represent set of registered Query provider factories.
    /// </summary>
    /// <seealso cref="IQueryProviderFactory"/>
    public static class QueryProviderFactories
    {
        private static Dictionary<string, IQueryProviderFactory> _factoryByProviderName =
            new Dictionary<string, IQueryProviderFactory>(StringComparer.InvariantCultureIgnoreCase);
        private static Dictionary<Type, IQueryProviderFactory> _factoryByConnection = new Dictionary<Type, IQueryProviderFactory>();

        /// <summary>
        /// Gets the factory by provider name.
        /// </summary>
        /// <param name="providerName">Db provider name.</param>
        /// <returns>
        /// Instance of <seealso cref="IQueryProviderFactory"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">When factory for specific <paramref name="providerName"/>
        /// is not registered.</exception>
        public static IQueryProviderFactory GetFactory(string providerName)
        {
            Check.NotNullOrWhiteSpace(providerName, nameof(providerName));

            if (_factoryByProviderName.TryGetValue(providerName, out var factory))
            {
                return factory;
            }
            else
            {
                throw new InvalidOperationException(
                    string.Format(Resources.QueryProviderFactoryNotRegisteredForProvider, providerName));
            }
        }

        /// <summary>
        /// Gets the factory by connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>
        /// Instance of <seealso cref="IQueryProviderFactory" />.
        /// </returns>
        /// <exception cref="InvalidOperationException">When factory for specific <paramref name="connection"/>
        /// is not registered.</exception>
        public static IQueryProviderFactory GetFactory(DbConnection connection)
        {
            Check.NotNull(connection, nameof(connection));

            if (_factoryByConnection.TryGetValue(connection.GetType(), out var factory))
            {
                return factory;
            }
            else
            {
                throw new InvalidOperationException(
                    string.Format(Resources.QueryProviderFactoryNotRegisteredForConnection, connection.GetType().FullName));
            }
        }

        /// <summary>
        /// Registers the specified query provider factory.
        /// </summary>
        /// <typeparam name="TConnection">The type of the connection.</typeparam>
        /// <param name="providerName">Name of the provider.</param>
        /// <param name="queryProviderFactory">The query provider factory.</param>
        public static void Register<TConnection>(
            string providerName,
            IQueryProviderFactory queryProviderFactory) where TConnection : DbConnection
        {
            Check.NotNullOrWhiteSpace(providerName, nameof(providerName));
            Check.NotNull(queryProviderFactory, nameof(queryProviderFactory));

            _factoryByProviderName[providerName] = queryProviderFactory;
            _factoryByConnection[typeof(TConnection)] = queryProviderFactory;
        }

        internal static void UnRegisterAll()
        {
            _factoryByConnection.Clear();
            _factoryByProviderName.Clear();
        }
    }
}
