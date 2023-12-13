using Application.Interfaces;

namespace Application.SignalRServices.Notifications
{
    public class ChannelChangedNotification: ISignalRNotification
    {

        public string SignalRHubMethodName => "NotifyNewChannel";
        public string SignalRClientMethodName => "ChannelChanged";

        public string SlingBoxName { get; init; }
        public int NewChannelNumber { get; init; }
        public string EventOrigin { get; init; }

        public DateTime Timestamp { get; }

        public ChannelChangedNotification(string slingBoxName, int newChannelNumber, string eventOrigin)
        {
            SlingBoxName = slingBoxName;
            NewChannelNumber = newChannelNumber;
            EventOrigin = eventOrigin;
            Timestamp = DateTime.Now;
        }
    }
}
