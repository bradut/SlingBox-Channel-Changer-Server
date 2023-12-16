using System.Text;
using System.Text.RegularExpressions;
using Application.Abstractions;
using Application.Interfaces;
using Application.SignalRServices;
using Application.SignalRServices.Notifications;
using Domain.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    /// <summary>
    /// Parse lines displayed on console by SlingBox_Server.exe
    /// - Update SlingBoxServerStatus in memory and on disk
    /// - Notify SignalR clients of changes
    /// This is a long and complex class, but it is the only way to get the status of the SlingBox_Server.exe
    /// </summary>
    public class ConsoleReaderService : IConsoleReaderService
    {

        public readonly struct ServerAction
        {
            public const string ChannelChanged = "ChannelChanged";
            public const string StreamingInProgress = "StreamingInProgress";
            public const string StreamingStopped = "StreamingStopped";
            public const string ErrorSlingBoxBricked = "SlingBoxBricked";
            public const string ErrorRemoteLocked = "RemoteControlLocked";
        }

        public SlingBoxServerStatus ServerStatus { get; }

        private readonly ConsoleDisplayDispatcher _console;
        private readonly ISignalRNotifier? _signalRNotifier;
        private readonly IFileSystemAccess? _fileService;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<ConsoleReaderService>? _logger;
        private static Dictionary<string, string?>? _remoteControlIrCodes;

        private string _previousLine = "";

        public ConsoleReaderService(ConsoleDisplayDispatcher? console, IFileSystemAccess? fileService,
                                    in Dictionary<string, string?>? remoteControlIrCodes, IMemoryCache? memoryCache = null,
                                    ISignalRNotifier? signalRNotifier = null, ILogger<ConsoleReaderService>? logger = null)
        {

            _console = console ?? new ConsoleDisplayDispatcher();
            _fileService = fileService;
            _remoteControlIrCodes = remoteControlIrCodes;
            _memoryCache = memoryCache ?? new MemoryCache(new MemoryCacheOptions());
            _signalRNotifier = signalRNotifier;
            _logger = logger;
            ServerStatus = _fileService?.LoadSlingBoxServerStatusFromFile() ?? new SlingBoxServerStatus();
        }


        public async Task ParseLogLineAsync(string line)
        {
            var (isParsed, actionType, slingBoxName) = ParseConsoleLog(line);

            await DisplayConsoleLineAsync(line, isParsed);


            if (isParsed && !string.IsNullOrWhiteSpace(slingBoxName))
            {
                await ChangeSlingBoxStatusAsync(actionType, slingBoxName);
            }

            UpdatePreviousLine(line);
        }


        private void UpdatePreviousLine(string line)
        {
            // ignore irrelevant lines interposed between the relevant ones :) 
            if (line.StartsWith("GET ") ||
                line.StartsWith("Remote Control Connected") ||
                line.Contains("RemoteControl connection from "))
            {
                return;
            }

            _previousLine = line;
        }


        private async Task ChangeSlingBoxStatusAsync(string actionType, string slingBoxName)
        {
            _fileService?.SaveToJsonFile(ServerStatus);


            if (_signalRNotifier == null)
            {
                var errMsg = $"Error: Cannot send notifications, {typeof(ISignalRNotifier)} is null!";
                await _console.WriteLineAsync(errMsg);

                _logger?.LogError(errMsg);
                return;
            }

            int channelNumber;

            if (ServerStatus.SlingBoxes.TryGetValue(slingBoxName, out var slingBoxStatus))
            {
                channelNumber = slingBoxStatus.CurrentChannelNumber;
            }
            else
            {
                var errMsg = $"Error: SlingBox {slingBoxName} not found in status file !";
                await _console.WriteLineAsync(errMsg);
                _logger?.LogError(errMsg);
                return;
            }

            var cacheKey = $"SlingBoxServerStatus_{slingBoxName}_{actionType}_{channelNumber}";

            const int cacheExpirationSeconds = 5;

            switch (actionType)
            {
                case ServerAction.ChannelChanged:

                    if (!_memoryCache.TryGetValue(cacheKey, out _))
                    {
                        // cache 5 seconds: Actually it take less than 3 seconds between receiving the POST request to change the channel
                        // and the server confirmation displayed on console, so 5" will be enough to avoid sending the notification twice
                        _memoryCache.Set(cacheKey, true, TimeSpan.FromSeconds(cacheExpirationSeconds));

                        await NotifyChannelChangeAsync(slingBoxName, slingBoxStatus.CurrentChannelNumber);
                    }
                    else
                    {
                        await DisplayMessageAsync($"Cache: Action {actionType}, SlingBox {slingBoxName}, Channel {channelNumber} already notified", true);
                    }
                    break;


                case ServerAction.StreamingInProgress:

                    var cacheKeyChannelChanged = $"SlingBoxServerStatus_{slingBoxName}_{ServerAction.ChannelChanged}_{channelNumber}";
                    if (!_memoryCache.TryGetValue(cacheKey, out _) &&
                        !_memoryCache.TryGetValue(cacheKeyChannelChanged, out _)) // If channel changed recently, then streaming in progress is implied
                    {
                        _memoryCache.Set(cacheKey, true, TimeSpan.FromSeconds(cacheExpirationSeconds));
                        await NotifyStreamingInProgressAsync(slingBoxName);
                    }
                    else
                    {
                        await DisplayMessageAsync($"Cache: Action {actionType}, SlingBox {slingBoxName}, Channel {channelNumber} already notified", true);
                    }
                    break;


                case ServerAction.StreamingStopped:
                    if (!_memoryCache.TryGetValue(cacheKey, out _))
                    {
                        _memoryCache.Set(cacheKey, true, TimeSpan.FromSeconds(cacheExpirationSeconds));
                        await NotifyStreamingStoppedAsync(slingBoxName);
                    }
                    else
                    {
                        await DisplayMessageAsync($"Cache: Action {actionType}, SlingBox {slingBoxName}, Channel {channelNumber} already notified", true);
                    }
                    break;

                case ServerAction.ErrorSlingBoxBricked:
                    if (!_memoryCache.TryGetValue(cacheKey, out _))
                    {
                        _memoryCache.Set(cacheKey, true, TimeSpan.FromSeconds(cacheExpirationSeconds));
                        await NotifySlingBoxBrickedAsync(slingBoxName);
                    }
                    else
                    {
                        await DisplayMessageAsync($"Cache: Action {actionType}, SlingBox {slingBoxName}, Channel {channelNumber} already notified", true);
                    }
                    break;

                case ServerAction.ErrorRemoteLocked:
                    if (!_memoryCache.TryGetValue(cacheKey, out _))
                    {
                        _memoryCache.Set(cacheKey, true, TimeSpan.FromSeconds(cacheExpirationSeconds));
                        await NotifyRemoteLockedAsync(slingBoxName);
                    }
                    else
                    {
                        await DisplayMessageAsync($"Cache: Action {actionType}, SlingBox {slingBoxName}, Channel {channelNumber} already notified", true);
                    }
                    break;


                default:
                    await DisplayMessageAsync($"Error: Action {actionType} not recognized", true);
                    break;
            }
        }


        private const string NullAction = "";
        private const string NullSlingBoxName = "";


        private (bool isParsed, string action, string slingBoxName) ParseConsoleLog(string line)
        {

            if (string.IsNullOrWhiteSpace(line))
                return (false, NullAction, NullSlingBoxName);


            if (line.StartsWith("BOXES [('") && ServerStatus.SlingBoxesCount == 0)
            {
                AddSlingBoxes(line);

                return (true, NullAction, NullSlingBoxName);
            }

  

            if (line.Contains("got ProHD"))
            {
                var slingBoxName = UpdateSlingBoxChannelAnalogue(line);

                return (true, ServerAction.ChannelChanged, slingBoxName);
            }

            // When 'StartChannel' is defined in config.ini
            // .12/11/2023, 13:35:28.949  slingBox2 Sending Start Channel 821
            if (line.Contains("Sending Start Channel"))
            {
                var slingBoxName = GetSlingboxNameFromLogWhenThirdInLine(line);
                var channelStr = line.Split("Sending Start Channel", StringSplitOptions.RemoveEmptyEntries)[^1];
                if (!string.IsNullOrWhiteSpace(channelStr))
                {
                    // analogue channels contain ".", but removing it may not be OK when "." is inside the channel number
                    channelStr = channelStr.Replace(".", "");
                }
                else
                {
                    return (false, NullAction, NullSlingBoxName);
                }

                if (!int.TryParse(channelStr, out var channelNumber))
                {
                    return (false, NullAction, NullSlingBoxName);
                }

                var slingBoxStatus = ServerStatus.GetSlingBoxStatus(slingBoxName);

                if (slingBoxStatus.IsAnalogue)
                {
                    UpdateSlingBoxChannelDigital(slingBoxName, channelNumber, line);
                }
                else
                {
                    UpdateSlingBoxChannelAnalogue(slingBoxName, channelNumber, line);
                }

                return (true, ServerAction.ChannelChanged, slingBoxName);
            }

            // The following 2 lines are always sent in sequence, but NOT ALWAYS consecutively:
            // Sending Channel Digits 1111
            // ...<sometimes other lines in between which render this method unusable>...
            // slingBox2 Got Streamer Control Message IR
            if (line.Contains("Got Streamer Control Message IR") && _previousLine.StartsWith("Sending Channel Digits"))
            {
                var slingBoxName = UpdateSlingBoxChannelDigital(line, _previousLine);

                return slingBoxName == NullSlingBoxName
                    ? (false, NullAction, NullSlingBoxName)
                    : (true, ServerAction.ChannelChanged, slingBoxName);
            }



            if (line.StartsWith("IR [b") && !_previousLine.Contains("Got Streamer Control Message IR"))
            {
                _console.WriteLine($"***************************\nPrevious line does not contain 'Got Streamer Control Message IR\"' \n It is {_previousLine}\n***************************");
            }

            // The following 2 lines are always sent in sequence, but NOT ALWAYS consecutively:
            // slingBox2 Got Streamer Control Message IR
            // ...<sometimes other lines in between which render this method unusable>...
            // IR [b'\x10192.168.1.127', b'\x0b192.168.1.127', b'\t192.168.1.127']
            if (line.StartsWith("IR [b") && _previousLine.Contains("Got Streamer Control Message IR"))
            {
                var slingBoxName = UpdateSlingBoxChannelDigital(line, _previousLine, true);

                return slingBoxName == NullSlingBoxName
                    ? (false, NullAction, NullSlingBoxName)
                    : (true, ServerAction.ChannelChanged, slingBoxName);
            }


            // Ch+:
            // 10/31/2023, 17:05:01.734  slingBox2 Sending IR keycode 4 1 for 192.168.1.10
            // Ch-:
            // 10/31/2023, 17:07:56.360  slingBox2 Sending IR keycode 5 1 for 192.168.1.10
            // Last:
            // 10/31/2023, 17:09:13.031  slingBox2 Sending IR keycode 56 1 for 192.168.1.10
            if (line.Contains("Sending IR keycode"))
            {
                var slingBoxName = UpdateSlingBoxChannelDigital(line);

                return slingBoxName == NullSlingBoxName
                    ? (false, NullAction, NullSlingBoxName)
                    : (true, ServerAction.ChannelChanged, slingBoxName);
            }


            if (line.Contains("Selecting Video Source") ||
                line.Contains("Got Streamer Control Message") ||
                line.Contains("New Stream Starting") ||
                line.Contains("Stream started at"))
            {
                var slingBoxName = UpdateSlingBoxLastHeartBeat1(line);

                return (true, ServerAction.StreamingInProgress, slingBoxName);
            }

            if (line.StartsWith(".") && line.Contains("Clients"))
            {
                var slingBoxName = UpdateSlingBoxLastHeartBeat2(line);

                return (true, ServerAction.StreamingInProgress, slingBoxName);
            }


            if (line.Contains("Shutting down connections") ||
                line.Contains("Logging Out") ||
                line.Contains("Giving up. Sorry"))
            {
                var slingBoxName = DetectStreamingStopped(line);

                return (true, ServerAction.StreamingStopped, slingBoxName);
            }

            // slingBox2 Error Starting Session. Slingbox might be Bricked
            if (line.Contains("Error Starting Session. Slingbox might be Bricked"))
            {
                var slingBoxName = GetSlingboxNameFromLogWhenFirstInLine(line);

                return (true, ServerAction.ErrorSlingBoxBricked, slingBoxName);
            }

            // slingBox2 Ignoring IR request from 192.168.1.121 Remote Locked by 192.168.1.10
            if (line.Contains("Ignoring IR request from") && line.Contains("Remote Locked"))
            {
                var slingBoxName = GetSlingboxNameFromLogWhenFirstInLine(line);

                return (true, ServerAction.ErrorRemoteLocked, slingBoxName);
            }

            return (false, NullAction, NullSlingBoxName);
        }

        
        private static string GetSlingboxNameFromLogWhenFirstInLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return NullSlingBoxName;
            }

            var segments = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2)
            {
                return NullSlingBoxName;
            }

            var slingBoxName = segments[0];

            if (!string.IsNullOrWhiteSpace(slingBoxName))
            {
                slingBoxName = slingBoxName.Replace(".", "");
            }

            return slingBoxName;
        }


        // .....08/22/2023,22:11:54 slingBox2 1 Clients.192.168.1.10:63984
        private static string GetSlingboxNameWhenSecondInLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return NullSlingBoxName;
            }

            var segments = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2)
            {
                return NullSlingBoxName;
            }

            var slingBoxName = segments[1];

            if (!string.IsNullOrWhiteSpace(slingBoxName))
            {
                slingBoxName = slingBoxName.Replace(".", "");
            }

            return slingBoxName;
        }


        // Ch+:
        // 10/31/2023, 17:05:01.734  slingBox2 Sending IR keycode 4 1 for 192.168.1.10
        // Ch-:
        // 10 / 31 / 2023, 17:07:56.360  slingBox2 Sending IR keycode 5 1 for 192.168.1.10
        // Last:
        // 10/31/2023, 17:09:13.031  slingBox2 Sending IR keycode 56 1 for 192.168.1.10
        private static string GetSlingboxNameFromLogWhenThirdInLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return NullSlingBoxName;
            }

            var segments = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 3)
            {
                return NullSlingBoxName;
            }

            var slingBoxName = segments[2];

            if (!string.IsNullOrWhiteSpace(slingBoxName))
            {
                slingBoxName = slingBoxName.Replace(".", "");
            }

            return slingBoxName;
        }




        //BOXES [('sb1', 'slingbox1'), ('sb2', 'slingbox2'), ('sb3', 'slingbox3')]
        private void AddSlingBoxes(string line)
        {
            var keyValuePairs = ExtractKeyValuePairs(line);

            foreach (var (slingBoxId, slingBoxName) in keyValuePairs)
            {
                ServerStatus.AddSlingBox(slingBoxName, slingBoxId);
            }
        }

        private static List<KeyValuePair<string, string>> ExtractKeyValuePairs(string inputString)
        {
            var keyValuePairs = new List<KeyValuePair<string, string>>();

            var cleanedString = inputString.Replace("BOXES [", "").Replace("]", "").Replace("'", "");
            var pairs = cleanedString.Split(new[] { "(", "),", ")" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var pair in pairs)
            {
                string[] parts = pair.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 2)
                    continue;

                var key = parts[0];
                var value = parts[1];

                keyValuePairs.Add(new KeyValuePair<string, string>(key, value));
            }

            return keyValuePairs;
        }




        // slingBox2 Shutting down connections
        // slingBox2 Logging Out
        // .slingBox2 Giving up. Sorry...
        private string DetectStreamingStopped(string line)
        {
            line = line.Replace(".", "");
            var slingBoxName = GetSlingboxNameFromLogWhenFirstInLine(line);
            if (string.IsNullOrWhiteSpace(slingBoxName) || slingBoxName.Equals(NullSlingBoxName))
                return NullSlingBoxName;

            if (!ServerStatus.SlingBoxes.TryGetValue(slingBoxName, out var slingBoxStatus))
                return NullSlingBoxName;

            slingBoxStatus.UpdateLastHeartBeatTimeStamp(null);

            DisplayMessageAsync($"Debug: Streaming stopped for {slingBoxName}, log line: {line}").GetAwaiter().GetResult();

            return slingBoxName;
        }

        
        // slingBox2 Selecting Video Source 0
        // slingBox2 Got Streamer Control Message ProHD
        //...slingBox2 Got Streamer Control Message ProHD
        // slingBox2 New Stream Starting 0
        // slingBox2 Stream started at 08/23/2023, 21:52:29.921  732 0
        private string UpdateSlingBoxLastHeartBeat1(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return NullSlingBoxName;

            if (line.StartsWith(".")) line = line.Replace(".", "");

            var slingBoxName = GetSlingboxNameFromLogWhenFirstInLine(line);

            if (!ServerStatus.SlingBoxes.TryGetValue(slingBoxName, out var slingBoxStatus))
                return NullSlingBoxName;

            slingBoxStatus.UpdateLastHeartBeatTimeStamp(DateTime.Now);

            DisplayMessageAsync($"Debug: Streaming in progress for {slingBoxName}, log line: {line}").GetAwaiter().GetResult();

            return slingBoxName;
        }

        
        // .....08/22/2023,22:11:54 slingBox2 1 Clients.192.168.1.10:63984
        private string UpdateSlingBoxLastHeartBeat2(string line)
        {
            var slingBoxName = GetSlingboxNameWhenSecondInLine(line);

            if (!ServerStatus.SetSlingBoxLastHeartBeatTimeStamp(slingBoxName, DateTime.Now))
            {
                return NullSlingBoxName;
            }

            DisplayMessageAsync($"Debug: Streaming in progress  for {slingBoxName}, log line: {line}").GetAwaiter().GetResult();

            return slingBoxName;
        }


        // 07/10/2023, 14:34:21.164  slingBox2 got ProHD 2.0.0 192.168.1.10
        private string UpdateSlingBoxChannelAnalogue(string line)
        {
            var (slingBoxName, commandType, channelNumber, slingUserIp) = GetAnalogueChannelChangeInfo(line);

            UpdateSlingBoxChannelAnalogue(slingBoxName, channelNumber, commandType, line, slingUserIp);

            return slingBoxName;
        }

        
        private void UpdateSlingBoxChannelAnalogue(string slingBoxName, int channelNumber, int commandType, string line, string slingUserIp)
        {
            var remoteControlHandler = new AnalogRemoteControlHandler
            {
                SlingBoxName = slingBoxName,
                CommandType = commandType,
                Channel = channelNumber
            };

            remoteControlHandler.UpdateSlingBoxStatus(ServerStatus);

            DisplayMessageAsync($"Debug: Analogue Channel changed for {slingBoxName}, channel: {channelNumber}, userIP: {slingUserIp} log line: {line}").GetAwaiter().GetResult();
        }
        

        private void UpdateSlingBoxChannelAnalogue(string slingBoxName, int channelNumber, string line)
        {
            const int commandType = (int)AnalogRemoteControlHandler.AnalogRemoteControlCommandType.ChangeChannel;
            const string slingUserIp = "";

            UpdateSlingBoxChannelAnalogue(slingBoxName, channelNumber, commandType, line, slingUserIp);
        }


        private (string slingName, int commandType, int channelNumber, string slingUserIp) GetAnalogueChannelChangeInfo(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return (NullSlingBoxName, 0, 0, "");

            var segments = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 7)
                return (NullSlingBoxName, 0, 0, "");

            var slingBoxName = segments[2];

            if (!ServerStatus.SlingBoxes.ContainsKey(slingBoxName))
                return (NullSlingBoxName, 0, 0, "");

            var remoteCommand = segments[5]; // "2.0.0"
            var slingUserIp = segments[6];

            var remoteCommandParts = remoteCommand.Split(".");
            if (remoteCommandParts.Length < 3)
                return (NullSlingBoxName, 0, 0, "");

            var commandType = int.Parse(remoteCommandParts[0]);
            if (!int.TryParse(remoteCommandParts[1], out var channel))
            {
                _console.WriteLine($"Debug: Channel changed for {slingBoxName}, channel: {channel}, log line: {line}");

                return (NullSlingBoxName, 0, 0, "");
            }

            return (slingBoxName, commandType, channel, slingUserIp);
        }

        internal readonly struct AnalogRemoteControlHandler
        {
            internal enum AnalogRemoteControlCommandType
            {
                ChannelUp = 0,
                ChannelDown = 1,
                ChangeChannel = 2,
                LastChannel = 3
            }

            public string SlingBoxName { get; init; }
            public int CommandType { get; init; }
            public int Channel { get; init; }

            public void UpdateSlingBoxStatus(SlingBoxServerStatus serverStatus)
            {
                if (!serverStatus.SlingBoxes.TryGetValue(SlingBoxName, out var slingBoxStatus))
                    return;

                switch (CommandType)
                {
                    case (int)AnalogRemoteControlCommandType.ChannelUp:
                        slingBoxStatus.ChannelUp();
                        break;

                    case (int)AnalogRemoteControlCommandType.ChannelDown:
                        slingBoxStatus.ChannelDown();
                        break;

                    case (int)AnalogRemoteControlCommandType.ChangeChannel:
                        slingBoxStatus.ChangeChannel(Channel);
                        break;

                    case (int)AnalogRemoteControlCommandType.LastChannel:
                        slingBoxStatus.SelectLastChannel();
                        break;

                    default:
                        throw new ArgumentOutOfRangeException($"Analogue Remote control command type unexpected: {CommandType}. " +
                                                              $"Should be: {string.Join(",", Enum.GetNames(typeof(AnalogRemoteControlCommandType)))}");

                }
            }
        }



        // Ch+:
        // 10/31/2023, 17:05:01.734  slingBox2 Sending IR keycode 4 1 for 192.168.1.10
        // Ch-:
        // 10 / 31 / 2023, 17:07:56.360  slingBox2 Sending IR keycode 5 1 for 192.168.1.10
        // Last:
        // 10/31/2023, 17:09:13.031  slingBox2 Sending IR keycode 56 1 for 192.168.1.10
        private string UpdateSlingBoxChannelDigital(string line)
        {

            var slingBoxName = GetSlingboxNameFromLogWhenThirdInLine(line);

            if (string.IsNullOrWhiteSpace(slingBoxName))
            {
                return NullSlingBoxName;
            }

            var (commandType, channelNumber) = GetDigitalChannelChangeInfo(line);

            if (commandType == DigitalRemoteControlCommandType.Unknown)
            {
                return NullSlingBoxName;
            }

            //var remoteControlHandler = new DigitalRemoteControlHandler
            //{
            //    SlingBoxName = slingBoxName,
            //    CommandType = commandType,
            //    Channel = channelNumber
            //};

            //remoteControlHandler.UpdateSlingBoxStatus(ServerStatus);

            UpdateSlingBoxChannelDigital(slingBoxName, channelNumber, commandType, line);


            return slingBoxName;
        }


        private void UpdateSlingBoxChannelDigital(string slingBoxName, int channelNumber, string line)
        {
            var commandType = DigitalRemoteControlCommandType.ChangeChannel;

            UpdateSlingBoxChannelDigital(slingBoxName, channelNumber, commandType, line);
        }

        private void UpdateSlingBoxChannelDigital(string slingBoxName, int channelNumber, int commandType, string line)
        {

            var remoteControlHandler = new DigitalRemoteControlHandler
            {
                SlingBoxName = slingBoxName,
                CommandType = commandType,
                Channel = channelNumber
            };

            remoteControlHandler.UpdateSlingBoxStatus(ServerStatus);

            DisplayMessageAsync($"Debug: Digital Channel changed for {slingBoxName}, channel: {channelNumber}, log line: {line}").GetAwaiter().GetResult();
        }


        //Sending Channel Digits 1111
        //slingBox2 Got Streamer Control Message IR
        //
        //Remote Last 56
        //slingBox2 Got Streamer Control Message IR
        //
        //Remote Ch+ 4
        //slingBox2 Got Streamer Control Message IR
        //
        //Remote Ch- 5
        //slingBox2 Got Streamer Control Message IR
        private string UpdateSlingBoxChannelDigital(string line, string previousLine, bool fromSpecialChars = false)
        {
            var slingBoxName = GetSlingboxNameFromLogWhenFirstInLine(!fromSpecialChars ? line : previousLine);

            if (string.IsNullOrWhiteSpace(slingBoxName))
            {
                return NullSlingBoxName;
            }


            int commandType, channelNumber;

            if (!fromSpecialChars)
            {
                (commandType, channelNumber) = GetDigitalChannelChangeInfo(previousLine);
            }
            else
            {
                (commandType, channelNumber) = GetDigitalChannelNumberFromHex(line);
            }

            if (commandType == DigitalRemoteControlCommandType.Unknown)
            {
                return NullSlingBoxName;
            }


            var remoteControlHandler = new DigitalRemoteControlHandler
            {
                SlingBoxName = slingBoxName,
                CommandType = commandType,
                Channel = channelNumber
            };

            remoteControlHandler.UpdateSlingBoxStatus(ServerStatus);

            DisplayMessageAsync($"Debug: Digital Channel changed for {slingBoxName}, channel: {channelNumber}, log line: {line}").GetAwaiter().GetResult();

            return slingBoxName;
        }

        private static (int commandType, int channelNumber) GetDigitalChannelNumberFromHex(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return (DigitalRemoteControlCommandType.Unknown, -1);
            if (!line.StartsWith("IR [b'")) return (DigitalRemoteControlCommandType.Unknown, -1);

            line = line.Replace("IR [", "").Replace("]", "")
                       .Replace(@"b'\", "").Replace("'", "").Trim();

            var inputEncodedDigits = line.Split(",", StringSplitOptions.RemoveEmptyEntries);

            if (inputEncodedDigits.Length < 1) return (DigitalRemoteControlCommandType.Unknown, -1);


            var foundMatch = false;

            // sling uses a custom encoding for digits 0-9 
            var slingEncodedDigits = new[] { "x12", "t", "n", "x0b", "x0c", "r", "x0e", "x0f", "x10", "x11" };

            var channelNumberTemp = new StringBuilder();
            foreach (var encodedDigit in inputEncodedDigits)
            {
                var cleanEncodedDigit = RemoveNonAsciiChars(encodedDigit).Trim();

                foundMatch = false;
                for (var i = 0; i < slingEncodedDigits.Length; i++)
                {
                    if (!cleanEncodedDigit.StartsWith(slingEncodedDigits[i]))
                        continue;

                    channelNumberTemp.Append(i);
                    foundMatch = true;
                    break;
                }

                if (!foundMatch)
                    break;
            }


            if (foundMatch && int.TryParse(channelNumberTemp.ToString(), out int channelNumber))
            {
                return (DigitalRemoteControlCommandType.ChangeChannel, channelNumber);
            }


            return (DigitalRemoteControlCommandType.Unknown, -1);
        }


        private static string RemoveNonAsciiChars(string input)
        {
            // Using regular expression to match non-ASCII characters
            string pattern = @"[^\x20-\x7E]";
            string cleanString = Regex.Replace(input, pattern, "");

            return cleanString;
        }

        private static (int commandType, int channelNumber) GetDigitalChannelChangeInfo(string line)
        {
            const string channelUp = "Remote Ch+";
            var channelUpKey = $"sending key {_remoteControlIrCodes?["Ch+"]} ";
            var channelUpKeyIR = $"Sending IR keycode {_remoteControlIrCodes?["Ch+"]} ";

            const string channelDown = "Remote Ch-";
            var channelDownKey = $"sending key {_remoteControlIrCodes?["Ch-"]} ";
            var channelDownKeyIR = $"Sending IR keycode {_remoteControlIrCodes?["Ch-"]} ";

            const string lastChannel = "Remote Last";
            var lastChannelKey = $"sending key {_remoteControlIrCodes?["Last"]} ";
            var lastChannelKeyIR = $"Sending IR keycode {_remoteControlIrCodes?["Last"]} ";

            const string changeChannel = "Sending Channel Digits";


            if (line.StartsWith(changeChannel))
            {
                var channelStr = line.Replace(changeChannel, "").Trim();
                if (int.TryParse(channelStr, out var channelNumber))
                {
                    return (DigitalRemoteControlCommandType.ChangeChannel, channelNumber);
                }
            }

            if (line.StartsWith(channelUp) || line.StartsWith(channelUpKey) || line.Contains(channelUpKeyIR))
            {
                return (DigitalRemoteControlCommandType.ChannelUp, -1);
            }

            if (line.StartsWith(channelDown) || line.StartsWith(channelDownKey) || line.Contains(channelDownKeyIR))
            {
                return (DigitalRemoteControlCommandType.ChannelDown, -1);
            }

            if (line.StartsWith(lastChannel) || line.StartsWith(lastChannelKey)

                                             || line.Contains(lastChannelKeyIR))
            {
                return (DigitalRemoteControlCommandType.LastChannel, -1);
            }

            return (DigitalRemoteControlCommandType.Unknown, -1);
        }


        private readonly struct DigitalRemoteControlCommandType
        {
            public static int Unknown => -1;
            public static int ChannelUp => int.Parse(_remoteControlIrCodes?["Ch+"] ?? "-1");
            public static int ChannelDown => int.Parse(_remoteControlIrCodes?["Ch-"] ?? "-1");
            public static int ChangeChannel => -2000;
            public static int LastChannel => int.Parse(_remoteControlIrCodes?["Last"] ?? "-1");

            public static IEnumerable<int> AllValues => new[] { ChannelUp, ChannelDown, ChangeChannel, LastChannel };
        }

        
        private readonly struct DigitalRemoteControlHandler
        {
            public string SlingBoxName { get; init; }
            public int CommandType { get; init; }
            public int Channel { get; init; }


            public void UpdateSlingBoxStatus(SlingBoxServerStatus serverStatus)
            {
                if (!serverStatus.SlingBoxes.TryGetValue(SlingBoxName, out var slingBoxStatus))
                    throw new ArgumentOutOfRangeException($"SlingBox [{SlingBoxName}] not found in status file !");


                if (CommandType == DigitalRemoteControlCommandType.ChannelUp)
                {
                    slingBoxStatus.ChannelUp();
                }
                else if (CommandType == DigitalRemoteControlCommandType.ChannelDown)
                {
                    slingBoxStatus.ChannelDown();
                }
                else if (CommandType == DigitalRemoteControlCommandType.ChangeChannel)
                {
                    slingBoxStatus.ChangeChannel(Channel);
                }
                else if (CommandType == DigitalRemoteControlCommandType.LastChannel)
                {
                    slingBoxStatus.SelectLastChannel();
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"Digital Remote control command type unexpected: {CommandType}. " +
                                                          $"Should be: {string.Join(",", DigitalRemoteControlCommandType.AllValues)}");
                }
            }
        }
        



        private async Task NotifyChannelChangeAsync(string slingBoxName, int channelNumber)
        {
            if (_signalRNotifier == null) return;

            var notification = new ChannelChangedNotification(slingBoxName, channelNumber, "server");
            await _signalRNotifier.NotifyClients(notification);
        }

        private async Task NotifyStreamingStoppedAsync(string slingBoxName)
        {
            if (_signalRNotifier == null) return;
            await DisplayMessageAsync("Wrapper: NotifyStreamingStoppedAsync");

            var notification = new StreamingStoppedNotification(slingBoxName, "server");
            await _signalRNotifier.NotifyClients(notification);
        }

        private async Task NotifyStreamingInProgressAsync(string slingBoxName)
        {
            if (_signalRNotifier == null) return;

            var notification = new StreamingInProgressNotification(slingBoxName, "server");
            await _signalRNotifier.NotifyClients(notification);
        }

        private async Task NotifySlingBoxBrickedAsync(string slingBoxName)
        {
            if (_signalRNotifier == null) return;

            var notification = new SlingBoxBrickedNotification(slingBoxName, "server");
            await _signalRNotifier.NotifyClients(notification);
        }

        private async Task NotifyRemoteLockedAsync(string slingBoxName)
        {
            if (_signalRNotifier == null) return;

            var notification = new RemoteLockedNotification(slingBoxName, "server");
            await _signalRNotifier.NotifyClients(notification);
        }





        private async Task DisplayConsoleLineAsync(string line, bool isParsed = false)
        {
            using (await _console.GetLockAsync())
            {
                if (isParsed)
                {
                    DisplayParsedConsoleLine(line);
                }
                else
                {
                    DisplayRegularConsoleLine(line);
                }
            }
        }

        private void DisplayParsedConsoleLine(string line)
        {
            var currentFontColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(line.Trim());

            Console.ResetColor();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(ServerStatus.ToString());

            Console.ForegroundColor = currentFontColor;
        }

        private static void DisplayRegularConsoleLine(string line)
        {
            var currentFontColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(line.Trim());
            Console.ForegroundColor = currentFontColor;
        }

        private async Task DisplayMessageAsync(string message, bool isError = false)
        {
            //lock (LockConsole)
            using (await _console.GetLockAsync())
            {
                var fontColor = Console.ForegroundColor;
                Console.ForegroundColor = isError
                    ? ConsoleColor.Red
                    : ConsoleColor.Cyan;
                Console.WriteLine("-----------------------------------------------");
                Console.WriteLine($"{message}");
                Console.WriteLine("-----------------------------------------------");
                Console.ForegroundColor = fontColor;
            }
        }

    }
}
