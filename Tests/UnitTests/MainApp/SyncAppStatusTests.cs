using Application.Abstractions;
using Domain.Models;
using NSubstitute;
using RunSlingServer.Configuration.Models;
using RunSlingServer.Configuration.Services;
using RunSlingServer.Helpers;

namespace UnitTests.MainApp;

public class SyncAppStatusTests
{
    private readonly IFileSystemAccess _fileSystemAccessMock = Substitute.For<IFileSystemAccess>();
    private readonly ISlingerConfigurationParser _slingerConfigParserMock = Substitute.For<ISlingerConfigurationParser>();
    private readonly IAppConfiguration _appConfigMock = Substitute.For<IAppConfiguration>();

    [Fact]
    public void SynchronizeAppStatus_ValidParams_ReturnsValidResult()
    {
        // Arrange
        _fileSystemAccessMock.LoadSlingBoxServerStatusFromFile().Returns(CreateSlingBoxServerStatus());
        _slingerConfigParserMock.Parse().Returns(CreateSlingerConfiguration());
        _appConfigMock.SlingRemoteControlServiceUrl.Returns(SlingerServerWrapperUrl);
        _appConfigMock.TvGuideUrl.Returns(TvGuideUrlAnalogue);

        // Act
        var result = SyncAppStatus.SynchronizeAppStatus(_fileSystemAccessMock, _slingerConfigParserMock, _appConfigMock);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.ErrorMessages.Count == 0);

        // SlingBox-specific settings
        Assert.Equal(3, result.Value.SlingBoxesCount);
        Assert.Equal(Sling0, result.Value.SlingBoxes.FirstOrDefault(s => s.Key == Sling0).Key);
        Assert.Equal(Sling1, result.Value.SlingBoxes.FirstOrDefault(s => s.Key == Sling1).Key);
        Assert.Equal(Sling2, result.Value.SlingBoxes.FirstOrDefault(s => s.Key == Sling2).Key);

        Assert.Equal(TvGuideUrlDigital, result.Value.GetSlingBoxStatus(Sling0).TvGuideUrl);
        Assert.True(string.IsNullOrWhiteSpace(result.Value.GetSlingBoxStatus(Sling1).TvGuideUrl));


        Assert.Equal(800, result.Value.GetSlingBoxStatus(Sling0).CurrentChannelNumber);
        Assert.Equal(801, result.Value.GetSlingBoxStatus(Sling1).CurrentChannelNumber);
        Assert.Equal(802, result.Value.GetSlingBoxStatus(Sling2).CurrentChannelNumber);

        Assert.False(result.Value.GetSlingBoxStatus(Sling0).IsAnalogue);
        Assert.True(result.Value.GetSlingBoxStatus(Sling1).IsAnalogue);
        Assert.False(result.Value.GetSlingBoxStatus(Sling2).IsAnalogue); // 350/500/M1

        // Server-wide settings
        Assert.Equal(TvGuideUrlAnalogue, result.Value.TvGuideUrl);
        Assert.Equal(SlingerServerWrapperUrl, result.Value.SlingRemoteControlServiceUrl);

