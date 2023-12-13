using Domain.Models;
using RunSlingServer.Services;
using RunSlingServer.WebApi.Services.Interfaces;
using System.Text;
using Application.SignalRServices;
using Application.SignalRServices.Notifications;
using Application.Abstractions;
using Application.Interfaces;


namespace RunSlingServer.WebApi.Services.EndpointsServices
{
    /// <summary>
    /// Forwards POST requests to Slinger server.
    /// It also check if the server has stopped streaming (workaround for Slinger 4.01).
    /// </summary>
    public class PostToUrlHandler : IPostToUrlHandler
    {
        private readonly IConsoleDisplayDispatcher _console;
        private readonly IFileSystemAccess _fileSystemAccess;
        private readonly ISignalRNotifier? _signalRNotifier;
        private readonly ILogger _logger;
        private readonly IWebHelpers _webHelper;

        public PostToUrlHandler(IConsoleDisplayDispatcher console, IFileSystemAccess fileSystemAccess, ISignalRNotifier? signalRNotifier, ILogger logger, IWebHelpers webHelper)
        {
            _console = console;
            _fileSystemAccess = fileSystemAccess;
            _signalRNotifier = signalRNotifier;

            _logger = logger;
            _webHelper = webHelper;
        }

        public async Task<string> HandlePostToUrl(HttpRequest request)
        {
            var clientIp = WebHelpers.GetClientIp(request);

            var (uriAddress, isValidUrl, errorMessage) = WebHelpers.ValidateUrl(request);

            if (!isValidUrl || uriAddress == null)
            {
                await DisplayMessage(errorMessage, true); _logger.LogError(errorMessage);
                request.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

                return await Task.FromResult(errorMessage);
            }

            var slingBoxName = uriAddress.Segments[^1];

            const string digitsKey = "Digits";
            var keyValuePairs = await WebHelpers.GetSlingChannelChangeDataFromRequest(request);
            var channelNumber = keyValuePairs.FirstOrDefault(kv =>
                                                    string.Equals(kv.Key, digitsKey, StringComparison.CurrentCultureIgnoreCase)).Value;

            await DisplayAndLogChannelChangeRequest(slingBoxName, channelNumber, clientIp);

            var serverStatus = _fileSystemAccess.LoadSlingBoxServerStatusFromFile();
            var slingBoxStatus = serverStatus?.GetSlingBoxStatus(slingBoxName);

            if (serverStatus == null || slingBoxStatus == null)
            {
                errorMessage = $"WebApi: SlingBox '{slingBoxName}' not found";
                request.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

                return await Task.FromResult(errorMessage);
            }

            var msg = $"WebApi Post: SlingBox '{slingBoxName}' found, current channel {slingBoxStatus.CurrentChannelNumber}";
            await DisplayMessage(msg);
            _logger.LogInformation(msg);

            // Append "." to channel number if the SlingBox uses an analog tuner
            channelNumber = UpdateChannelNumberWithAnalogueSetting(slingBoxStatus, channelNumber, keyValuePairs, digitsKey);

            var url = uriAddress.ToString();



            // This response contains the HTML remote control form from slingbox server.
            // If the MAGIC STRING 'Status:%s' is included in the form, it will include server status.
            var postResponse = await _webHelper.PostToSlingerServer(url, keyValuePairs, request, _logger);


            if (IsResponseError(out var statusCode))
            {
                var inputData = GetRequestBody();
                errorMessage = $"WebApi Post: SlingBox {slingBoxName} returned error: {postResponse}.\n Input data {inputData}";
                await DisplayMessage(errorMessage, true);
                _logger.LogError(errorMessage);

                request.HttpContext.Response.StatusCode = statusCode;

                return await Task.FromResult(errorMessage);
            }


            if (string.IsNullOrWhiteSpace(postResponse))
            {
                var inputData = GetRequestBody();
                errorMessage = $"WebApi Post: SlingBox {slingBoxName} not responding or not found in status file.\n Input data {inputData}";
                await DisplayMessage(errorMessage, true);
                _logger.LogError(errorMessage);

                request.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

                return await Task.FromResult(errorMessage);
            }


            if (IsSlingStopped())
            {
                msg = $"WebApi Post: SlingBox {slingBoxName} is STOPPED";
                await DisplayMessage(msg); _logger.LogInformation(msg);
                await NotifyStreamingStoppedAsync(slingBoxName);

                _fileSystemAccess.SaveToJsonFile(serverStatus);
                Thread.Sleep(1000);

                serverStatus.SetServerStreamingStopped();

                return await Task.FromResult(msg);
            }

            if (IsChannelSelected())
            {
                msg = $"WebApi Post: SlingBox {slingBoxName} already on channel {channelNumber}";
                await DisplayMessage(msg); _logger.LogInformation(msg);

                return await Task.FromResult(msg);
            }

            msg = $"WebApi Post: SlingBox {slingBoxName} on channel {channelNumber}";

            return await Task.FromResult(msg);




            string GetRequestBody()
            {
                var sb = new StringBuilder();
                foreach (var keyValuePair in keyValuePairs)
                {
                    sb.Append($"{keyValuePair.Key}={keyValuePair.Value}&");
                }

                return sb.ToString().TrimEnd('&');
            }

            bool IsChannelSelected()
            {
                return slingBoxStatus.CurrentChannelNumber.ToString() == channelNumber.Replace(".", "");
            }

            // Workaround for Slinger 4.01 where the last server message cannot be read
            // from console because it is not followed by a newline
            bool IsSlingStopped()
            {
                return postResponse.Contains("Waiting for first client. Slingbox at") ||
                       postResponse.Contains("Can't find a slingbox on network");
            }

            bool IsResponseError(out int responseStatusCode)
            {

                if (postResponse.Contains("500 Internal Server Error"))
                {
                    responseStatusCode = StatusCodes.Status500InternalServerError;
                    return true;
                }

                if (postResponse.Contains("Error"))
                {
                    responseStatusCode = StatusCodes.Status500InternalServerError;
                    return true;
                }

                responseStatusCode = StatusCodes.Status200OK;
                return false;
            }
        }




