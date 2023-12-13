using RunSlingServer.Configuration.Models;

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
                .Where(line => !line.StartsWith(";"))
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
                if (line.StartsWith("port", StringComparison.InvariantCultureIgnoreCase))
                {
                    serverConfig.Port = int.Parse(GetLineValue(line));
                }

                // single slingbox config
                else if (line.StartsWith("maxstreams", StringComparison.InvariantCultureIgnoreCase))
                {
                    serverConfig.MaxRemoteStreams = int.Parse(GetLineValue(line));
                }

                // multiple slingboxes config
                else if (line.StartsWith("maxremotestreams", StringComparison.InvariantCultureIgnoreCase))
                {
                    serverConfig.MaxRemoteStreams = int.Parse(GetLineValue(line));
                }

                else if (line.StartsWith("URLbase", StringComparison.InvariantCultureIgnoreCase))
                {
                    serverConfig.UrlBase = GetLineValue(line);
                }
            }
        }

        private static void ParseSlingBoxSectionConfig(List<string> slingBoxConfigLines, SlingBoxConfiguration slingBoxConfig)
        {
            foreach (var line in slingBoxConfigLines)
            {
                if (line.StartsWith("sbtype", StringComparison.InvariantCultureIgnoreCase))
                {
                    slingBoxConfig.SlingBoxType = GetLineValue(line);
                }
                else if (line.StartsWith("VideoSource", StringComparison.InvariantCultureIgnoreCase))
                {
                    slingBoxConfig.VideoSource = int.Parse(GetLineValue(line));
                }
                else if (line.StartsWith("name", StringComparison.InvariantCultureIgnoreCase))
                {
                    slingBoxConfig.SlingBoxName = GetLineValue(line);
                }
                else if (line.StartsWith("Remote", StringComparison.InvariantCultureIgnoreCase))
                {
                    slingBoxConfig.RemoteControlFileName = GetLineValue(line);
                }
                else if (line.StartsWith("tvGuideUrl", StringComparison.InvariantCultureIgnoreCase))
                {
                    slingBoxConfig.TvGuideUrl = GetLineValue(line);
                }
                else if (line.StartsWith("include", StringComparison.CurrentCultureIgnoreCase)) // old-style config remote control file
                {
                    slingBoxConfig.RemoteControlFileName = GetLineValue(line);
                }
            }
        }


        private static string GetLineValue(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) throw new ArgumentNullException(nameof(line));
            if (!line.Contains('=')) throw new InvalidDataException($"Missing '=' in {line}");

            if (line.Contains(";")) line = line.Substring(0, line.IndexOf(';')); // remove in-line comments

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
                Console.WriteLine(errMsg);
                Console.WriteLine(errMsg);
                Console.WriteLine(errMsg);
                Console.ForegroundColor = consoleColor;

                _logger?.LogError(errMsg);

                throw new FileNotFoundException(errMsg);
            }

            var configBody = File.ReadAllText(ConfigFilePath);

            return configBody;
        }
    }
}

