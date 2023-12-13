using Application.SignalRServices.Notifications;

namespace Application.SignalRServices;

public interface ISignalRAuctionHub
{
    Task NotifyNewChannel(ChannelChangedNotification notification);
    Task NotifyStreamingInProgress(StreamingInProgressNotification notification);
    Task NotifyStreamingStopped(StreamingStoppedNotification notification);
    Task NotifySlingBoxBricked(SlingBoxBrickedNotification notification);
    Task NotifyRemoteLocked(SlingBoxBrickedNotification notification);
}