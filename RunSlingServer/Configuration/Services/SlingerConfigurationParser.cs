using RunSlingServer.Configuration.Models;
using RunSlingServer.Helpers;

namespace RunSlingServer.Configuration.Services
{
    /// <summary>
    /// Create a SlingBoxServerConfiguration from the config file
    /// used by SLINGER SERVER (Slingbox_server.exe): config.ini or unified_config.ini.
    /// - reads the list of SlingBoxes
    /// - determine if a given slingbox source type is analogue.
    /// - reads the UrlBase value, which is Url segment with a "secret value" meant to prevent your slingbox server being easily accessible.
    /// </summary>
    public class SlingerConfigurationParser : ISlingerConfigurationParser
    {
        public string ConfigFilePath { get; }

        private readonly ILogger? _logger;

        public SlingerConfigurationParser(string configFilePath, ILogger? logger = null)
        {
            ConfigFilePath = configFilePath;
            _logger = logger;
        }


        public SlingerConfiguration Parse(string configBody = "")
        {
            if (string.IsNullOrWhiteSpace(configBody))
            {
                configBody = LoadFromFile();
            }

            var lines = configBody
                .Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => !line.StartsWith(';') && (line.Contains('=') || line.Contains('[') && line.Contains(']')))
                .Select(line => line.Trim())
                .ToList();

            var isSingleSlingBoxConfig = lines.Any(line => line.Contains("[SLINGBOX]"));


            var slingBoxServerConfiguration = isSingleSlingBoxConfig
                ? ParseSingleSlingConfig(lines)
                : ParseMultipleSlingConfig(lines);

            slingBoxServerConfiguration.IsUnifiedConfig = !isSingleSlingBoxConfig;

            return slingBoxServerConfiguration;
        }


        private static SlingerConfiguration ParseMultipleSlingConfig(List<string> lines)
        {
            var slingBoxConfigLines = GetSectionLines("[SLINGBOXES]", lines);
            var serverConfigLines = GetSectionLines("[SERVER]", lines);


            var slingerConfig = new SlingerConfiguration();

            var slingBoxConfigList = ParseSlingBoxesHeaderSection(slingBoxConfigLines);

            foreach (var boxConfiguration in slingBoxConfigList)
            {
                var header = $"[{boxConfiguration.SlingBoxName}]";
                var thisSlingBoxConfigLines = GetSectionLines(header, lines);

                ParseSlingBoxSectionConfig(thisSlingBoxConfigLines, boxConfiguration);

                slingerConfig.AddSlingBox(boxConfiguration);
            }

            ParseServerConfig(serverConfigLines, slingerConfig);

            return slingerConfig;
        }


        private static SlingerConfiguration ParseSingleSlingConfig(List<string> lines)
        {
            var slingBoxHeaderLines = GetSectionLines("[SLINGBOX]", lines);
            var slingBoxRemoteLines = GetSectionLines("[REMOTE]", lines);
            var serverConfigLines = GetSectionLines("[SERVER]", lines);

            var slingBoxConfig = new SlingBoxConfiguration();
            ParseSlingBoxSectionConfig(slingBoxHeaderLines, slingBoxConfig);
            ParseSlingBoxSectionConfig(slingBoxRemoteLines, slingBoxConfig);

            var slingerConfig = new SlingerConfiguration();
            slingerConfig.AddSlingBox(slingBoxConfig);

            ParseServerConfig(serverConfigLines, slingerConfig);

            return slingerConfig;
        }

