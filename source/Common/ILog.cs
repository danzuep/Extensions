using System;
using Microsoft.Extensions.Logging;

namespace Zue.Common
{
    public interface ILog : ILogger
    {
        void Trace(string message);
        void Trace(string message, params object[] args);
        void Trace(Exception exception, string message);
        void Trace(Exception exception, string message, params object[] args);

        void Debug(string message);
        void Debug(string message, params object[] args);
        void Debug(Exception exception, string message);
        void Debug(Exception exception, string message, params object[] args);

        void Info(string message);
        void Info(string message, params object[] args);
        void Info(Exception exception, string message);
        void Info(Exception exception, string message, params object[] args);

        void Warn(string message);
        void Warn(string message, params object[] args);
        void Warn(Exception exception, string message);
        void Warn(Exception exception, string message, params object[] args);

        void Error(string message);
        void Error(string message, params object[] args);
        void Error(Exception exception, string message);
        void Error(Exception exception, string message, params object[] args);

        void Critical(string message);
        void Critical(string message, params object[] args);
        void Critical(Exception exception, string message);
        void Critical(Exception exception, string message, params object[] args);

        void Fatal(string message);
        void Fatal(string message, params object[] args);
        void Fatal(Exception exception, string message);
        void Fatal(Exception exception, string message, params object[] args);

        void LogTrace(string message);
        void LogTrace(string message, params object[] args);
        void LogTrace(Exception exception, string message);
        void LogTrace(Exception exception, string message, params object[] args);

        void LogDebug(string message);
        void LogDebug(string message, params object[] args);
        void LogDebug(Exception exception, string message);
        void LogDebug(Exception exception, string message, params object[] args);

        void LogInformation(string message);
        void LogInformation(string message, params object[] args);
        void LogInformation(Exception exception, string message);
        void LogInformation(Exception exception, string message, params object[] args);

        void LogWarning(string message);
        void LogWarning(string message, params object[] args);
        void LogWarning(Exception exception, string message);
        void LogWarning(Exception exception, string message, params object[] args);

        void LogError(string message);
        void LogError(string message, params object[] args);
        void LogError(Exception exception, string message);
        void LogError(Exception exception, string message, params object[] args);

        void LogCritical(string message);
        void LogCritical(string message, params object[] args);
        void LogCritical(Exception exception, string message);
        void LogCritical(Exception exception, string message, params object[] args);
    }
}
