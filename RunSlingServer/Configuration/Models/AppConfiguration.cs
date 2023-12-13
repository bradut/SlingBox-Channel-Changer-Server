﻿using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using RunSlingServer.Configuration.Services;

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
        public string TvGuideUrl { get; set; } = "http://localhost:80/TvGuideWebSite/TvGuide.html";
        public string SlingRemoteControlUrl { get; set; } = "http://localhost:5196/api/post-to-url";
    }

    [JsonSerializable(typeof(AppSettings))]
    public class AppSettings
    {
        private const string AppVersion = "1.0.0";
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
            Https = new UrlSettings { Url = null! } //"https://localhost:7064"// Leave null to avoid certificate issues
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
        //public IISExpressSettings IISExpress { get; set; } = new();


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
        public string AppSettingsFullPath => Path.Combine(Directory.GetCurrentDirectory(), JsonFileName);


        [JsonIgnore]
        public string RootPath => Directory.GetCurrentDirectory();


        [JsonIgnore]
        public Dictionary<string, string?>? RemoteControlIrCodes
        {
            get => AppSettings.RemoteControlIrCodes;
            set => throw new NotSupportedException();
        }


        [JsonIgnore]
        public string SlingboxServerExecutableName
        {
            get => AppSettings.SlingboxServer.ExecutableName;
            set => throw new NotSupportedException();
        }


        [JsonIgnore]
        public string SlingBoxServerConfigFileName => AppSettings.SlingboxServer.Arguments.FirstOrDefault() ?? "config.ini";


        [JsonIgnore]
        public string TvGuideUrl
        {
            get => AppSettings.TvGuide.TvGuideUrl;
            set => throw new NotSupportedException();
        }


        [JsonIgnore]
        public string SlingRemoteControlServiceUrl
        {
            get => AppSettings.TvGuide.SlingRemoteControlUrl;
            set => throw new NotSupportedException();
        }


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



    [JsonSerializable(typeof(AppConfiguration))]
    [JsonSerializable(typeof(AppSettings))]

    [JsonSerializable(typeof(LoggingSettings))]
    [JsonSerializable(typeof(LogLevelSettings))]
    [JsonSerializable(typeof(UrlSettings))]

    [JsonSerializable(typeof(EndpointsSettings))]
    [JsonSerializable(typeof(EndpointsSettingsConverter))]

    [JsonSerializable(typeof(KestrelSettings))]
    public partial class JsonContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// Hand made converter for EndpointsSettings that does not write Https if it is null, but can read it if is not null.
    /// </summary>
    public class EndpointsSettingsConverter : JsonConverter<EndpointsSettings>
    {
        public override EndpointsSettings Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            var endpointsSettings = new EndpointsSettings();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return endpointsSettings;
                }
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(propertyName))
                    {
                        throw new JsonException("Could not read property name");
                    }

                    reader.Read(); // advance to the value
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
                    switch (propertyName)
                    {
                        case "Http":
                            endpointsSettings.Http = JsonSerializer.Deserialize<UrlSettings>(ref reader, options);
                            break;

                        case "Https":
                            endpointsSettings.Https = JsonSerializer.Deserialize<UrlSettings>(ref reader, options);
                            break;

                        default:
                            throw new JsonException($"Expected 'Http' or 'Https' but was {propertyName}");
                    }
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
                }
            }
            throw new JsonException();
        }



        // This code manually writes the properties of the Http and Https objects, checking for null values before writing.
        // This should achieve the same result as using the JsonIgnore attribute with JsonIgnoreCondition.WhenWritingNull.
        public override void Write(Utf8JsonWriter writer, EndpointsSettings value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            if (value.Http != null && !string.IsNullOrEmpty(value.Http.Url))
            {
                writer.WritePropertyName("Http");
                writer.WriteStartObject();
                writer.WriteString("Url", value.Http.Url);
                writer.WriteEndObject();
            }
            if (value.Https != null && !string.IsNullOrEmpty(value.Https.Url))
            {
                writer.WritePropertyName("Https");
                writer.WriteStartObject();
                writer.WriteString("Url", value.Https.Url);
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }
    }

}