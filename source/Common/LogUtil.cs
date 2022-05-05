using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Zue.Common
{
    public class LogUtil : ILog
    {
        private Lazy<ILogger> _logger =
            new Lazy<ILogger>(NullLogger.Instance);
        public ILogger Logger
        {
            get => _logger.Value;
            set => _logger = new Lazy<ILogger>(
                value ?? NullLogger.Instance);
        }

        private LogUtil(string name) : base()
        {
            _logger = new Lazy<ILogger>(
                LogProvider.GetLogger(name));
        }

        public static ILog GetLogger<T>() => GetLogger(typeof(T).Name);

        public static ILog GetLogger(string name) => new LogUtil(name);

        public static void Log(string message, params string[] args)
        {
            var logger = LogProvider.GetLogger<LogUtil>();
            LogLevel logLevel = LogLevel.Debug;
            logger.Log(logLevel, message, args);
        }

        public static void Log(LogLevel logLevel = LogLevel.Debug, string message = "", params string[] args)
        {
            var logger = LogProvider.GetLogger<LogUtil>();
            logger.Log(logLevel, message, args);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Logger.Log(logLevel, eventId, state, exception, formatter);
        }

        public void LogTrace(string message)
        {
            Logger.LogTrace(message);
        }

        public void LogTrace(string message, params object[] args)
        {
            Logger.LogTrace(message, args);
        }

        public void LogTrace(Exception exception, string message)
        {
            Logger.LogTrace(exception, message);
        }

        public void LogTrace(Exception exception, string message, params object[] args)
        {
            Logger.LogTrace(exception, message, args);
        }

        public void LogDebug(string message)
        {
            Logger.LogDebug(message);
        }

        public void LogDebug(string message, params object[] args)
        {
            Logger.LogDebug(message, args);
        }

        public void LogDebug(Exception exception, string message)
        {
            Logger.LogDebug(exception, message);
        }

        public void LogDebug(Exception exception, string message, params object[] args)
        {
            Logger.LogDebug(exception, message, args);
        }

        public void LogInformation(string message)
        {
            Logger.LogInformation(message);
        }

        public void LogInformation(string message, params object[] args)
        {
            Logger.LogInformation(message, args);
        }

        public void LogInformation(Exception exception, string message)
        {
            Logger.LogInformation(exception, message);
        }

        public void LogInformation(Exception exception, string message, params object[] args)
        {
            Logger.LogInformation(exception, message, args);
        }

        public void LogWarning(string message)
        {
            Logger.LogWarning(message);
        }

        public void LogWarning(string message, params object[] args)
        {
            Logger.LogWarning(message, args);
        }

        public void LogWarning(Exception exception, string message)
        {
            Logger.LogWarning(exception, message);
        }

        public void LogWarning(Exception exception, string message, params object[] args)
        {
            Logger.LogWarning(exception, message, args);
        }

        public void LogError(string message)
        {
            Logger.LogError(message);
        }

        public void LogError(string message, params object[] args)
        {
            Logger.LogError(message, args);
        }

        public void LogError(Exception exception, string message)
        {
            Logger.LogError(exception, message);
        }

        public void LogError(Exception exception, string message, params object[] args)
        {
            Logger.LogError(exception, message, args);
        }

        public void LogCritical(string message)
        {
            Logger.LogCritical(message);
        }

        public void LogCritical(string message, params object[] args)
        {
            Logger.LogCritical(message, args);
        }

        public void LogCritical(Exception exception, string message)
        {
            Logger.LogCritical(exception, message);
        }

        public void LogCritical(Exception exception, string message, params object[] args)
        {
            Logger.LogCritical(exception, message, args);
        }

        public void Trace(string message)
        {
            Logger.LogTrace(message);
        }

        public void Trace(string message, params object[] args)
        {
            Logger.LogTrace(message, args);
        }

        public void Trace(Exception exception, string message)
        {
            Logger.LogTrace(exception, message);
        }

        public void Trace(Exception exception, string message, params object[] args)
        {
            Logger.LogTrace(exception, message, args);
        }

        public void Debug(string message)
        {
            Logger.LogDebug(message);
        }

        public void Debug(string message, params object[] args)
        {
            Logger.LogDebug(message, args);
        }

        public void Debug(Exception exception, string message)
        {
            Logger.LogDebug(exception, message);
        }

        public void Debug(Exception exception, string message, params object[] args)
        {
            Logger.LogDebug(exception, message, args);
        }

        public void Info(string message)
        {
            Logger.LogInformation(message);
        }

        public void Info(string message, params object[] args)
        {
            Logger.LogInformation(message, args);
        }

        public void Info(Exception exception, string message)
        {
            Logger.LogInformation(exception, message);
        }

        public void Info(Exception exception, string message, params object[] args)
        {
            Logger.LogInformation(exception, message, args);
        }

        public void Warn(string message)
        {
            Logger.LogWarning(message);
        }

        public void Warn(string message, params object[] args)
        {
            Logger.LogWarning(message, args);
        }

        public void Warn(Exception exception, string message)
        {
            Logger.LogWarning(exception, message);
        }

        public void Warn(Exception exception, string message, params object[] args)
        {
            Logger.LogWarning(exception, message, args);
        }

        public void Error(string message)
        {
            Logger.LogError(message);
        }

        public void Error(string message, params object[] args)
        {
            Logger.LogError(message, args);
        }

        public void Error(Exception exception, string message)
        {
            Logger.LogError(exception, message);
        }

        public void Error(Exception exception, string message, params object[] args)
        {
            Logger.LogError(exception, message, args);
        }

        public void Critical(string message)
        {
            Logger.LogCritical(message);
        }

        public void Critical(string message, params object[] args)
        {
            Logger.LogCritical(message, args);
        }

        public void Critical(Exception exception, string message)
        {
            Logger.LogCritical(exception, message);
        }

        public void Critical(Exception exception, string message, params object[] args)
        {
            Logger.LogCritical(exception, message, args);
        }

        public void Fatal(string message)
        {
            Logger.LogCritical(message);
        }

        public void Fatal(string message, params object[] args)
        {
            Logger.LogCritical(message, args);
        }

        public void Fatal(Exception exception, string message)
        {
            Logger.LogCritical(exception, message);
        }

        public void Fatal(Exception exception, string message, params object[] args)
        {
            Logger.LogCritical(exception, message, args);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return Logger.IsEnabled(logLevel);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return Logger.BeginScope(state);
        }
    }
}
