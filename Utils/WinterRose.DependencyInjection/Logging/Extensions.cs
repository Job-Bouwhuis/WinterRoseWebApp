using System.Runtime.CompilerServices;
using WinterRose.Recordium;

namespace WinterRose.DependancyInjection.Logging;

public static class Extensions
{
    extension(IServiceBuilder services)
    {
        public void AddLogging(string LogPath = "logs")
        {
            services.AddSingleton<ILogDestination, FileLogDestination>()
                .Configure<FileLogDestination>(f => f.FileDirectory = LogPath);

            services.AddSingleton<ILogDestination, ConsoleLogDestination>();

            services.AddSingleton<LogDestinationsHandler>();
            
            services.AddSingleton(typeof(ILogger<>), typeof(GenericLogger<>));
        }
    }
}

public interface ILogger<T> : ILogger;

file class LogDestinationsHandler
{
    private static volatile bool Handled = false;
    
    public LogDestinationsHandler(IServiceProvider services)
    {
        if (!Handled)
        {
            Handled = true;
            var destinations = services.ResolveAll<ILogDestination>();
            foreach (var destination in destinations)
                LogDestinations.AddDestination(destination);
        }
    }
}

file class GenericLogger<T> : ILogger<T>
{
    private Log log;
    
    public GenericLogger(LogDestinationsHandler destinations)
    {
        log = new Log(typeof(T).Name);
    }
    
    public void Dispose()
    {
        log.Dispose();
    }

    public string Category
    {
        get => log.Category;
        set => log.Category = value;
    }
    public void Write(LogEntry entry)
    {
        log.Write(entry);
    }

    public LogEntry CreateEntry(LogSeverity severity, string message, string fileName, int lineNumber)
    {
        return log.CreateEntry(severity, message, fileName, lineNumber);
    }

    public LogEntry CreateEntry(LogSeverity severity, Exception? ex, string message, string? fileName, int lineNumber)
    {
        return log.CreateEntry(severity, ex, message, fileName, lineNumber);
    }

    public Task WriteAsync(LogEntry entry)
    {
        return  log.WriteAsync(entry);
    }

    public void Debug(string message, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0)
    {
        log.Debug(message, fileName, lineNumber);
    }

    public void Debug(Exception ex, string message = "", [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0)
    {
        log.Debug(ex, message, fileName, lineNumber);
    }

    public Task DebugAsync(string message, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0)
    {
        return log.DebugAsync(message, fileName, lineNumber);
    }

    public void Info(string message, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0)
    {
        log.Info(message, fileName, lineNumber);
    }

    public Task InfoAsync(string message, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0)
    {
        return log.InfoAsync(message, fileName, lineNumber);
    }

    public void Warning(string message, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0)
    {
        log.Warning(message, fileName, lineNumber);
    }

    public void Warning(Exception ex, string message = "", [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0)
    {
        log.Warning(ex, message, fileName, lineNumber);
    }

    public Task WarningAsync(string message, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0)
    {
        return log.WarningAsync(message, fileName, lineNumber);
    }

    public void Error(string message, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0)
    {
        log.Error(message, fileName, lineNumber);
    }

    public void Error(Exception ex, string message = "", [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0)
    {
        log.Error(ex, message, fileName, lineNumber);
    }

    public Task ErrorAsync(string message, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0)
    {
        return log.ErrorAsync(message, fileName, lineNumber);
    }

    public void Critical(string message, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0)
    {
        log.Critical(message, fileName, lineNumber);
    }

    public void Critical(Exception ex, string message = "", [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0)
    {
        log.Critical(ex, message, fileName, lineNumber);
    }

    public Task CriticalAsync(string message, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0)
    {
        return log.CriticalAsync(message, fileName, lineNumber);
    }

    public void Fatal(string message, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0)
    {
        log.Fatal(message, fileName, lineNumber);
    }

    public void Fatal(Exception ex, string message = "", [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0)
    {
        log.Fatal(ex, message, fileName, lineNumber);
    }

    public Task FatalAsync(string message, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0)
    {
        return log.FatalAsync(message, fileName, lineNumber);
    }
}