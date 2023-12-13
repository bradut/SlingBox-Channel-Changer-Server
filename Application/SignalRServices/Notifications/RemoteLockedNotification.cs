using Application.Interfaces;

namespace Application.SignalRServices.Notifications
{
    public class RemoteLockedNotification : ISignalRNotification
    {

        public string SignalRHubMethodName => "NotifyRemoteLocked";
        public string SignalRClientMethodName => "RemoteLocked";

        public string SlingBoxName { get; init; }
        public string EventOrigin { get; init; }

        public DateTime Timestamp { get; }

        public RemoteLockedNotification(string slingBoxName, string eventOrigin)
        {
            SlingBoxName = slingBoxName;
            EventOrigin = eventOrigin;
            Timestamp = DateTime.Now;
        }
    }
}
