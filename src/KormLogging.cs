using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kros.KORM;

/// <summary>
/// Support for logging in KORM.
/// </summary>
public static class KormLogging
{
    private static ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;

    /// <summary>
    /// Logger factory used in KORM for creating loggers. If no set, <see cref="NullLoggerFactory"/> is used.
    /// Set this factory either manually, or from DI container.
    /// </summary>
    public static ILoggerFactory LoggerFactory
    {
        get => _loggerFactory;
        set => _loggerFactory = value is null ? NullLoggerFactory.Instance : value;
    }

    /// <summary>
    /// Log level for logging executed SQL commands in <see cref="Kros.KORM.Query.QueryProvider"/>.
    /// Default value is <see cref="LogLevel.Information"/>.
    /// </summary>
    public static LogLevel SqlCommandLogLevel { get; set; } = LogLevel.Information;

    internal static ILogger<T> CreateLogger<T>() => LoggerFactory.CreateLogger<T>();
}
