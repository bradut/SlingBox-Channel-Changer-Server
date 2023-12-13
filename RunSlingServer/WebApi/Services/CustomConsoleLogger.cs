using Application.Interfaces;

namespace RunSlingServer.WebApi.Services
{
    /// <summary>
    /// Custom Logger that writes to the console via the shared ConsoleDisplayDispatcher
    /// </summary>
    public class CustomConsoleLogger : ILogger
    {
        private readonly IConsoleDisplayDispatcher _console;

        public CustomConsoleLogger(IConsoleDisplayDispatcher console)
        {
            _console = console;
        }

        // Logger name is often used to filter loggers.
        public string Name => "CustomConsoleLogger";

        // This method is called by .NET Core logger to check if a log level is enabled.
        // Here I just enable all events, but it could be modified to filter events.
        public bool IsEnabled(LogLevel level) => true;


        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception, string> formatter)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            if (!IsEnabled(level))
            {
                return;
            }

            var message = formatter(state, exception!);

            if (string.IsNullOrEmpty(message) && exception == null)
            {
                return;
            }

            _console.WriteLine($"{level}: {message}");
        }

        // BeginScope is called at the start of each logging scope -
        // for example, each HTTP request handling could be wrapped into a separate logging scope.
        // As this is not important in this case, just return null
        IDisposable? ILogger.BeginScope<TState>(TState state)
        {
            return null;
        }
    }
}
