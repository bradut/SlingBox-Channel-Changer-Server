using Domain.Abstractions;

namespace RunSlingServer.Configuration.Services;

public interface IAppConfiguration : ISerializeToJsonFile
{
    string Version { get; set; }
    string SlingboxServerExecutableName { get; set; }
    string SlingBoxServerConfigFileName { get; }
    string TvGuideUrl { get; set; }
    string SlingRemoteControlServiceUrl { get; set; }
    string WebApiBaseUrl { get; }
    string RootPath { get; }
    Dictionary<string, string?>? RemoteControlIrCodes { get; set; }
}