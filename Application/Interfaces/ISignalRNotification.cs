namespace Application.Interfaces
{
    public interface ISignalRNotification
    {
        string SignalRHubMethodName { get; } // Method name on the hub
        string SignalRClientMethodName { get; } // Method name on the client


        string SlingBoxName { get; init; }
        string EventOrigin { get; init; }
        DateTime Timestamp { get; }
    }
}