        private static IEnumerable<SlingBoxConfiguration> ParseSlingBoxesHeaderSection(List<string> slingBoxConfigLines)
        {

            var sbConfigList = new List<SlingBoxConfiguration>();
            foreach (var line in slingBoxConfigLines)
            {
                if (!line.StartsWith("sb", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                var parts = line.Split('=');
                if (parts.Length == 2)
                {
                    sbConfigList.Add(new SlingBoxConfiguration { SlingBoxId = parts[0].Trim(), SlingBoxName = parts[1].Trim() });
                }
            }

            return sbConfigList;
        }

        private static void ParseServerConfig(List<string> serverConfigLines, SlingerConfiguration serverConfig)
        {
            foreach (var line in serverConfigLines)
            {
                var key = line.Split('=')[0].Trim().ToLowerInvariant();

                switch (key)
                {
                    case "port":
                        serverConfig.Port = int.Parse(GetLineValue(line));
                        break;
                    case "maxstreams": // single slingbox config
                    case "maxremotestreams": // multiple slingboxes config
                        serverConfig.MaxRemoteStreams = int.Parse(GetLineValue(line));
                        break;
                    case "urlbase":
                        serverConfig.UrlBase = GetLineValue(line);
                        break;
                }
            }
        }

        private static void ParseSlingBoxSectionConfig(List<string> slingBoxConfigLines, SlingBoxConfiguration slingBoxConfig)
        {
            foreach (var line in slingBoxConfigLines)
            {
                var key = line.Split('=')[0].Trim().ToLowerInvariant();

                switch (key)
                {
                    case "sbtype":
                        slingBoxConfig.SlingBoxType = GetLineValue(line);
                        break;
                    case "videosource":
                        slingBoxConfig.VideoSource = int.Parse(GetLineValue(line));
                        break;
                    case "name":
                        slingBoxConfig.SlingBoxName = GetLineValue(line);
                        break;
                    case "remote":
                        slingBoxConfig.RemoteControlFileName = GetLineValue(line);
                        break;
                    case "tvguideurl":
                        slingBoxConfig.TvGuideUrl = GetLineValue(line);
                        break;
                    case "include": // old-style config remote control file name
                        slingBoxConfig.RemoteControlFileName = GetLineValue(line);
                        break;
                }

            }
        }


        private static string GetLineValue(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) throw new ArgumentNullException(nameof(line));
            if (!line.Contains('=')) throw new InvalidDataException($"Missing '=' in {line}");

            if (line.Contains(';')) line = line.Substring(0, line.IndexOf(';')); // remove in-line comments

            var parts = line.Split('=');

            return parts[1].Trim().Replace("\"", "");
        }


        private static List<string> GetSectionLines(string sectionHeader, List<string> lines)
        {
            if (string.IsNullOrWhiteSpace(sectionHeader)) return new List<string>();
            if (!(sectionHeader.StartsWith("[") && sectionHeader.EndsWith("]")))
            {
                throw new ArgumentException($"sectionHeader should be like this [...] but was {sectionHeader}");
            }

            var sectionLines = new List<string>();

            foreach (var line in lines)
            {
                if (line.Equals(sectionHeader, StringComparison.InvariantCultureIgnoreCase))
                {
                    sectionLines.Add(line);
                }
                else if (line.StartsWith('[') && line.Contains(']') && sectionLines.Any())
                {
                    break;
                }

                else if (sectionLines.Any())
                {
                    sectionLines.Add(line);
                }
            }

            return sectionLines;
        }

        private string LoadFromFile()
        {
            var consoleColor = Console.ForegroundColor;

            Console.Write("Reading 'Slinger' configuration from file ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(ConfigFilePath);
            Console.ForegroundColor = consoleColor;

            if (!File.Exists(ConfigFilePath))
            {
                var errMsg = $"Slingbox server configuration file '{Path.GetFileName(ConfigFilePath)}' DOES NOT EXIST or was not set correctly in configuration file:{Environment.NewLine} or was not " +
                             $"{ConfigFilePath}{Environment.NewLine}" +
                             $"The app will STOP HERE {Environment.NewLine}";

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(errMsg);
                Console.ForegroundColor = consoleColor;

                SoundPlayer.PlayArpeggio();
                _logger?.LogError(errMsg);

                throw new FileNotFoundException(errMsg);
            }

            var configBody = File.ReadAllText(ConfigFilePath);

            return configBody;
        }
    }
}

