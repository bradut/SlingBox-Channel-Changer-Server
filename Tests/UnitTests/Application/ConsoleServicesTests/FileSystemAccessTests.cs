using Application.Abstractions;
using Domain.Models;
using Infrastructure.FileAccess;
using Microsoft.Extensions.Logging;
using Moq;
using System.Globalization;
using System.Reflection;

namespace UnitTests.Application.ConsoleServicesTests;


// B/c  xUnit does not have a global initialization/teardown extension
// we have to use a constructor and IDisposable to clean up the file
// https://stackoverflow.com/questions/12976319/xunit-net-global-setup-teardown
public class FileSystemAccessTests : IDisposable
{
    private static string _fullPath = "";
    private readonly IFileSystemAccess _sut;



    public FileSystemAccessTests()
    {
        // Do "global" initialization here; Called before every test method.

        var path = AssemblyDirectory;

        var idx = path.LastIndexOf(@"\bin\", StringComparison.Ordinal);
        if (idx > 0)
        {
            path = path.Substring(0, idx);
        }

        if (!path.EndsWith("TestFiles\\"))
        {
            path = Path.Combine(path, "TestFiles\\");
        }

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        _fullPath = Path.Combine(path, SlingBoxServerStatus.JSON_FILE_NAME);


        Mock<ILogger<IFileSystemAccess>> loggerMock = new();

        _sut = new FileSystemAccess(path, loggerMock.Object);
    }


    [Fact]
    public void SaveToJsonFile_WithFileContent_CreatesFileOnDisk()
    {
        // Arrange
        var slingBoxServerStatus = GenerateSlingBoxServerStatusTestData();


        // Act
        _sut.SaveToJsonFile(slingBoxServerStatus);

        // Assert
        Assert.True(File.Exists(_fullPath));
    }


    [Fact]
    public void LoadSlingBoxServerStatusFromFile_WithValidFilePath_ReturnsSlingBoxServerStatus()
    {
        // Arrange
        File.WriteAllText(_fullPath, SlingBoxServerStatusJson);


        // Act
        var slingBoxServerStatus = _sut.LoadSlingBoxServerStatusFromFile();

        // Assert
        Assert.NotNull(slingBoxServerStatus);

        Assert.Equal(UrlBase, slingBoxServerStatus.UrlBase);
        Assert.Equal(TvGuideUrl, slingBoxServerStatus.TvGuideUrl);
        Assert.Equal(SlingRemoteControlServiceUrl, slingBoxServerStatus.SlingRemoteControlServiceUrl);

        Assert.Equal(3, slingBoxServerStatus.SlingBoxes.Count);

        Assert.Equal("sb1Name", slingBoxServerStatus.SlingBoxes["sb1Name"].SlingBoxName);
        var sb1 = slingBoxServerStatus.SlingBoxes["sb1Name"];
        Assert.Equal(1, sb1.CurrentChannelNumber);
        Assert.False(sb1.IsAnalogue);
        Assert.Null(sb1.LastHeartBeatTimeStamp);

        Assert.Equal(2, slingBoxServerStatus.SlingBoxes["sb2Name"].CurrentChannelNumber);
        var sb2 = slingBoxServerStatus.SlingBoxes["sb2Name"];
        Assert.Equal(22, sb2.LastChannelNumber);
        Assert.True(sb2.IsAnalogue);
        Assert.NotNull(sb2.LastHeartBeatTimeStamp);
        Assert.Equal(DateTime.ParseExact("2023-01-31T18:53:53", "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            sb2.LastHeartBeatTimeStamp);


        var sb3 = slingBoxServerStatus.SlingBoxes["sb3Name"];
        Assert.Equal(3, sb3.CurrentChannelNumber);
        Assert.Equal(33, sb3.LastChannelNumber);
        Assert.True(sb3.IsAnalogue);
        Assert.Equal(DateTime.ParseExact("2023-02-28T19:19:19", "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            sb3.LastHeartBeatTimeStamp);

    }



    public const string UrlBase = "sb_secret";
    public const string TvGuideUrl = "http://localhost:80/TvGuide/TvGuide.html";
    public const string SlingRemoteControlServiceUrl = "https://localhost:7064/api/post-to-url";


    // Note in the test data below that the JSON field names do not start with lowercase, but can still be parsed
    private const string SlingBoxServerStatusJson = $@"
{{
  ""SlingBoxes"": {{
    ""sb1Name"": {{
      ""SlingBoxId"": ""sb1"",
      ""CurrentChannelNumber"": 1,
      ""LastChannelNumber"": -1,
      ""IsAnalogue"": false,
      ""LastHeartBeatTimeStamp"": null
    }},
    ""sb2Name"": {{
      ""SlingBoxId"": ""sb2"",
      ""CurrentChannelNumber"": 2,
      ""LastChannelNumber"": 22,
      ""IsAnalogue"": true,
      ""LastHeartBeatTimeStamp"": ""2023-01-31T18:53:53""
    }},
    ""sb3Name"": {{
      ""SlingBoxId"": ""sb3"",
      ""CurrentChannelNumber"": 3,
      ""LastChannelNumber"": 33,
      ""IsAnalogue"": true,
      ""LastHeartBeatTimeStamp"": ""2023-02-28T19:19:19""
    }}
  }},
  ""UrlBase"": ""{UrlBase}"",
  ""tvGuideUrl"": ""{TvGuideUrl}"",
  ""slingRemoteControlServiceUrl"": ""{SlingRemoteControlServiceUrl}""
}}";


    private static SlingBoxServerStatus GenerateSlingBoxServerStatusTestData()
    {
        var sb1 = GenerateSlingBoxStatus("sb1", "sb1Name", 1);
        var sb2 = GenerateSlingBoxStatus("sb2", "sb2Name", 2);
        var sb3 = GenerateSlingBoxStatus("sb3", "sb3Name", 3);

        var sbServer = new SlingBoxServerStatus(new List<SlingBoxStatus> { sb1, sb2, sb3 })
        {
            UrlBase = "sb_secret",
            TvGuideUrl = "http://localhost:80/TvGuide/TvGuide.html",
            SlingRemoteControlServiceUrl = "https://localhost:7064/api/post-to-url"
        };


        return sbServer;
    }

    private static SlingBoxStatus GenerateSlingBoxStatus(string slingId, string slingName, int channelNumber)
    {
        var slingBox = new SlingBoxStatus(slingBoxId: slingId, slingBoxName: slingName);

        slingBox.ChangeChannel(channelNumber);

        return slingBox;
    }

    // https://stackoverflow.com/questions/52797/how-do-i-get-the-path-of-the-assembly-the-code-is-in
    public static string AssemblyDirectory
    {
        get
        {
            var assemblyLocationPath = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrWhiteSpace(assemblyLocationPath))
                return string.Empty;

            var uriBuilder = new UriBuilder(assemblyLocationPath);
            var path = Uri.UnescapeDataString(uriBuilder.Path);
            var dir = Path.GetDirectoryName(path);

            return string.IsNullOrWhiteSpace(dir)
                ? string.Empty
                : dir;
        }
    }

    public void Dispose()
    {
        // Do "global" teardown here; Called after every test method.
        if (File.Exists(_fullPath))
        {
            File.Delete(_fullPath);
        }
    }
}
