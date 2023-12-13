namespace RunSlingServer.WebApi.Services.Interfaces;

public interface IGetStreamingStatusHandler
{
    Task<string> GetStreamingStatus(HttpContext context);
}