using Application.Interfaces;

namespace Application.SignalRServices.Notifications
{
    public class StreamingStoppedNotification : ISignalRNotification
    {
        public string SignalRHubMethodName => "NotifyStreamingStopped";
        public string SignalRClientMethodName => "StreamingStopped";

        public string SlingBoxName { get; init; }
        public string EventOrigin { get; init; }
        public DateTime Timestamp { get; }

        public StreamingStoppedNotification(string slingBoxName, string eventOrigin)
        {
            SlingBoxName = slingBoxName;
            EventOrigin = eventOrigin;
            Timestamp = DateTime.Now;
        }
    }
}
