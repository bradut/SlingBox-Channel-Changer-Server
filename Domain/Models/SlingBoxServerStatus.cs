using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Domain.Abstractions;
using Domain.Helpers;

namespace Domain.Models
{
    public class SlingBoxServerStatus : ISerializeToJsonFile
    {
        // ReSharper disable once InconsistentNaming
        public const string JSON_FILE_NAME = "SlingBoxStatus.json";

        [JsonIgnore]
        public string JsonFileName => JSON_FILE_NAME;


        [JsonPropertyName("_slingBoxes")] // Specify the JSON property name for the private field as "slingBoxes"
        private readonly Dictionary<string, SlingBoxStatus> _slingBoxes = new();
        public ReadOnlyDictionary<string, SlingBoxStatus> SlingBoxes => _slingBoxes.AsReadOnly();

        // Url segment with a "secret value" meant to prevent slingbox server being easily accessible to hackers / port scanners.
        // Example: "secret_sbx"=> http://my.SlingBoxServer.com/secret_sbx/MySlingBoxName
        public string UrlBase { get; set; } = string.Empty;

        // Website that displays the TV Guide with info gathered from 'various sources'
        public string TvGuideUrl { get; set; } = string.Empty;

        // This WebAPI's URL sent to the TvGuide to call back when changing channels on the SlingBox
        public string SlingRemoteControlServiceUrl { get; set; } = string.Empty;




        public SlingBoxServerStatus()
        {
        }


        public SlingBoxServerStatus(IEnumerable<SlingBoxStatus> slingBoxStatusCollection)
        {
            foreach (var slingBoxStatus in slingBoxStatusCollection)
            {
                _slingBoxes[slingBoxStatus.SlingBoxName] = slingBoxStatus;
            }
        }


        public void AddSlingBox(string slingBoxName, string slingBoxId,
                                   int currentChannelNumber = -1, int lastChannelNumber = -1,
                                   bool isAnalogue = false, DateTime? lastHeartBeatTimeStamp = null, string? tvGuideUrl = null)
        {
            if (_slingBoxes.ContainsKey(slingBoxName))
            {
                throw new ArgumentException($"SlingBox name {slingBoxName} already exists in {nameof(SlingBoxServerStatus)}");
            }

            var slingBoxStatus = new SlingBoxStatus(slingBoxName, slingBoxId, currentChannelNumber, lastChannelNumber,
                                                    isAnalogue, lastHeartBeatTimeStamp, tvGuideUrl);

            _slingBoxes[slingBoxStatus.SlingBoxName] = slingBoxStatus;
        }

        public void RemoveSlingBox(string slingBoxName)
        {
            _slingBoxes.Remove(slingBoxName);
        }

        public SlingBoxStatus GetSlingBoxStatus(string slingBoxName)
        {
            return _slingBoxes.TryGetValue(slingBoxName, out var slingBoxStatus)
                ? slingBoxStatus
                : throw new KeyNotFoundException($"SlingBox name {slingBoxName} not found in {nameof(SlingBoxServerStatus)}");
        }

        [JsonIgnore]
        public int SlingBoxesCount => _slingBoxes.Count;


        public bool SetSlingBoxLastHeartBeatTimeStamp(string slingBoxName, DateTime timeStamp)
        {
            var slingBoxStatus = GetSlingBoxStatus(slingBoxName);
            slingBoxStatus.UpdateLastHeartBeatTimeStamp(timeStamp);
            return true; // initial logic was to return false if sling not found. ToDO: refactor
        }
        
        public void SetSlingBoxCurrentChannelNumber(string slingBoxName, int channelNumber)
        {
            var slingBoxStatus = GetSlingBoxStatus(slingBoxName);
            slingBoxStatus.CurrentChannelNumber = channelNumber;
        }
        
        public void SetSlingBoxIsAnalogue(string slingBoxName, bool isAnalogue)
        {
            var slingBoxStatus = GetSlingBoxStatus(slingBoxName);
            slingBoxStatus.IsAnalogue = isAnalogue;
        }

        public void SetSlingBoxTvGuideUrl(string slingBoxName, string tvGuideUrl)
        {
            var slingBoxStatus = GetSlingBoxStatus(slingBoxName);
            slingBoxStatus.TvGuideUrl = tvGuideUrl;
        }
        
        public void SetServerStreamingStopped()
        {
            foreach (var slingBox in SlingBoxes)
            {
                slingBox.Value.SetStreamingStopped();
            }
        }



        public string ToJson()
        {
            return SlingBoxServerSerializer.SerializeToJson(this);
        }
        
        public override string ToString()
        {
            if (SlingBoxesCount == 0) return "No SlingBoxes saved in previous session";

            var result = "SlingBoxes\n";
            foreach (var (slingBoxName, slingBox) in SlingBoxes)
            {
                var channelStr = $"{slingBox.CurrentChannelNumber,5}";
                var lastChannelStr = $"{slingBox.LastChannelNumber,5}";
                result += $"Name: {slingBoxName}, \tId: {slingBox.SlingBoxId},   Channel: {channelStr},   Last: {lastChannelStr},   isAnalogue: {slingBox.IsAnalogue},   isStreaming: {slingBox.IsStreaming}\n";
            }

            return result;
        }

    }
}
