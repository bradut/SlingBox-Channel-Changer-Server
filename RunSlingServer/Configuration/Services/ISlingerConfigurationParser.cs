using RunSlingServer.Configuration.Models;

namespace RunSlingServer.Configuration.Services;

public interface ISlingerConfigurationParser
{
    SlingerConfiguration Parse(string configBody = "");
    string ConfigFilePath { get; }
}