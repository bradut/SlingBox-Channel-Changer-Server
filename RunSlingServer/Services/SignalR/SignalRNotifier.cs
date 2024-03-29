﻿using Application.Interfaces;
using Application.Services;
using Application.SignalRServices;
using Application.SignalRServices.Notifications;
using Microsoft.AspNetCore.SignalR.Client;
using RunSlingServer.Helpers;

namespace RunSlingServer.Services.SignalR
{
    public class SignalRNotifier : ISignalRNotifier
    {
        private readonly string _webServerUrl;
        private readonly ConsoleDisplayDispatcher _console;
        private readonly ILogger<SignalRNotifier>? _logger;

        private HubConnection? Connection { get; set; }
        private readonly CancellationToken _cancellationToken = new();

        private string HubEndpoint => $"{_webServerUrl}/auctionhub";

        public SignalRNotifier(string webServerUrl, ConsoleDisplayDispatcher console, ILogger<SignalRNotifier>? logger = null)
        {
            _webServerUrl = webServerUrl;
            _console = console;
            _logger = logger ?? CreateLogger();

            _ = CreateHubConnectionAndRegisterHandlersAsync(_cancellationToken);
        }

        private static ILogger<SignalRNotifier> CreateLogger()
        {
            return LoggerFactory
                .Create(loggingBuilder => loggingBuilder.AddConsole())
                .CreateLogger<SignalRNotifier>();
        }


        public async Task NotifyClients(ISignalRNotification notification)
        {
            // it's OK to not use an IHttpClientFactory here because this is a console app and will use a single instance of HttpClient
            using var httpClient = CreateHttpConnectionWithSignalRServer();

            if (Connection is null)
            {
                var errMsg = $"{nameof(NotifyClients)}(): SignalR connection is null";
                _logger?.LogError(errMsg);
                await DisplayMessageAsync(errMsg, true);

                return;
            }

            if (Connection.State != HubConnectionState.Connected)
            {
                await CreateHubConnectionAndRegisterHandlersAsync(_cancellationToken);
            }


            if (Connection.State == HubConnectionState.Connected)
            {
                try
                {
                    await DisplayNotificationInfo(notification, false);

                    await Connection.InvokeAsync(notification.SignalRHubMethodName, notification, cancellationToken: _cancellationToken);

                    await DisplayMessageAsync($"Notified others about {notification.SignalRClientMethodName}, {notification.SlingBoxName}");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Error notifying others about {notification.SignalRClientMethodName}, {notification.SlingBoxName}");
                    await DisplayMessageAsync(ex.Message, true);
                }
            }
            else
            {
                _logger?.LogError($"SignalR: Connection is NOT Connected: {Connection.State}");
                await DisplayMessageAsync($"{nameof(NotifyClients)}(): SignalR connection is NOT Connected: {Connection.State}");
            }
        }

        private HttpClient CreateHttpConnectionWithSignalRServer()
        {
            var httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(300),
                BaseAddress = new Uri(_webServerUrl)
            };

            return httpClient;
        }


        private static readonly SemaphoreSlim ConnectionSyncLock = new(1);

        private async Task CreateHubConnectionAndRegisterHandlersAsync(CancellationToken cancellationToken)
        {
            Connection = new HubConnectionBuilder()
                .WithUrl(HubEndpoint)
                .WithAutomaticReconnect()
                .Build();


            RegisterConnectionEventHandlers();


            // Register a callback function to handle the Closed event of the SignalR connection.
            // When the Connection is closed unexpectedly, the anonymous async function is called.
            // It first waits for a random delay between 0 and 5 seconds using the Task.Delay method,
            // and then restarts the SignalR connection by calling Connection.StartAsync method.
            //
            // This code pattern ensures the SignalR connection is always being monitored for closure,
            // and will immediately attempt to reopen the connection if it closes unexpectedly.

            Connection.Closed += async (error) =>
            {
                _logger?.LogInformation($"SignalR: Connection closed: {error?.Message}");

                await Task.Delay(new Random().Next(0, 5) * 1000, cancellationToken);
                await CreateConnection(cancellationToken);
            };

            await CreateConnection(cancellationToken);
            return;
            

            async Task CreateConnection(CancellationToken cancelToken)
            {
                await ConnectionSyncLock.WaitAsync(cancelToken);

                try
                {
                    if (Connection.State == HubConnectionState.Disconnected)
                    {
                        if (cancelToken.IsCancellationRequested)
                        {
                            Console.WriteLine("SignalR: Cancellation requested while creating connection");
                            return;
                        }

                        Console.WriteLine($"SignalR: Restarting connection. Current State: {Connection.State}");
                        await Connection.StartAsync(cancelToken);
                    }
                }
                catch (Exception exception)
                {
                    var displayErrMsg = GenerateDisplayErrMsg(exception);
                    await DisplayMessageAsync(displayErrMsg, true);

                    var errMsg = $"SignalR: Error creating connection to '{HubEndpoint}'";
                    _logger?.LogError(exception, errMsg);  //throw;

                    SoundPlayer.PlayArpeggio();
                }
                finally
                {
                    ConnectionSyncLock.Release();
                }
            }


            string GenerateDisplayErrMsg(Exception ex)
            {
                var displayErrMsg = $"SignalR: Error creating connection to '{HubEndpoint}'.\n\n" +

                                    "This server is not able to communicate with the channel-changer TV Guide.\n\n" +

                                    "Please check if the endpoint above belongs to THIS server, if port forwarding to your server is correct, etc.\n\n" +

                                    "This issue may be caused by a misconfigured server, or a (temporary) network issue.\n\n" +

                                    $"Error message: {ex.Message}";

                return displayErrMsg;
            }
        }

