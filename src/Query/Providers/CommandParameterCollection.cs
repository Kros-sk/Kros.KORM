using System;
using System.Collections.ObjectModel;
using System.Data;

namespace Kros.KORM.Query
{
    /// <summary>
    /// Zoznam parametrov pre pre SQL príkaz (<see autoUpgrade="true" cref="IQueryProvider.ExecuteNonQuery(System.String)"/>),
    /// alebo stored procedúru (<see autoUpgrade="true" cref="IQueryProvider.ExecuteStoredProcedure(System.String)"/>).
    /// </summary>
    public class CommandParameterCollection
        : KeyedCollection<string, CommandParameter>
    {
        /// <summary>
        /// Vytvorí inštanciu triedy. V názvoch parametrov, ktoré predstavujú kľúč do slovníka, nezáleží na veľkosti písmen.
        /// </summary>
        public CommandParameterCollection() : base(StringComparer.OrdinalIgnoreCase) { }

        /// <summary>
        /// Pridá do zoznamu nový parameter so zadanými hodnotami.
        /// </summary>
        /// <param name="parameterName">Meno parametra.</param>
        /// <param name="value">Hodnota parametra.</param>
        /// <returns>Pridaný parameter.</returns>
        public CommandParameter Add(string parameterName, object value)
        {
            var parameter = new CommandParameter(parameterName, value);
            Add(parameter);
            return parameter;
        }

        /// <summary>
        /// Pridá do zoznamu nový parameter so zadanými hodnotami.
        /// </summary>
        /// <param name="parameterName">Meno parametra.</param>
        /// <param name="value">Hodnota parametra.</param>
        /// <param name="direction">Druh parametra. Zmysel má len pri parametroch pre stored procedúry.</param>
        /// <returns>Pridaný parameter.</returns>
        public CommandParameter Add(string parameterName, object value, ParameterDirection direction)
        {
            var parameter = new CommandParameter(parameterName, value, direction);
            Add(parameter);
            return parameter;
        }

        /// <summary>
        /// Pridá do zoznamu nový parameter so zadanými hodnotami.
        /// </summary>
        /// <param name="parameterName">Meno parametra.</param>
        /// <param name="value">Hodnota parametra.</param>
        /// <param name="dataType">Dátový typ parametra.</param>
        /// <returns>Pridaný parameter.</returns>
        public CommandParameter Add(string parameterName, object value, DbType dataType)
        {
            var parameter = new CommandParameter(parameterName, value, dataType);
            Add(parameter);
            return parameter;
        }

        /// <summary>
        /// Pridá do zoznamu nový parameter so zadanými hodnotami.
        /// </summary>
        /// <param name="parameterName">Meno parametra.</param>
        /// <param name="value">Hodnota parametra.</param>
        /// <param name="dataType">Dátový typ parametra.</param>
        /// <param name="direction">Druh parametra. Zmysel má len pri parametroch pre stored procedúry.</param>
        /// <returns>Pridaný parameter.</returns>
        public CommandParameter Add(string parameterName, object value, DbType? dataType, ParameterDirection direction)
        {
            var parameter = new CommandParameter(parameterName, value, dataType, direction);
            Add(parameter);
            return parameter;
        }

        /// <summary>
        /// Vráti kľuč do slovníka, čo je meno parametra <see cref="CommandParameter.ParameterName"/>.
        /// </summary>
        /// <param name="item">Parameter, pre ktorý sa zíkava kľúč.</param>
        /// <returns>Reťazec - meno parametra.</returns>
        protected override string GetKeyForItem(CommandParameter item)
        {
            return item.ParameterName;
        }
    }
}