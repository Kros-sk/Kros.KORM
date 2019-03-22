using Kros.Utils;
using System.Data;

namespace Kros.KORM.Query
{
    /// <summary>
    /// Parameter pre SQL príkaz (<see autoUpgrade="true" cref="IQueryProvider.ExecuteNonQuery(System.String)"/>),
    /// alebo stored procedúru (<see autoUpgrade="true" cref="IQueryProvider.ExecuteStoredProcedure(System.String)"/>).
    /// </summary>
    public class CommandParameter
    {
        /// <summary>
        /// Vytvorí nový parameter so zadanými hodnotami.
        /// </summary>
        /// <param name="parameterName">Meno parametra.</param>
        /// <param name="value">Hodnota parametra.</param>
        public CommandParameter(string parameterName, object value)
            : this(parameterName, value, null, ParameterDirection.Input)
        {
        }

        /// <summary>
        /// Vytvorí nový parameter so zadanými hodnotami.
        /// </summary>
        /// <param name="parameterName">Meno parametra.</param>
        /// <param name="value">Hodnota parametra.</param>
        /// <param name="direction">Druh parametra. Zmysel má len pri parametroch pre stored procedúry.</param>
        public CommandParameter(string parameterName, object value, ParameterDirection direction)
            : this(parameterName, value, null, direction)
        {
        }

        /// <summary>
        /// Vytvorí nový parameter so zadanými hodnotami.
        /// </summary>
        /// <param name="parameterName">Meno parametra.</param>
        /// <param name="value">Hodnota parametra.</param>
        /// <param name="dataType">Dátový typ parametra.</param>
        public CommandParameter(string parameterName, object value, DbType dataType)
            : this(parameterName, value, dataType, ParameterDirection.Input)
        {
        }

        /// <summary>
        /// Vytvorí nový parameter so zadanými hodnotami.
        /// </summary>
        /// <param name="parameterName">Meno parametra.</param>
        /// <param name="value">Hodnota parametra.</param>
        /// <param name="dataType">Dátový typ parametra.</param>
        /// <param name="direction">Druh parametra. Zmysel má len pri parametroch pre stored procedúry.</param>
        public CommandParameter(string parameterName, object value, DbType? dataType, ParameterDirection direction)
        {
            Check.NotNullOrEmpty(parameterName, nameof(parameterName));

            ParameterName = parameterName;
            Value = value;
            DataType = dataType;
            Direction = direction;
        }

        /// <summary>
        /// Meno parametra.
        /// </summary>
        public string ParameterName { get; }

        /// <summary>
        /// Hodnota parametra. Ak je <c>NULL</c> (<see langword="null"/>, alebo <see cref="System.DBNull"/>),
        /// mal by byť nastavený presný dátový typ <see cref="DataType"/>.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Dátový typ parametra. Ak nie je nastavený, reálnemu databázovému parametru sa nastaví automaticky podľa typu hodnoty
        /// <see cref="Value"/>. Musí byť nastavený, ak <see cref="Value"/> je NULL.
        /// </summary>
        public DbType? DataType { get; set; } = null;

        /// <summary>
        /// Druh parametra: vstupný, výstupný, vstupno-výstupný, alebo návratová hodnota. Zmysel má len pri parametroch
        /// pre stored procedúry.
        /// </summary>
        public ParameterDirection Direction { get; set; } = ParameterDirection.Input;
    }
}
