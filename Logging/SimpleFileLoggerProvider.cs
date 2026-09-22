using Microsoft.Extensions.Logging;

namespace GoodMorningGFN.Logging;

/// <summary>
/// Minimaler Datei-Logger, der parallel zur Konsole nach logs/goodmorning-{yyyyMMdd}.log schreibt.
/// Bewusst ohne externe Logging-Bibliothek (Serilog o. Ä.) gehalten, um die Abhängigkeitsliste
/// dieser Etappe klein zu halten — reicht für Autostart-Diagnose (Task Scheduler zeigt kein Fenster).
/// </summary>
public sealed class SimpleFileLoggerProvider : ILoggerProvider
{
    private readonly string _logFilePath;
    private readonly object _writeLock = new();

    public SimpleFileLoggerProvider(string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);
        _logFilePath = Path.Combine(logDirectory, $"goodmorning-{DateTime.Now:yyyyMMdd}.log");
    }

    public ILogger CreateLogger(string categoryName) => new SimpleFileLogger(categoryName, _logFilePath, _writeLock);

    public void Dispose()
    {
    }

    private sealed class SimpleFileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string _logFilePath;
        private readonly object _writeLock;

        public SimpleFileLogger(string categoryName, string logFilePath, object writeLock)
        {
            _categoryName = categoryName;
            _logFilePath = logFilePath;
            _writeLock = writeLock;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {_categoryName}: {formatter(state, exception)}";
            if (exception != null)
            {
                line += Environment.NewLine + exception;
            }

            lock (_writeLock)
            {
                File.AppendAllText(_logFilePath, line + Environment.NewLine);
            }
        }
    }
}
