using Kros.Utils;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Naming quota.
    /// </summary>
    public class Quota
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Quota"/> class.
        /// </summary>
        /// <param name="starting">The starting.</param>
        /// <param name="ending">The ending.</param>
        public Quota(string starting, string ending)
        {
            Starting = Check.NotNull(starting, nameof(starting));
            Ending = Check.NotNull(ending, nameof(ending));
        }

        /// <summary>
        /// Gets the starting quota.
        /// </summary>
        public string Starting { get; }

        /// <summary>
        /// Gets the ending quota.
        /// </summary>
        public string Ending { get; }

        /// <summary>
        /// Quoteds the name.
        /// </summary>
        /// <param name="name">The name.</param>
        public string QuoteName(string name)
            => Starting + name + Ending;

        /// <summary>
        /// The square brackets.
        /// </summary>
        public static Quota SquareBrackets = new("[", "]");

        /// <summary>
        /// The empty.
        /// </summary>
        public static Quota Empty = new(string.Empty, string.Empty);

        /// <summary>
        /// Converts to string.
        /// </summary>
        public override string ToString() => $"{Starting} {Ending}";
    }
}
