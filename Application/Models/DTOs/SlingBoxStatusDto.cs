using System.Text.Json.Serialization;

namespace Application.Models.DTOs;

public class SlingBoxStatusDto
{
    public SlingBoxStatusDto()
    { }
    
    public SlingBoxStatusDto(string slingBoxId, string slingBoxName, int currentChannelNumber, int lastChannelNumber, bool isAnalogue)
    {
        SlingBoxId = slingBoxId;
        SlingBoxName = slingBoxName;
        CurrentChannelNumber = currentChannelNumber;
        LastChannelNumber = lastChannelNumber;
        IsAnalogue = isAnalogue;
    }

    [JsonIgnore]
    public string SlingBoxName { get; set; } = string.Empty;

    public string SlingBoxId { get; set; } = string.Empty;

    public int CurrentChannelNumber { get; set; } = -1;

    public int LastChannelNumber { get; set; } = -1;

    public bool IsAnalogue { get; set; }

    public DateTime? LastHeartBeatTimeStamp { get; set; }

    public string? TvGuideUrl { get; set; } = string.Empty;

    public string? SlingRemoteControlServiceUrl { get; set; } = string.Empty;
}