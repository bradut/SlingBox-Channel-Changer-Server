using Application.Abstractions;
using Application.Interfaces;
using Application.SignalRServices;
using Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using RunSlingServer.WebApi.Services;
using RunSlingServer.WebApi.Services.EndpointsServices;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace UnitTests.Application.WebAPITests;

public class PostToUrlHandlerTests
{
    private readonly Mock<IFileSystemAccess> _fileSystemAccessMock;
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<IWebHelpers> _webHelpersMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly PostToUrlHandler _sut;

    public PostToUrlHandlerTests()
    {
        Mock<IConsoleDisplayDispatcher> consoleMock = new();
        _fileSystemAccessMock = new Mock<IFileSystemAccess>();
        Mock<ISignalRNotifier> signalRNotifierMock = new();
        _loggerMock = new Mock<ILogger>();
        _webHelpersMock = new Mock<IWebHelpers>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();

        const int minDelayDigitalSecForTesting = 2;

        _sut = new PostToUrlHandler(consoleMock.Object, _fileSystemAccessMock.Object, signalRNotifierMock.Object,
                                    _loggerMock.Object, _webHelpersMock.Object, _dateTimeProviderMock.Object,
                                    minDelayDigitalSecForTesting);
    }



    [Theory]
    [InlineData(true, false, false)] // analogue tuner will never be delayed
    [InlineData(true, true, false)] // analogue tuner will never be delayed

    [InlineData(false, false, true)]
    [InlineData(false, true, false)]

    public async Task HandlePostToUrl_DigitalChannel_PostsDelayedByAtLeast4Seconds(bool isAnalogue, bool isLastHeartBeatNull, bool isExpectedToBeDelayed)
    {
        // Arrange
        const string slingBoxName = "SlingBox1";
        const int channel = 800;
        var context = new DefaultHttpContext();
        var request = context.Request;
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes($"Channel=Channel&Digits={channel}"));

        request.QueryString = new QueryString($"?url=http://domain.com/Remote/{slingBoxName}");

        _dateTimeProviderMock.Setup(dateTimeProvider => dateTimeProvider.Now).Returns(new DateTime(2023, 12, 15, 23, 13, 00));

        DateTime? now = isLastHeartBeatNull ? null : _dateTimeProviderMock.Object.Now;


        var slingBoxStatus = new SlingBoxStatus(slingBoxName, "slingBoxId", isAnalogue: isAnalogue, currentChannelNumber: channel, lastChannelNumber: 799, lastHeartBeatTimeStamp: now);
        var serverStatus = new SlingBoxServerStatus(new List<SlingBoxStatus> { slingBoxStatus });
        _fileSystemAccessMock.Setup(fileSystemAccess => fileSystemAccess.LoadSlingBoxServerStatusFromFile())
            .Returns(serverStatus);

        var postResponses = $"<label>Status:</label>{slingBoxName} Streaming 1 clients. Resolution=12 Packets=1534";
        _webHelpersMock.Setup(webHelpers => webHelpers
                .PostToSlingerServer(It.IsAny<string>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>(),
                    context.Request, _loggerMock.Object))
                .ReturnsAsync(postResponses);


        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        await _sut.HandlePostToUrl(request);

        stopwatch.Stop();

        // Assert
        var isDelayedByAtLeast4Seconds =
            stopwatch.Elapsed.TotalSeconds + 0.5 >= _sut.MinDelayDigitalSec;

        Assert.Equal(isExpectedToBeDelayed, isDelayedByAtLeast4Seconds);
    }


    [Theory]
    [ClassData(typeof(PostResponseTestData))]
    public async Task HandlePostToUrl_ValidUrl_ReturnsExpectedResult(string slingBoxName, int channel, string postResponse, string expectedSubstring)
    {
        // Arrange
        var context = new DefaultHttpContext();
        const string clientIp = "127.0.0.1";
        var someUrl = $"http://domain.com/Remote/{slingBoxName}";

        var slingBoxStatus = new SlingBoxStatus(slingBoxName, "slingBoxId");
        var serverStatus = new SlingBoxServerStatus(new List<SlingBoxStatus> { slingBoxStatus });

        context.Connection.RemoteIpAddress = IPAddress.Parse(clientIp);
        context.Request.QueryString = new QueryString($"?url={someUrl}");
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes($"Channel=Channel&Digits={channel}"));

        _fileSystemAccessMock.Setup(fileSystemAccess => fileSystemAccess.LoadSlingBoxServerStatusFromFile())
                             .Returns(serverStatus);
        _webHelpersMock.Setup(webHelpers => webHelpers
                       .PostToSlingerServer(It.IsAny<string>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>(),
                                              context.Request, _loggerMock.Object))
                       .ReturnsAsync(postResponse);


        // Act
        var result = await _sut.HandlePostToUrl(context.Request);


        // Assert
        Assert.Contains(slingBoxName, result);
        Assert.Contains(expectedSubstring, result);
    }



    public class PostResponseTestData : TheoryData<string, int, string, string>
    {
        public const int ChannelNumber = 4;
        public const string SlingBoxName = "SlingBox1";

        public PostResponseTestData()
        {
            Add(SlingBoxName, ChannelNumber, ValidPostResponseContent, $"channel {ChannelNumber}");
            Add(SlingBoxName, ChannelNumber, InvalidPostResponseContentError500, "500 Internal Server Error");
            Add(SlingBoxName, ChannelNumber, InvalidPostResponseContentError, "Error");
            Add(SlingBoxName, ChannelNumber, InvalidPostResponseContentEmpty, "not found");
            Add(SlingBoxName, ChannelNumber, PostResponseContentSlingStopped, "STOPPED");
        }

        private static string ValidPostResponseContent
        {
            get
            {
                var postResponse =
                    $"<label>Status:</label>{SlingBoxName} Streaming 1 clients. Resolution=12 Packets=1534";
                return postResponse;
            }
        }

        private static string InvalidPostResponseContentError500 => "500 Internal Server Error";

        private static string InvalidPostResponseContentError => "Error";

        private static string InvalidPostResponseContentEmpty => "";

        private static string PostResponseContentSlingStopped => "Can't find a slingbox on network";
    }
}