        private async Task NotifyStreamingStoppedAsync(string slingBoxName)
        {
            if (_signalRNotifier == null) return;

            var notification = new StreamingStoppedNotification(slingBoxName, "server");
            await _signalRNotifier.NotifyClients(notification);
        }


        private static string UpdateChannelNumberWithAnalogueSetting(SlingBoxStatus slingBoxStatus, string channelNumber, IDictionary<string, string> keyValuePairs, string digitsKey)
        {
            if (string.IsNullOrWhiteSpace(channelNumber))
                return string.Empty;


            if (slingBoxStatus.IsAnalogue)
            {
                /* Analogue tuners need a decimal point in the channel number to work:
                 * You need to add a decimal point to the channel number so the code can tell the difference between IR and internal tuner.
                 *  - So in the US you can say 4.0 or 9.1 for subchannels.
                 *  - In the UK just put in the channel number with a decimal 101.
                 */

                if (channelNumber.Count(c => c == '.') == 1 && !channelNumber.StartsWith("."))
                    return channelNumber;

                channelNumber = channelNumber.Replace(".", "");
                channelNumber += ".";
                keyValuePairs[digitsKey] = channelNumber;
            }
            else
            {
                if (!channelNumber.Contains('.'))
                    return channelNumber;

                channelNumber = channelNumber.Replace(".", "");
                keyValuePairs[digitsKey] = channelNumber;
            }

            return channelNumber;
        }


        private async Task DisplayAndLogChannelChangeRequest(string slingBoxName, string channelNumber, string clientIp)
        {
            string msg;
            msg =
                $"WebApi Post: Request channel change for SlingBox '{slingBoxName}', new channel {channelNumber}, from IP {clientIp}";
            await DisplayMessage(msg);
            _logger.LogInformation(msg);
        }


        private async Task DisplayMessage(string message, bool isError = false)
        {
            //lock (LockConsole)
            using (await _console.GetLockAsync())
            {
                var fontColor = Console.ForegroundColor;
                Console.ForegroundColor = isError
                    ? ConsoleColor.Red
                    : ConsoleColor.Blue;
                Console.WriteLine("-----------------------------------------------");
                Console.WriteLine($"{message}");
                Console.WriteLine("-----------------------------------------------");
                Console.ForegroundColor = fontColor;
            }
        }
    }

}
