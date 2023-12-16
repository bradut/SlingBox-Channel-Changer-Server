using Application.Abstractions;
using Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using RunSlingServer.WebApi.Services.EndpointsServices;

namespace UnitTests.Application.WebAPITests;

public class GetStreamingStatusHandlerTests
{
    private readonly Mock<IFileSystemAccess> _fileSystemAccessMock;

    private readonly GetStreamingStatusHandler _getStreamingStatusHandler;

    private const string SlingBoxNameParameterName = "slingBoxName";

    public GetStreamingStatusHandlerTests()
    {
        _fileSystemAccessMock = new Mock<IFileSystemAccess>();
        Mock<ILogger> loggerMock = new();

        _getStreamingStatusHandler = new GetStreamingStatusHandler(_fileSystemAccessMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task GetStreamingStatus_WithValidParameters_ReturnsSerializedBoxesStatus()
    {
        // Arrange
        const string slingBoxName = "SlingBox1";
        var context = new DefaultHttpContext();
        context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            [SlingBoxNameParameterName] = new StringValues(slingBoxName)
        });

        var slingBoxStatus = new SlingBoxStatus(slingBoxName, "slingBoxId");
        var serverStatus = new SlingBoxServerStatus(new List<SlingBoxStatus> { slingBoxStatus });
        _fileSystemAccessMock.Setup(f => f.LoadSlingBoxServerStatusFromFile()).Returns(serverStatus);

        // Act
        var result = await _getStreamingStatusHandler.GetStreamingStatus(context);

        // Assert
        Assert.Contains(slingBoxName, result);
        _fileSystemAccessMock.Verify(f => f.LoadSlingBoxServerStatusFromFile(), Times.Once);
    }

}