        private void RegisterConnectionEventHandlers()
        {
            if (Connection == null)
            {
                _logger?.LogError("SignalR: Connection is null");
                DisplayMessageAsync("SignalR: Connection is null", true).GetAwaiter().GetResult();

                return;
            }


            Connection.Remove("ChannelChanged");
            Connection.On("ChannelChanged",
                async (ChannelChangedNotification receivedNotification) =>
                {
                    await DisplayNotificationInfo(receivedNotification, true);
                });
            
            Connection.Remove("StreamingInProgress");
            Connection.On("StreamingInProgress",
                async (StreamingInProgressNotification receivedNotification) =>
                {
                    await DisplayNotificationInfo(receivedNotification, true);
                });
            
            Connection.Remove("StreamingStopped");
            Connection.On("StreamingStopped",
                async (StreamingStoppedNotification receivedNotification) =>
                {
                    await DisplayNotificationInfo(receivedNotification, true);
                });

            Connection.Remove("SlingBoxBricked");
            Connection.On("SlingBoxBricked",
                async (SlingBoxBrickedNotification receivedNotification) =>
                {
                    await DisplayNotificationInfo(receivedNotification, true);
                });

            Connection.Remove("RemoteLocked");
            Connection.On("RemoteLocked",
                async (RemoteLockedNotification receivedNotification) =>
                {
                    await DisplayNotificationInfo(receivedNotification, true);
                });
        }


        private async Task DisplayNotificationInfo(ISignalRNotification notification, bool isNotificationReceived)
        {
            using (await _console.GetLockAsync())
            {
                var fontColor = Console.ForegroundColor;
                var newFontColor = isNotificationReceived
                    ? ConsoleColor.Yellow
                    : ConsoleColor.Cyan;

                var explanation = isNotificationReceived
                    ? $"Received notification: {notification.SignalRClientMethodName}, {notification.SlingBoxName}:"
                    : $"Will notify others: {notification.SignalRClientMethodName}, {notification.SlingBoxName}:";

                Console.ForegroundColor = newFontColor;
                Console.WriteLine("--[SignalR Notifier]---------------------------");
                Console.WriteLine($"{explanation}");

                switch (notification)
                {
                    case ChannelChangedNotification cc:
                        Console.WriteLine(
                            $"SlingBox: {cc.SlingBoxName}  New channel: {cc.NewChannelNumber}  Event origin: {cc.EventOrigin}  TimeStamp {cc.Timestamp}");
                        break;

                    case StreamingStoppedNotification _:
                    case StreamingInProgressNotification _:
                    case SlingBoxBrickedNotification _:
                    case RemoteLockedNotification _:
                        Console.WriteLine(
                            $"SlingBox: {notification.SlingBoxName}  Event origin: {notification.EventOrigin} TimeStamp {notification.Timestamp}");
                        break;

                    default:
                        await DisplayMessageAsync($"Unknown notification type {notification.GetType()}, Event origin {notification.EventOrigin}, TimeStamp {notification.Timestamp}", true);
                        break;
                }

                Console.WriteLine("-----------------------------------------------");
                Console.ForegroundColor = fontColor;
            }
        }


        private async Task DisplayMessageAsync(string message, bool isError = false)
        {
            using (await _console.GetLockAsync())
            {
                var fontColor = Console.ForegroundColor;
                Console.ForegroundColor = isError
                ? ConsoleColor.Red
                : ConsoleColor.Blue;

                Console.WriteLine("--[SignalR Notifier]---------------------------");
                Console.WriteLine($"{message}");
                Console.WriteLine("-----------------------------------------------");
                Console.ForegroundColor = fontColor;
            }
        }
    }
}
