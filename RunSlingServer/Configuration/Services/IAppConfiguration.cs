using Domain.Abstractions;

namespace RunSlingServer.Configuration.Services;

public interface IAppConfiguration : ISerializeToJsonFile
{
    string Version { get; set; }
    string SlingboxServerExecutableName { get; }
    string SlingBoxServerConfigFileName { get; }
    string TvGuideUrl { get; }
    string SlingRemoteControlServiceUrl { get; }
    string WebApiBaseUrl { get; }
    string RootPath { get; }
    Dictionary<string, string?>? RemoteControlIrCodes { get; }
}