        _fileSystemAccessMock.Received(1).LoadSlingBoxServerStatusFromFile();
        _fileSystemAccessMock.Received(1).SaveToJsonFile(Arg.Is<SlingBoxServerStatus>(sss => sss.SlingBoxes.Count == 3));
    }


    [Fact]
    public void SynchronizeAppStatus_NoSlingBoxServerStatusFileFound_ReturnsDefaultValueResult()
    {
        // Arrange
        _fileSystemAccessMock.LoadSlingBoxServerStatusFromFile().Returns(null as SlingBoxServerStatus);
        _slingerConfigParserMock.Parse().Returns(CreateSlingerConfiguration());
        _appConfigMock.SlingRemoteControlServiceUrl.Returns(SlingerServerWrapperUrl);
        _appConfigMock.TvGuideUrl.Returns(TvGuideUrlAnalogue);

        // Act
        var result = SyncAppStatus.SynchronizeAppStatus(_fileSystemAccessMock, _slingerConfigParserMock, _appConfigMock);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.ErrorMessages.Count == 0);

        // SlingBox-specific settings
        Assert.Equal(3, result.Value.SlingBoxesCount);
        Assert.Equal(Sling0, result.Value.SlingBoxes.FirstOrDefault(s => s.Key == Sling0).Key);
        Assert.Equal(Sling1, result.Value.SlingBoxes.FirstOrDefault(s => s.Key == Sling1).Key);
        Assert.Equal(Sling2, result.Value.SlingBoxes.FirstOrDefault(s => s.Key == Sling2).Key);

        Assert.Equal(TvGuideUrlDigital, result.Value.GetSlingBoxStatus(Sling0).TvGuideUrl);
        Assert.True(string.IsNullOrWhiteSpace(result.Value.GetSlingBoxStatus(Sling1).TvGuideUrl));

        Assert.Equal(-1, result.Value.GetSlingBoxStatus(Sling0).CurrentChannelNumber);
        Assert.Equal(-1, result.Value.GetSlingBoxStatus(Sling1).CurrentChannelNumber);
        Assert.Equal(-1, result.Value.GetSlingBoxStatus(Sling2).CurrentChannelNumber);

        Assert.False(result.Value.GetSlingBoxStatus(Sling0).IsAnalogue);
        Assert.True(result.Value.GetSlingBoxStatus(Sling1).IsAnalogue);
        Assert.False(result.Value.GetSlingBoxStatus(Sling2).IsAnalogue); // 350/500/M1

        // Server-wide settings
        Assert.Equal(TvGuideUrlAnalogue, result.Value.TvGuideUrl);
        Assert.Equal(SlingerServerWrapperUrl, result.Value.SlingRemoteControlServiceUrl);


        _fileSystemAccessMock.Received(1).LoadSlingBoxServerStatusFromFile();
        _fileSystemAccessMock.Received(1).SaveToJsonFile(Arg.Is<SlingBoxServerStatus>(sss => sss.SlingBoxes.Count == 3));
    }



    [Fact]
    public void SynchronizeAppStatus_AdditionalSlingBoxRecordsInServerStatusFileFound_ReturnsResultWithSlingerConfigSlingBoxRecords()
    {
        // Arrange
        _fileSystemAccessMock.LoadSlingBoxServerStatusFromFile().Returns(CreateBoxServerStatusWithDifferentSlingBoxes());
        _slingerConfigParserMock.Parse().Returns(CreateSlingerConfiguration());
        _appConfigMock.SlingRemoteControlServiceUrl.Returns(SlingerServerWrapperUrl);
        _appConfigMock.TvGuideUrl.Returns(TvGuideUrlAnalogue);

        // Act
        var result = SyncAppStatus.SynchronizeAppStatus(_fileSystemAccessMock, _slingerConfigParserMock, _appConfigMock);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.ErrorMessages.Count == 0);

        // SlingBox-specific settings
        Assert.Equal(3, result.Value.SlingBoxesCount);
        Assert.Equal(Sling0, result.Value.SlingBoxes.FirstOrDefault(s => s.Key == Sling0).Key);
        Assert.Equal(Sling1, result.Value.SlingBoxes.FirstOrDefault(s => s.Key == Sling1).Key);
        Assert.Equal(Sling2, result.Value.SlingBoxes.FirstOrDefault(s => s.Key == Sling2).Key);

        Assert.Equal(TvGuideUrlDigital, result.Value.GetSlingBoxStatus(Sling0).TvGuideUrl);
        Assert.True(string.IsNullOrWhiteSpace(result.Value.GetSlingBoxStatus(Sling1).TvGuideUrl));

        Assert.Equal(-1, result.Value.GetSlingBoxStatus(Sling0).CurrentChannelNumber);
        Assert.Equal(-1, result.Value.GetSlingBoxStatus(Sling1).CurrentChannelNumber);
        Assert.Equal(-1, result.Value.GetSlingBoxStatus(Sling2).CurrentChannelNumber);

        Assert.False(result.Value.GetSlingBoxStatus(Sling0).IsAnalogue);
        Assert.True(result.Value.GetSlingBoxStatus(Sling1).IsAnalogue);
        Assert.False(result.Value.GetSlingBoxStatus(Sling2).IsAnalogue); //350/500/M1

        // Server-wide settings
        Assert.Equal(TvGuideUrlAnalogue, result.Value.TvGuideUrl);
        Assert.Equal(SlingerServerWrapperUrl, result.Value.SlingRemoteControlServiceUrl);

        _fileSystemAccessMock.Received(1).LoadSlingBoxServerStatusFromFile();
        _fileSystemAccessMock.Received(1).SaveToJsonFile(Arg.Is<SlingBoxServerStatus>(sss => sss.SlingBoxes.Count == 3));
    }



    [Fact]
    public void SynchronizeAppStatus_SlingerOldConfigStyle_ReturnsFailureResult()
    {
        // Arrange
        _fileSystemAccessMock.LoadSlingBoxServerStatusFromFile().Returns(CreateBoxServerStatusWithDifferentSlingBoxes());
        _slingerConfigParserMock.Parse().Returns(CreateOldStyleSlingerConfiguration());
        _appConfigMock.SlingRemoteControlServiceUrl.Returns(SlingerServerWrapperUrl);
        _appConfigMock.TvGuideUrl.Returns(TvGuideUrlAnalogue);

        // Act
        var result = SyncAppStatus.SynchronizeAppStatus(_fileSystemAccessMock, _slingerConfigParserMock, _appConfigMock);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.ErrorMessages.Count == 1);
    }



    private const string Sling0 = "sling0";
    private const string Sling1 = "sling1";
    private const string Sling2 = "sling2";
    private const string TvGuideUrlDigital = "http://localhost:8080";
    private const string TvGuideUrlAnalogue = "http://localhost:9080";
    private const string SlingerServerWrapperUrl = "http://localhost:9081";


    private static SlingBoxServerStatus CreateSlingBoxServerStatus()
    {
        var slingBoxServerStatus = new SlingBoxServerStatus
        {
            SlingRemoteControlServiceUrl = SlingerServerWrapperUrl,
            TvGuideUrl = TvGuideUrlAnalogue
        };

        slingBoxServerStatus.AddSlingBox(Sling0, "sb1");
        slingBoxServerStatus.AddSlingBox(Sling1, "sb2");
        slingBoxServerStatus.AddSlingBox(Sling2, "sb3");

        slingBoxServerStatus.SetSlingBoxCurrentChannelNumber(Sling0, 800);
        slingBoxServerStatus.SetSlingBoxCurrentChannelNumber(Sling1, 801);
        slingBoxServerStatus.SetSlingBoxCurrentChannelNumber(Sling2, 802);

        slingBoxServerStatus.SetSlingBoxIsAnalogue(Sling0, false);
        slingBoxServerStatus.SetSlingBoxIsAnalogue(Sling1, true);
        slingBoxServerStatus.SetSlingBoxIsAnalogue(Sling2, true);

        slingBoxServerStatus.SetSlingBoxTvGuideUrl(Sling0, TvGuideUrlDigital);

        return slingBoxServerStatus;
    }


    private static SlingBoxServerStatus CreateBoxServerStatusWithDifferentSlingBoxes()
    {
        var slingBoxServerStatus = new SlingBoxServerStatus
        {
            SlingRemoteControlServiceUrl = SlingerServerWrapperUrl,
            TvGuideUrl = TvGuideUrlAnalogue
        };

        const string slingBoxName0 = "slingBox_0";
        const string slingBoxName1 = "slingBox_1";
        const string slingBoxName2 = "slingBox_2";

        slingBoxServerStatus.AddSlingBox(slingBoxName0, "sb1");
        slingBoxServerStatus.AddSlingBox(slingBoxName1, "sb2");
        slingBoxServerStatus.AddSlingBox(slingBoxName2, "sb3");

        slingBoxServerStatus.SetSlingBoxCurrentChannelNumber(slingBoxName0, 800);
        slingBoxServerStatus.SetSlingBoxCurrentChannelNumber(slingBoxName1, 801);
        slingBoxServerStatus.SetSlingBoxCurrentChannelNumber(slingBoxName2, 802);

        slingBoxServerStatus.SetSlingBoxIsAnalogue(slingBoxName0, false);
        slingBoxServerStatus.SetSlingBoxIsAnalogue(slingBoxName1, true);
        slingBoxServerStatus.SetSlingBoxIsAnalogue(slingBoxName2, true);

        slingBoxServerStatus.SetSlingBoxTvGuideUrl(slingBoxName0, TvGuideUrlDigital);

        return new SlingBoxServerStatus();
    }


    private static SlingerConfiguration CreateSlingerConfiguration()
    {
        var slingerConfiguration = new SlingerConfiguration
        {
            Port = 8080,
            MaxRemoteStreams = 3,
            UrlBase = "http://localhost:8080",
            IsUnifiedConfig = true
        };

        var sb1 = new SlingBoxConfiguration
        {
            SlingBoxName = Sling0,
            SlingBoxType = "Solo/Pro/ProHD",
            VideoSource = 1,
            RemoteControlFileName = "remote_digital.html",
            TvGuideUrl = TvGuideUrlDigital
        };

        var sb2 = new SlingBoxConfiguration
        {
            SlingBoxName = Sling1,
            SlingBoxType = "Solo/Pro/ProHD",
            VideoSource = 0,
            RemoteControlFileName = "remote_analogue.html",
        };

        var sb3 = new SlingBoxConfiguration
        {
            SlingBoxName = Sling2,
            SlingBoxType = "350/500/M1",
            VideoSource = 0,
            RemoteControlFileName = "remote_analogue.html",
        };

        slingerConfiguration.AddSlingBox(sb1);
        slingerConfiguration.AddSlingBox(sb2);
        slingerConfiguration.AddSlingBox(sb3);

        return slingerConfiguration;
    }


    // Original-style config.ini file [for a single SlingBox]
    private static SlingerConfiguration CreateOldStyleSlingerConfiguration()
    {
        var slingerConfiguration = new SlingerConfiguration
        {
            Port = 8080,
            MaxRemoteStreams = 3,
            UrlBase = "http://localhost:8080",
            IsUnifiedConfig = false
        };

        var sb1 = new SlingBoxConfiguration
        {
            SlingBoxName = Sling0,
            SlingBoxType = "Solo/Pro/ProHD",
            VideoSource = 1,
            RemoteControlFileName = "remote_digital.html",
            TvGuideUrl = TvGuideUrlDigital
        };

        slingerConfiguration.AddSlingBox(sb1);

        return slingerConfiguration;
    }
}

