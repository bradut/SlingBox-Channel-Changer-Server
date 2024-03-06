using RunSlingServer.Configuration.Services;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RunSlingServer.Configuration.Models
{
    /// <summary>
    /// Collection of classes that represent the appsettings.json file
    /// </summary>

    [JsonSerializable(typeof(SlingboxServerSettings))]
    public class SlingboxServerSettings
    {
        public string ExecutableName { get; set; } = "slingbox_server.exe";
        public string?[] Arguments { get; set; } = { "config.ini" };
    }
    

    [JsonSerializable(typeof(TvGuideSettings))]
    public class TvGuideSettings
    {
        private string _tvGuideUrl = string.Empty;

        public string TvGuideUrl
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_tvGuideUrl))
                    throw new ArgumentNullException($"{nameof(TvGuideUrl)} cannot be empty");
                return _tvGuideUrl;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException($"{nameof(TvGuideUrl)} cannot be empty");
                _tvGuideUrl = value;
            }
        }


        private string _slingRemoteControlUrl = string.Empty;

        public string SlingRemoteControlUrl
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_slingRemoteControlUrl))
                    throw new ArgumentNullException($"{nameof(SlingRemoteControlUrl)} cannot be empty");
                return _slingRemoteControlUrl;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException($"{nameof(SlingRemoteControlUrl)} cannot be empty");
                _slingRemoteControlUrl = value;
            }
        }
    }

    [JsonSerializable(typeof(AppSettings))]
    public class AppSettings
    {
        private const string AppVersion = "2024.03.06"; // remove output cache
        // "2024.02.02"; // output cache
        // "2024.01.19"; // fix assembly not found exception
        // "2024.01.03";
        // //"1.0.0";

        public string Version { get; set; } = AppVersion;

        public SlingboxServerSettings SlingboxServer { get; set; } = new();

        public TvGuideSettings TvGuide { get; set; } = new();

        public Dictionary<string, string?>? RemoteControlIrCodes { get; set; } = new()
        {
            {"Ch+", "4"},
            {"Ch-", "5"},
            {"Last", "56"}
            //, will NOT read channel-number specific IR codes from Slinger console, it's too error-prone. 
            //{"num_1", "9"},
            //{"num_2", "10"},
            //{"num_3", "11"},
            //{"num_4", "12"},
            //{"num_5", "13"},
            //{"num_6", "14"},
            //{"num_7", "15"},
            //{"num_8", "16"},
            //{"num_9", "17"},
            //{"num_0", "18"}
        };

        [JsonIgnore]
        public bool RequiresUpdate => Version != AppVersion;


        public void UpdateVersion()
        {
            Version = AppVersion;
        }
    }


    [JsonSerializable(typeof(LoggingSettings))]
    public class LoggingSettings
    {
        public LogLevelSettings LogLevel { get; set; } = new();
        public string LogFilePath { get; set; } = "logs/log-channel-change-{Date}.txt";
    }

    public class LogLevelSettings
    {
        public string Default { get; set; } = "Information";

        [JsonPropertyName("Microsoft.AspNetCore")]
        public string MicrosoftAspNetCore { get; set; } = "Warning";
    }


    public class UrlSettings
    {
        public string Url { get; set; } = "http://localhost:5196";
    }

    [JsonConverter(typeof(EndpointsSettingsConverter))]
    public class EndpointsSettings
    {
        public UrlSettings? Http { get; set; }
        public UrlSettings? Https { get; set; }
    }

    [JsonSerializable(typeof(KestrelSettings))]
    public class KestrelSettings
    {
        public EndpointsSettings Endpoints { get; set; } = new()
        {
            Http = new UrlSettings { Url = "http://localhost:5196" },
            Https = new UrlSettings { Url = null! } //"https://localhost:7064"// Leave null to avoid SLL certificate issues
        };
    }


    [JsonSerializable(typeof(AppConfiguration))]
    public class AppConfiguration : IAppConfiguration
    {
        public const string JSON_FILE_NAME = "appsettings.json";

        [JsonIgnore]
        public string JsonFileName => JSON_FILE_NAME;

        [JsonIgnore]
        public string Version
        {
            get => AppSettings.Version;
            set => AppSettings.Version = value;
        }

        public AppSettings AppSettings { get; set; } = new();

        public LoggingSettings Logging { get; set; } = new();
        public string AllowedHosts { get; set; } = "*";
        public KestrelSettings Kestrel { get; set; } = new();


        private string _webApiBaseUrl = "";
        [JsonIgnore]
        public string WebApiBaseUrl
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_webApiBaseUrl))
                {
                    return _webApiBaseUrl;
                }

                var uri = new Uri(AppSettings.TvGuide.SlingRemoteControlUrl);
                _webApiBaseUrl = uri.Scheme + "://" + uri.Authority;

                return _webApiBaseUrl;
            }
        }


        [JsonIgnore]
        public string RootPath => Directory.GetCurrentDirectory();

        [JsonIgnore]
        public Dictionary<string, string?>? RemoteControlIrCodes => AppSettings.RemoteControlIrCodes;

        [JsonIgnore]
        public string SlingboxServerExecutableName => AppSettings.SlingboxServer.ExecutableName;
        
        [JsonIgnore]
        public string SlingBoxServerConfigFileName => AppSettings.SlingboxServer.Arguments.FirstOrDefault() ?? "config.ini";

        [JsonIgnore]
        public string TvGuideUrl => AppSettings.TvGuide.TvGuideUrl;

        [JsonIgnore]
        public string SlingRemoteControlServiceUrl => AppSettings.TvGuide.SlingRemoteControlUrl;

        [JsonIgnore]
        public bool RequiresUpdate => AppSettings.RequiresUpdate;


        public void UpdateVersion()
        {
            AppSettings.UpdateVersion();
        }



        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"SlingboxServerExecutableName: {SlingboxServerExecutableName}{Environment.NewLine}");
            sb.AppendLine($"SlingboxServerArguments: {string.Join(", ", AppSettings.SlingboxServer.Arguments)}{Environment.NewLine}");
            sb.AppendLine($"TvGuideUrl: {TvGuideUrl}{Environment.NewLine}");
            sb.AppendLine($"SlingRemoteControlServiceUrl: {SlingRemoteControlServiceUrl}{Environment.NewLine}");

            return sb.ToString();
        }


        public string ToJson()
        {
            // This handles the special encoding of "+" but is not AoT friendly
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
            var indentedJson = JsonSerializer.Serialize(this, options);
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
            
            return indentedJson;
        }
    }
}
