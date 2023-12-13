using System.Collections.ObjectModel;

namespace RunSlingServer.Configuration.Models
{
    /// <summary>
    /// Represents the configuration of all the SlingBox-es and of the SlingBox_Server.exe
    /// 
    /// Mirrors the SlingBox_Server.exe configuration file: 'config.ini' or 'unified_config.ini'
    /// Useful to determine which boxes are analogue as well to get the UrlBase
    /// </summary>
    public class SlingerConfiguration
    {
        private readonly Dictionary<string, SlingBoxConfiguration> _slingBoxes = new();

        public ReadOnlyDictionary<string, SlingBoxConfiguration> SlingBoxes => new(_slingBoxes);


        public string UrlBase { get; set; } = string.Empty;
        public int Port { get; set; }
        public int MaxRemoteStreams { get; set; }

        public bool IsUnifiedConfig { get; set; }

        public int SlingBoxesCount => _slingBoxes.Count;


        public void AddSlingBox(SlingBoxConfiguration slingBoxConfiguration)
        {
            if (string.IsNullOrWhiteSpace(slingBoxConfiguration.SlingBoxName))
            {
                throw new ArgumentException("SlingBoxName cannot be null or empty");
            }

            _slingBoxes[slingBoxConfiguration.SlingBoxName] = slingBoxConfiguration;
        }
    }

}
