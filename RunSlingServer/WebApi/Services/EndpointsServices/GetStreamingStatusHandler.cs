using Application.Abstractions;
using Domain.Helpers;
using Domain.Models;
using Microsoft.Extensions.Primitives;
using RunSlingServer.WebApi.Services.Interfaces;


namespace RunSlingServer.WebApi.Services.EndpointsServices
{
    public class GetStreamingStatusHandler : IGetStreamingStatusHandler
    {
        private readonly IFileSystemAccess _fileSystemAccess;

        private readonly ILogger _logger;

        public GetStreamingStatusHandler(IFileSystemAccess fileSystemAccess, ILogger logger)
        {
            _fileSystemAccess = fileSystemAccess;
            _logger = logger;

        }

        public async Task<string> GetStreamingStatus(HttpContext context)
        {
            const string slingBoxNameParameterName = "slingBoxName";

            if (!context.Request.Query.ContainsKey(slingBoxNameParameterName))
            {
                const string errorMessage = $"WebApi Get: Missing '{slingBoxNameParameterName}' parameter";
                await HandleError(errorMessage);

                return string.Empty;
            }

            var serverStatus = GetSlingBoxServerStatus();
            if (serverStatus == null)
            {
                const string errorMessage = "WebApi Get: Could not get Server status";
                await HandleError(errorMessage);

                return string.Empty;
            }

            var slingBoxesNames = context.Request.Query[slingBoxNameParameterName];
            var serializedBoxesStatusToJson = GetSlingBoxesStatusAsJson(serverStatus, slingBoxesNames);
            var ip = WebHelpers.GetClientIp(context.Request);

            context.Response.StatusCode = StatusCodes.Status200OK;

            LogStreamingStatus(slingBoxesNames, ip, serializedBoxesStatusToJson);

            return serializedBoxesStatusToJson;



            async Task HandleError(string errorMessage)
            {
                _logger.LogError(errorMessage);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync(errorMessage);
            }
        }

        private SlingBoxServerStatus? GetSlingBoxServerStatus()
        {
            try
            {
                var serverStatus = _fileSystemAccess.LoadSlingBoxServerStatusFromFile();
                return serverStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError($"WebApi Get: Error loading server status from file: {ex.Message}");
                return null;
            }
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

        private void LogStreamingStatus(StringValues slingBoxesNames, string ip, string serializedBoxesStatusToJson)
        {
            var msg = $"WebApi Get: Request: streaming status for SlingBoxes '{slingBoxesNames}', IP {ip}, at {DateTime.Now} " +
                      $"\nResponse:\n{serializedBoxesStatusToJson}";

            _logger.LogInformation(msg);
        }
    }
}
