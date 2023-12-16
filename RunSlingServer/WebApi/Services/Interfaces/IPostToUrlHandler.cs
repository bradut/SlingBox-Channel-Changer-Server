namespace RunSlingServer.WebApi.Services.Interfaces;

public interface IPostToUrlHandler
{
    Task<string> HandlePostToUrl(HttpRequest request);

    int MinDelayDigitalSec { get; }
}