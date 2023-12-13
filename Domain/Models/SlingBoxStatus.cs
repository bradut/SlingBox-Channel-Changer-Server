using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Domain.Models
{
    public class SlingBoxStatus : INotifyPropertyChanged
    {
        private const int IntervalToDeclareSlingBoxNotStreamingSeconds = 100;

        public SlingBoxStatus(string slingBoxName, string slingBoxId, bool isAnalogue = false, string? tvGuideUrl = null)
        {
            SlingBoxName = slingBoxName;
            SlingBoxId = slingBoxId;
            IsAnalogue = isAnalogue;
            TvGuideUrl = tvGuideUrl;
        }


        public SlingBoxStatus(string slingBoxName, string slingBoxId, int currentChannelNumber, int lastChannelNumber, 
                              bool isAnalogue, DateTime? lastHeartBeatTimeStamp, string? tvGuideUrl = null)
        {
            SlingBoxId = slingBoxId;
            SlingBoxName = slingBoxName;
            CurrentChannelNumber = currentChannelNumber;
            LastChannelNumber = lastChannelNumber;
            IsAnalogue = isAnalogue;
            LastHeartBeatTimeStamp = lastHeartBeatTimeStamp;
            TvGuideUrl = tvGuideUrl;
        }


        [JsonIgnore]
        public string SlingBoxName { get; init; }

        public string SlingBoxId { get; init; }


        private int _currentChannelNumber = -1;
        public int CurrentChannelNumber
        {
            get => _currentChannelNumber;
            set
            {
                if (value < 0) return;
                if (_currentChannelNumber == value) return;

                LastChannelNumber = _currentChannelNumber;
                _currentChannelNumber = value;

                LastHeartBeatTimeStamp = DateTime.Now;

                OnPropertyChanged();
            }
        }

        private int _lastChannelNumber = -1;
        public int LastChannelNumber
        {
            get => _lastChannelNumber;
            private set
            {
                if (value < 0) return;
                _lastChannelNumber = value;
            }
        }



        public bool IsAnalogue { get; set; }

        // anytime info about this channel is updated in the console it means it is in use
        public DateTime? LastHeartBeatTimeStamp { get; private set; }

        public bool IsStreaming => LastHeartBeatTimeStamp is not null &&
                                   (DateTime.Now - LastHeartBeatTimeStamp.Value).TotalSeconds < IntervalToDeclareSlingBoxNotStreamingSeconds;


        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TvGuideUrl { get; set; } // when not null, override server-wide TvGuideUrl



        // ToDo: implement circular navigation, as after the LAST channel, the next channel is the first one
        public void ChannelUp()
        {
            if (CurrentChannelNumber < 0) return;
            CurrentChannelNumber++;
        }

        // ToDo: implement circular navigation, as after the FIRST channel, the previous channel is the last one
        public void ChannelDown()
        {
            if (CurrentChannelNumber < 1) return;
            CurrentChannelNumber--;
        }

        public void ChangeChannel(int channel)
        {
            if (channel < 0) return;
            CurrentChannelNumber = channel;
        }

        public void SelectLastChannel()
        {
            if (LastChannelNumber < 0) return;
            CurrentChannelNumber = LastChannelNumber;
        }

        public void UpdateLastHeartBeatTimeStamp(DateTime? timeStamp)
        {
            LastHeartBeatTimeStamp = timeStamp;
        }

        public void SetStreamingStopped()
        {
            LastHeartBeatTimeStamp = null;
        }


        public override string ToString()
        {
            return SlingBoxName;
        }

        // ToDo: either implement observable pattern or remove this code
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

