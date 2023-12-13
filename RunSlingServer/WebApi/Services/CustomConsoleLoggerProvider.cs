using Application.Interfaces;

namespace RunSlingServer.WebApi.Services
{
    /// <summary>
    /// Custom Logger Provider that writes to the console via the shared ConsoleDisplayDispatcher
    /// </summary>
    public class CustomConsoleLoggerProvider : ILoggerProvider
    {
        private readonly IConsoleDisplayDispatcher _consoleControlService;

        public CustomConsoleLoggerProvider(IConsoleDisplayDispatcher consoleControlService)
        {
            _consoleControlService = consoleControlService;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new CustomConsoleLogger(_consoleControlService);
        }

        public void Dispose()
        {
 
        }
    }
}
