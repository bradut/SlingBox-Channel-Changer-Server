namespace RunSlingServer.Configuration.Models
{
    /// <summary>
    /// Represents the configuration of an individual SlingBox as defined in
    /// SlingBox_Server.exe configuration file config.ini or 'unified_config.ini'
    /// </summary>
    internal struct ProHdVideoSource
    {
        public static int Tuner => 0;
        public static int Composite => 1;
        public static int SVideo => 2;
        public static int Component => 3;
    }

    public class SlingBoxConfiguration
    {
        public string SlingBoxId { get; init; } = string.Empty;
        public string SlingBoxName { get; set; } = string.Empty;
        public string SlingBoxType { get; set; } = string.Empty;
        public int VideoSource { get; set; }
        public string RemoteControlFileName { get; set; } = string.Empty;
        public string TvGuideUrl { get; set; } = string.Empty;


        /* *************************************************************************************************
        ; Video Source: 0, 1, 2, 3 depending on your hardware corresponds to one of Composite, Component, S-Video, HDMI or Tuner.
        ;Pro:        0=(Analog)Tuner   1=Composite   2=S-Video     3=Component (Via HDMI to Component Cable)
        ;ProHD:      0=Tuner           1=Composite   2=S-Video     3=Component
        ;Solo:       0=Composite       1=S-Video     2=Component
        ;500:        0=Composite       1=Component   2=HDMI
        ;350/M1/M2:  0=Composite       1=Component
        * *************************************************************************************************/
        public bool IsAnalogue => (SlingBoxType.Contains("ProHD", StringComparison.OrdinalIgnoreCase) ||
                                   SlingBoxType.Contains("Pro", StringComparison.OrdinalIgnoreCase)) &&
                                  VideoSource == ProHdVideoSource.Tuner;

        public override string ToString()
        {
            var aboutThis = $"name: {SlingBoxName}, isAnalogue: {IsAnalogue}, Type {SlingBoxType}";
            return aboutThis;
        }
    }
}
