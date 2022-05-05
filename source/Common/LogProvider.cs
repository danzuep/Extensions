using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.Extensions.DependencyInjection;

namespace Zue.Common
{
    public static class LogProvider
    {
        private static IDictionary<string, ILogger> _loggers =
            new Dictionary<string, ILogger>();

        private static ILoggerFactory? _loggerFactory;
        internal static ILoggerFactory Factory
        {
            get
            {
                if (_loggerFactory is null)
                {
                    ILoggerProvider loggerProvider = new DebugLoggerProvider();
                    var loggerProviders = new ILoggerProvider[] { loggerProvider };
                    _loggerFactory = new LoggerFactory(loggerProviders);
                }
                return _loggerFactory;
            }
            set
            {
                if (_loggerFactory is null && value != null)
                {
                    _loggerFactory = value;
                    _loggers.Clear();
                }
            }
        }

        public static void SetLoggerFactory(this IServiceProvider serviceProvider) =>
            Factory = serviceProvider.GetRequiredService<ILoggerFactory>();

        private static ILogger CreateLogger(string name) => Factory.CreateLogger(name);

        public static ILogger GetLogger<T>() => GetLogger(typeof(T).Name);

        public static ILogger GetLogger(string category)
        {
            if (!_loggers.ContainsKey(category))
                _loggers[category] = CreateLogger(category);
            return _loggers[category];
        }

        public static void Dispose()
        {
            _loggerFactory?.Dispose();
            _loggers.Clear();
        }
    }
}
