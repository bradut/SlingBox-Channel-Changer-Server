namespace RunSlingServer.WebApi.Services;

public interface IWebHelpers
{
    Task<string> PostToSlingerServer(string url, IEnumerable<KeyValuePair<string, string>> postData, HttpRequest request, ILogger? logger);
}