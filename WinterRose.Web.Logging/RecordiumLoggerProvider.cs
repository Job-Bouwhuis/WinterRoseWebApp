using Microsoft.Extensions.Logging;
using WinterRose.Recordium;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace WinterRose.Web.Logging;

public sealed class RecordiumLoggerProvider : ILoggerProvider
{
    static RecordiumLoggerProvider() 
    {
        LogDestinations.AddDestination(new ConsoleLogDestination());
        LogDestinations.AddDestination(new FileLogDestination());
    }

    public ILogger CreateLogger(string categoryName)
    {
        Type? type = Type.GetType(categoryName);

        string shortName =
            type != null
                ? type.Name
                : ShortCategory(categoryName);

        return new RecordiumLogger(shortName);
    }

    private static string ShortCategory(string categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            return categoryName;

        string[] parts = categoryName.Split('.');

        if (parts.Length == 1)
            return parts[0];

        if (parts.Length == 2)
            return categoryName;

        return parts[^1];
    }

    public void Dispose()
    {
    }
}

internal sealed class RecordiumLogger : ILogger
{
    private readonly Log logger;

    public RecordiumLogger(string category)
    {
        this.logger = Recordium.Log.GetLogger(category);
    }
    
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return new NoopDisposable();
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        string message = formatter(state, exception);

        LogSeverity severity = logLevel switch
        {
            LogLevel.Trace => LogSeverity.Debug,
            LogLevel.Debug => LogSeverity.Debug,
            LogLevel.Information => LogSeverity.Info,
            LogLevel.Warning => LogSeverity.Warning,
            LogLevel.Error => LogSeverity.Error,
            LogLevel.Critical => LogSeverity.Fatal,
            _ => LogSeverity.Info
        };

        var entry = logger.CreateEntry(severity, exception, message, null, 0);
        logger.Write(entry);
    }
}

public class RecordiumLogger<T> : ILogger<T>
{
    private readonly ILogger _inner;

    public RecordiumLogger(ILogger<T> inner)
    {
        _inner = inner;
    }

    public IDisposable? BeginScope<TState>(TState state) => _inner.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        string original = typeof(T).FullName ?? typeof(T).Name;
        string shortName = typeof(T).Name;

        // replace category via state formatting
        _inner.Log(logLevel, eventId, state, exception, formatter);
    }
}