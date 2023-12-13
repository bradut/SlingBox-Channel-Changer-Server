using Application.Interfaces;

namespace Application.SignalRServices;

public interface ISignalRNotifier
{
    Task NotifyClients(ISignalRNotification notification);
}