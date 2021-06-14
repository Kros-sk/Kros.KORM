using Kros.Utils;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Delimiters.
    /// </summary>
    public class Delimiters
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Delimiters"/> class.
        /// </summary>
        /// <param name="opening">The opening.</param>
        /// <param name="closing">The closing.</param>
        public Delimiters(string opening, string closing)
        {
            Openin = Check.NotNull(opening, nameof(opening));
            Closing = Check.NotNull(closing, nameof(closing));
        }

        /// <summary>
        /// Gets the closing.
        /// </summary>
        public string Openin { get; }

        /// <summary>
        /// Gets the opening.
        /// </summary>
        public string Closing { get; }

        /// <summary>
        /// Quoteds the identifier.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        public string QuoteIdentifier(string identifier)
            => Openin + identifier + Closing;

        /// <summary>
        /// The square brackets.
        /// </summary>
        public static Delimiters SquareBrackets = new("[", "]");

        /// <summary>
        /// The empty.
        /// </summary>
        public static Delimiters Empty = new(string.Empty, string.Empty);

        /// <summary>
        /// Converts to string.
        /// </summary>
        public override string ToString() => $"{Openin} {Closing}";
    }
}
