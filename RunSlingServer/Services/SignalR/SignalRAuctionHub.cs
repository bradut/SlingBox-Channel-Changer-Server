using Application.SignalRServices;
using Application.SignalRServices.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace RunSlingServer.Services.SignalR
{
    public class SignalRAuctionHub : Hub, ISignalRAuctionHub
    {
        public async Task NotifyNewChannel(ChannelChangedNotification notification)
        {
            await Clients.All.SendAsync("ChannelChanged", notification);
        }

        public async Task NotifyStreamingInProgress(StreamingInProgressNotification notification)
        {
            await Clients.All.SendAsync("StreamingInProgress", notification);
        }

        public async Task NotifyStreamingStopped(StreamingStoppedNotification notification)
        {
            await Clients.All.SendAsync("StreamingStopped", notification);
        }

        public async Task NotifySlingBoxBricked(SlingBoxBrickedNotification notification)
        {
            await Clients.All.SendAsync("SlingBoxBricked", notification);
        }

        public async Task NotifyRemoteLocked(SlingBoxBrickedNotification notification)
        {
            await Clients.All.SendAsync("RemoteLocked", notification);
        }
    }
}
