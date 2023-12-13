using Application.Interfaces;

namespace Application.SignalRServices.Notifications
{
    public class StreamingInProgressNotification: ISignalRNotification
    {

        public string SignalRHubMethodName => "NotifyStreamingInProgress";
        public string SignalRClientMethodName => "StreamingInProgress";


        public string SlingBoxName { get; init; }
        public string EventOrigin { get; init; }
        public DateTime Timestamp { get; }

        public StreamingInProgressNotification(string slingBoxName, string eventOrigin)
        {
            SlingBoxName = slingBoxName;
            EventOrigin = eventOrigin;
            Timestamp = DateTime.Now;
        }
    }
}
