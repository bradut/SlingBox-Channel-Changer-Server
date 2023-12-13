using System.Net;
using System.Text;
using Application.Abstractions;
using Application.Interfaces;
using Application.SignalRServices;
using Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using RunSlingServer.WebApi.Services;
using RunSlingServer.WebApi.Services.EndpointsServices;

namespace UnitTests.Application.WebAPITests
{
    public class PostToUrlHandlerTests
    {
        private readonly Mock<IFileSystemAccess> _fileSystemAccessMock;
        private readonly Mock<ILogger> _loggerMock;
        private readonly Mock<IWebHelpers> _webHelpersMock;

        private readonly PostToUrlHandler _sut;

        public PostToUrlHandlerTests()
        {
            Mock<IConsoleDisplayDispatcher> consoleMock = new();
            _fileSystemAccessMock = new Mock<IFileSystemAccess>();
            Mock<ISignalRNotifier> signalRNotifierMock = new();
            _loggerMock = new Mock<ILogger>();
            _webHelpersMock = new Mock<IWebHelpers>();

            _sut = new PostToUrlHandler(consoleMock.Object, _fileSystemAccessMock.Object, signalRNotifierMock.Object,
                                        _loggerMock.Object, _webHelpersMock.Object);
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
            var serverStatus = new SlingBoxServerStatus(new List<SlingBoxStatus>{slingBoxStatus});

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
}
