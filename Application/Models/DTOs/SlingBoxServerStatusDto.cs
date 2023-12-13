namespace Application.Models.DTOs;

public class SlingBoxServerStatusDto
{
    public Dictionary<string, SlingBoxStatusDto> SlingBoxes { get; set; } = new();

    public string UrlBase { get; set; } = string.Empty;
    public string TvGuideUrl { get; set; } = string.Empty;
    public string SlingRemoteControlServiceUrl { get; set; } = string.Empty;
}