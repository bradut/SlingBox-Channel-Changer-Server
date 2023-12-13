using Application.Abstractions;
using Application.Services;
using Infrastructure.FileAccess;
using RunSlingServer.Configuration;
using RunSlingServer.Configuration.Services;
using RunSlingServer.Services;
using RunSlingServer.WebApi.Services;
using SignalRNotifier = RunSlingServer.Services.SignalR.SignalRNotifier;

namespace RunSlingServer
{
    public static class ConsoleAppDependencyInjection
    {
        private static readonly IServiceCollection Services = new ServiceCollection();

        private static void ConfigureServices(string[] args)
        {
            // register ILoggerFactory service with the dependency injection container. 
            Services.AddLogging(builder => builder.AddConsole());
            var loggerFactory = Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();

            Services.AddSingleton<ConsoleDisplayDispatcher>();
            
            Services.AddSingleton<ILoggerProvider>(serviceProvider =>
            {
                var consoleControlService = serviceProvider.GetRequiredService<ConsoleDisplayDispatcher>();
                return new CustomConsoleLoggerProvider(consoleControlService);
            });
            
            Services.AddSingleton<IFileSystemAccess>(_ =>
            {
                var logger = loggerFactory.CreateLogger<IFileSystemAccess>();
                return new FileSystemAccess(Directory.GetCurrentDirectory(), logger);
            });

            Services.AddSingleton<AppConfigurationService>();
            Services.AddSingleton<IAppConfiguration>(sp => sp.GetRequiredService<AppConfigurationService>().LoadAndUpdateConfiguration());
           
            Services.AddSingleton<SlingerServerRunner>();

            Services.AddSingleton<SignalRNotifier>(serviceProvider =>
            {
                var appConfiguration = serviceProvider.GetRequiredService<IAppConfiguration>();
                var consoleDisplayDispatcher = serviceProvider.GetRequiredService<ConsoleDisplayDispatcher>();

                return new SignalRNotifier(appConfiguration.WebApiBaseUrl, consoleDisplayDispatcher);
            });
            
            Services.AddSingleton<SlingerServerRunner>(serviceProvider =>
                {
                    var appConfiguration = serviceProvider.GetRequiredService<IAppConfiguration>();
                    var serveRootPath = appConfiguration.RootPath;
                    var consoleDisplayDispatcher = serviceProvider.GetRequiredService<ConsoleDisplayDispatcher>();
                    var fileSystemAccess = serviceProvider.GetRequiredService<IFileSystemAccess>();
                    var signalRNotifier = serviceProvider.GetRequiredService<SignalRNotifier>();

                    return new SlingerServerRunner(appConfiguration, serveRootPath, consoleDisplayDispatcher, fileSystemAccess, signalRNotifier);
                }
            );

            Services.AddSingleton<WebApiService>(serviceProvider =>
            {
                var appConfiguration = serviceProvider.GetRequiredService<IAppConfiguration>();
                var serveRootPath = appConfiguration.RootPath;
                var consoleDisplayDispatcher = serviceProvider.GetRequiredService<ConsoleDisplayDispatcher>();
                var fileSystemAccess = serviceProvider.GetRequiredService<IFileSystemAccess>();
                var signalRNotifier = serviceProvider.GetRequiredService<SignalRNotifier>();
                
                var logger = loggerFactory.CreateLogger<WebApiService>();

                return new WebApiService(args, serveRootPath, consoleDisplayDispatcher, fileSystemAccess, signalRNotifier, logger);
            });


            Services.AddSingleton<SlingerConfigurationParser>(serviceProvider =>
            {
                var appConfiguration = serviceProvider.GetRequiredService<IAppConfiguration>();
                var logger = loggerFactory.CreateLogger<SlingerConfigurationParser>();

                return new SlingerConfigurationParser(appConfiguration.SlingBoxServerConfigFileName, logger);
            });
        }


        private static IServiceProvider? _serviceProvider;
        public static IServiceProvider GetServiceProvider(string[] args)
        {
            if (_serviceProvider != null)
                return _serviceProvider;

            ConfigureServices(args);
            _serviceProvider = Services.BuildServiceProvider();

            return _serviceProvider;
        }
    }
}
