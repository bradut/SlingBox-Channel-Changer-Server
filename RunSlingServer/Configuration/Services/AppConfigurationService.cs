using Application.Abstractions;
using Domain.Abstractions;
using RunSlingServer.Configuration.Models;

namespace RunSlingServer.Configuration.Services
{
    /// <summary>
    /// Load and save the app configuration from the appsettings.json file
    /// </summary>
    public class AppConfigurationService
    {
        public string AppSettingsConfigFileName => AppConfiguration.JSON_FILE_NAME; //"appsettings.json";

        private readonly IFileSystemAccess _fileSystemAccess;
        private AppConfiguration _appSettings = null!;
        private readonly ILogger<AppConfigurationService> _logger;

        public AppConfigurationService(IFileSystemAccess fileSystemAccess, ILogger<AppConfigurationService> logger)
        {
            _fileSystemAccess = fileSystemAccess;
            _logger = logger;
        }


        public AppConfiguration LoadAndUpdateConfiguration()
        {
            var appSettings = LoadConfiguration();

            if (!appSettings.RequiresUpdate)
                return appSettings;

            _logger.LogInformation("Updating the appsettings.json file to the latest version");
            appSettings.UpdateVersion();
            SaveConfiguration(appSettings);

            return appSettings;
        }

        private void SaveConfiguration(ISerializeToJsonFile appSettings)
        {
            _fileSystemAccess.SaveToJsonFile(appSettings);
        }

        public AppConfiguration LoadConfiguration()
        {
            if (!File.Exists(AppSettingsConfigFileName))
            {
                var appSettings = new AppConfiguration();
                SetAppSettingsDefaultValues(appSettings);
                SaveConfiguration(appSettings);

                _logger.LogInformation("Created the appsettings.json file with default values");

                return appSettings;
            }

            var configuration = new ConfigurationBuilder()
                  .SetBasePath(Directory.GetCurrentDirectory())
                  .AddJsonFile(AppSettingsConfigFileName, optional: true, reloadOnChange: true)
                  .Build();


            _appSettings = new AppConfiguration
            {
                Version = configuration["AppSettings:Version"] ?? "0.0.0"
            };


#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
            _appSettings.AppSettings.SlingboxServer = new SlingboxServerSettings
            {
                ExecutableName = configuration["AppSettings:SlingboxServer:ExecutableName"] ?? "slingbox_server.exe",
                Arguments = configuration.GetSection("AppSettings:SlingboxServer:Arguments").Get<string?[]>() ?? new[] { "config.ini" }
            };
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

            _appSettings.AppSettings.TvGuide = new TvGuideSettings
            {
                TvGuideUrl = configuration["AppSettings:TvGuide:TvGuideUrl"] ?? string.Empty,
                SlingRemoteControlUrl = configuration["AppSettings:TvGuide:SlingRemoteControlUrl"] ?? string.Empty
            };

            if (string.IsNullOrWhiteSpace(_appSettings.AppSettings.TvGuide.TvGuideUrl)) throw new KeyNotFoundException("AppSettings:TvGuide:TvGuideUrl");
            if (string.IsNullOrWhiteSpace(_appSettings.AppSettings.TvGuide.SlingRemoteControlUrl)) throw new KeyNotFoundException("AppSettings:TvGuide:SlingRemoteControlUrl");

            try
            {
#pragma warning disable IL2026 
                configuration.GetSection("AppSettings:TvGuide:RemoteControlIrCodes").Bind(_appSettings.RemoteControlIrCodes);
#pragma warning restore IL2026
            }
            catch (Exception e)
            {
                var msg = "Could not read the IR RC codes from config files. Error: " + e.Message;
                Console.WriteLine(msg);
                _logger.LogError(msg);
            }



            _appSettings.Logging = new LoggingSettings
            {
                LogLevel = new LogLevelSettings
                {
                    Default = configuration["Logging:LogLevel:Default"] ?? "Information",
                    MicrosoftAspNetCore = configuration["Logging:LogLevel:Microsoft.AspNetCore"] ?? "Warning"
                },
                LogFilePath = configuration["Logging:LogFilePath"] ?? "logs/log-channel-change-{Date}.txt"
            };

            _appSettings.AllowedHosts = configuration["AllowedHosts"] ?? "*";


            var httpUrl = configuration["Kestrel:Endpoints:Http:Url"] ?? "";
            var httpsUrl = configuration["Kestrel:Endpoints:Https:Url"] ?? "";

            _appSettings.Kestrel = new KestrelSettings
            {
                Endpoints = new EndpointsSettings
                {
                    Http = new UrlSettings { Url = httpUrl },
                    Https = new UrlSettings { Url = httpsUrl }
                }
            };


            return _appSettings;
        }


        private void SetAppSettingsDefaultValues(AppConfiguration appSettings)
        {
            appSettings.AppSettings.SlingboxServer.ExecutableName = "slingbox_server.exe";
            //appSettings.AppSettings.SlingboxServer.Arguments = new[] { "config.ini" };
            SetSlingerConfigFileNameAsParameter(appSettings);

            appSettings.AppSettings.TvGuide.TvGuideUrl = "http://localhost:80/TvGuideWebSite/TvGuide.html";
            appSettings.AppSettings.TvGuide.SlingRemoteControlUrl = "http://localhost:5196/api/post-to-url";

            //appSettings.AppSettings.Logging.LogLevel.Default = "Information";
            //appSettings.AppSettings.Logging.LogLevel.MicrosoftAspNetCore = "Warning";
            //appSettings.AppSettings.Logging.LogFilePath = "logs/log-channel-change-{Date}.txt";

            //appSettings.AppSettings.AllowedHosts = "*";

            //appSettings.AppSettings.Kestrel.Endpoints.Http.Url = "http://localhost:5196";
            //appSettings.AppSettings.Kestrel.Endpoints.Https.Url = null; //"https://localhost:7064"// Leave null to avoid SLL certificate issues
        }


        private void SetSlingerConfigFileNameAsParameter(AppConfiguration appSettings)
        {
            if (appSettings.AppSettings.SlingboxServer.Arguments.Any())
            {
                var configFileName = appSettings.AppSettings.SlingboxServer.Arguments[0];
                if (File.Exists(configFileName))
                {
                    return;
                }
            }

            if (File.Exists("config.ini"))
            {
                appSettings.AppSettings.SlingboxServer.Arguments = new[] { "config.ini" };
            }
            else

            if (File.Exists("unified_config.ini"))
            {
                appSettings.AppSettings.SlingboxServer.Arguments = new[] { "unified_config.ini" };
            }
            else
            {
                var someConfigFile = Directory.GetFiles(".", "*.ini").FirstOrDefault();
                if (someConfigFile != null)
                {
                    appSettings.AppSettings.SlingboxServer.Arguments = new[] { someConfigFile };
                }
                else
                {
                    var msg = "Could not find any 'ini' config file in the current directory: " + Directory.GetCurrentDirectory();
                    var color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"****************************************************\n" +
                                      $"{msg}" +
                                      $"\n\"****************************************************");
                    Console.ForegroundColor = color;
                    _logger.LogError(msg);
                }
            }
        }
    }

}
