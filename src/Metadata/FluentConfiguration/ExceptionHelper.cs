using System;
using System.Runtime.CompilerServices;
using Kros.Extensions;

namespace Kros.KORM.Metadata.FluentConfiguration
{
    /// <summary>
    /// Internal exception helper for fluent confuguration.
    /// </summary>
    internal static class ExceptionHelper
    {
        /// <summary>
        /// Throw exception if same method is call multiple time.
        /// </summary>
        /// <param name="check">Function checks if method is called multiple times.</param>
        /// <param name="methodName">Method name.</param>
        public static void CheckMultipleTimeCalls(Func<bool> check, [CallerMemberName] string methodName = null)
        {
            if (check())
            {
                throw new InvalidOperationException(Properties.Resources.CannotCallMultipleTime.Format(methodName));
            }
        }
    }
}
