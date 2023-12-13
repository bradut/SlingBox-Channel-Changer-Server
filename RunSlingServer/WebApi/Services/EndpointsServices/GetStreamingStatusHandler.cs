using Domain.Helpers;
using Domain.Models;
using Microsoft.Extensions.Primitives;
using RunSlingServer.Helpers;
using Domain;
using RunSlingServer.Services;
using RunSlingServer.WebApi.Services.Interfaces;
using Application.Abstractions;
using Application.Interfaces;


namespace RunSlingServer.WebApi.Services.EndpointsServices
{
    public class GetStreamingStatusHandler : IGetStreamingStatusHandler
    {
        private readonly IFileSystemAccess _fileSystemAccess;
        private readonly IConsoleDisplayDispatcher _console;
        private readonly ILogger _logger;

        public GetStreamingStatusHandler(IConsoleDisplayDispatcher console, IFileSystemAccess fileSystemAccess,
            ILogger logger)
        {
            _fileSystemAccess = fileSystemAccess;
            _logger = logger;
            _console = console;
        }

        public async Task<string> GetStreamingStatus(HttpContext context)
        {
            const string slingBoxNameParameterName = "slingBoxName";

            if (!context.Request.Query.ContainsKey(slingBoxNameParameterName))
            {
                const string errorMessage = $"WebApi Get: Missing '{slingBoxNameParameterName}' parameter";
                _logger.LogError(errorMessage);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync(errorMessage);

                return string.Empty;
            }

            var serverStatus = _fileSystemAccess.LoadSlingBoxServerStatusFromFile();
            if (serverStatus == null)
            {
                const string errorMessage = "WebApi Get: Server status could not be loaded";
                _logger.LogError(errorMessage);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync(errorMessage);

                return string.Empty;
            }

            var slingBoxesNames = context.Request.Query[slingBoxNameParameterName];
            var serializedBoxesStatusToJson = GetSlingBoxesStatusAsJson(serverStatus, slingBoxesNames);
            var ip = WebHelpers.GetClientIp(context.Request);

            await DisplayAndLogStreamingStatus(slingBoxesNames, ip, serializedBoxesStatusToJson);

            context.Response.StatusCode = StatusCodes.Status200OK;

            return serializedBoxesStatusToJson;
        }


        private async Task DisplayAndLogStreamingStatus(StringValues slingBoxesNames, string ip, string serializedBoxesStatusToJson)
        {
            var msg = $"WebApi Get: Request: streaming status for SlingBoxes '{slingBoxesNames}', IP {ip}, at {DateTime.Now} " +
                      $"\nResponse:\n{serializedBoxesStatusToJson}";
            await DisplayMessage(msg);
        }

        public static string GetSlingBoxesStatusAsJson(SlingBoxServerStatus serverStatus, StringValues slingBoxesNames)
        {
            var slingBoxes = serverStatus.SlingBoxes;
            var commonSlingBoxNames = slingBoxesNames.Intersect(slingBoxes.Keys);
            var commonSlingBoxesStatus = commonSlingBoxNames.Select(name => (slingBoxes[name ?? ""])).ToList();
            var serializedBoxesStatusToJson =
                SlingBoxServerSerializer.SerializeSlingBoxesStatusToJsonForTvGuideWebSite(commonSlingBoxesStatus);

            return serializedBoxesStatusToJson;
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
