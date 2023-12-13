using Application.Interfaces;

namespace Application.SignalRServices.Notifications
{
    public class SlingBoxBrickedNotification : ISignalRNotification
    {

        public string SignalRHubMethodName => "NotifySlingBoxBricked";
        public string SignalRClientMethodName => "SlingBoxBricked";


        public string SlingBoxName { get; init; }
        public string EventOrigin { get; init; }
        public DateTime Timestamp { get; }

        public SlingBoxBrickedNotification(string slingBoxName, string eventOrigin)
        {
            SlingBoxName = slingBoxName;
            EventOrigin = eventOrigin;
            Timestamp = DateTime.Now;
        }
    }

}
