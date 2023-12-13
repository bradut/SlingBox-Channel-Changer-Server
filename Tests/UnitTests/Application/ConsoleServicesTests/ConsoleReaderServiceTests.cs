using Application.Interfaces;
using Application.Services;
using Application.SignalRServices;
using Application.SignalRServices.Notifications;
using FluentAssertions;
using Moq;
using RunSlingServer.Configuration.Models;

namespace UnitTests.Application.ConsoleServicesTests
{
    public class ConsoleReaderServiceTests
    {
        private readonly IConsoleReaderService _sut = CreateConsoleOutputReader();
        private static readonly Mock<ISignalRNotifier> SignalRNotifierMock = new();

        private static ConsoleReaderService CreateConsoleOutputReader()
        {
            var appConfiguration = new AppConfiguration();
            SignalRNotifierMock.Setup(s => s.NotifyClients(It.IsAny<ISignalRNotification>())).Returns(Task.CompletedTask);

            var sut = new ConsoleReaderService(null, null, appConfiguration.RemoteControlIrCodes, null, SignalRNotifierMock.Object);

            return sut;
        }



        [Fact]
        public async Task ParseLog_WithValidLogEntries_CreatesSlingBoxes()
        {
            // Arrange


            // Act
            foreach (var line in Log_HeaderOnly
                         .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                await _sut.ParseLogLineAsync(line);
            }

            // Assert
            Assert.NotNull(_sut.ServerStatus);
            Assert.Equal(3, _sut.ServerStatus.SlingBoxesCount);

            var slingBoxes = _sut.ServerStatus.SlingBoxes.Keys.ToArray();
            Assert.Equal("slingbox1", slingBoxes[0]);
            Assert.Equal("slingbox2", slingBoxes[1]);
            Assert.Equal("slingbox3", slingBoxes[2]);
        }


        [Theory]
        [MemberData(nameof(GetAnalogueTunerChannelChangeTestData))]
        public async Task ParseLog_AnalogueTunerData_ChangesChannelNumber(string logText, int expectedChannel, int expectedLastChannel)
        {
            // Arrange


            // Act
            foreach (var line in logText.Split(new[] { Environment.NewLine },
                         StringSplitOptions.RemoveEmptyEntries))
            {
                await _sut.ParseLogLineAsync(line);
            }

            // Assert
            Assert.Equal(3, _sut.ServerStatus.SlingBoxesCount);

            var slingBox = _sut.ServerStatus.SlingBoxes.FirstOrDefault(kvp => kvp.Key == "slingbox2").Value;
            Assert.Equal(expectedChannel, slingBox.CurrentChannelNumber);
            Assert.Equal(expectedLastChannel, slingBox.LastChannelNumber);

            if (expectedChannel >= 0)
            {
                SignalRNotifierMock.Verify(n => n.NotifyClients(It.IsAny<ISignalRNotification>()), Times.AtLeast(1));

                SignalRNotifierMock.Verify(n => n.NotifyClients(It.IsAny<StreamingInProgressNotification>()), Times.AtLeast(1));
                SignalRNotifierMock.Verify(n => n.NotifyClients(It.IsAny<ChannelChangedNotification>()), Times.AtLeast(1));
                SignalRNotifierMock.Verify(n => n.NotifyClients(It.IsAny<StreamingStoppedNotification>()), Times.AtLeast(1));
            }
        }



        [Theory]
        [ClassData(typeof(LogEntries4UpdateLastHeartBeat))]
        public async Task ParseLog_LogEntries4UpdateHeartBeat_ChangesLastHeartBeat(string line, string slingBoxName)
        {
            // Arrange
            _sut.ServerStatus.AddSlingBox(slingBoxName, slingBoxName, isAnalogue: false);


            // Act
            await _sut.ParseLogLineAsync(line);

            // Assert
            Assert.Equal(1, _sut.ServerStatus.SlingBoxesCount);
            var slingBox = _sut.ServerStatus.SlingBoxes.FirstOrDefault(x => x.Key == slingBoxName).Value;
            Assert.NotNull(slingBox.LastHeartBeatTimeStamp);
            Assert.True(slingBox.LastHeartBeatTimeStamp is not null && slingBox.LastHeartBeatTimeStamp > DateTime.Now.AddSeconds(-5));
        }



        [Theory]
        [ClassData(typeof(LogEntries4DigitalTunerTestData))]
        public async Task ParseLog_DigitalTunerData_ChangesChannelNumber(string logText, string slingBoxName, int expectedChannel, string testName)
        {
            // Arrange
            _sut.ServerStatus.AddSlingBox(slingBoxName, slingBoxName, isAnalogue: false);

            // Act
            foreach (var line in logText.Split(new[] { Environment.NewLine },
                         StringSplitOptions.RemoveEmptyEntries))
            {
                await _sut.ParseLogLineAsync(line);
            }

            // Assert
            Assert.Equal(1, _sut.ServerStatus.SlingBoxesCount);

            var slingBox = _sut.ServerStatus.SlingBoxes.FirstOrDefault(x => x.Key == slingBoxName).Value;

            slingBox.CurrentChannelNumber.Should().Be(expectedChannel, $"[{testName}]");
        }


        [Theory]
        [InlineData("slingBox2 Shutting down connections", "slingBox2")]
        [InlineData("slingBox2 Logging Out", "slingBox2")]
        [InlineData(".slingBox2 Giving up. Sorry...", "slingBox2")]
        public async Task ParseLog_SlingBoxShuttingDown_SetLastHeartBeastToNull(string line, string slingBoxName)
        {
            // Arrange
            _sut.ServerStatus.AddSlingBox(slingBoxName, slingBoxName, isAnalogue: false);
            _sut.ServerStatus.SetSlingBoxLastHeartBeatTimeStamp(slingBoxName, DateTime.Now);

            // Act
            await _sut.ParseLogLineAsync(line);

            // Assert
            Assert.Equal(1, _sut.ServerStatus.SlingBoxesCount);
            var slingBox = _sut.ServerStatus.SlingBoxes.FirstOrDefault(x => x.Key == slingBoxName).Value;
            Assert.Null(slingBox.LastHeartBeatTimeStamp);
            SignalRNotifierMock.Verify(n => n.NotifyClients(It.IsAny<StreamingStoppedNotification>()), Times.AtLeast(1));
        }


        [Theory]
        [InlineData("slingBox2 Error Starting Session. Slingbox might be Bricked", "slingBox2")]
        public async Task ParseLog_SlingBoxShuttingDown_SendSlingboxBrickedNotification(string line, string slingBoxName)
        {
            // Arrange
            _sut.ServerStatus.AddSlingBox(slingBoxName, slingBoxName, isAnalogue: false);

            // Act
            await _sut.ParseLogLineAsync(line);

            // Assert
            SignalRNotifierMock.Verify(n => n.NotifyClients(It.IsAny<SlingBoxBrickedNotification>()), Times.AtLeast(1));
        }

        [Theory]
        [InlineData("slingBox2 Ignoring IR request from 192.168.1.121 Remote Locked by 192.168.1.10", "slingBox2")]
        public async Task ParseLog_SlingBoxShuttingDown_SendRemoteLockedNotification(string line, string slingBoxName)
        {
            // Arrange
            _sut.ServerStatus.AddSlingBox(slingBoxName, slingBoxName, isAnalogue: false);

            // Act
            await _sut.ParseLogLineAsync(line);

            // Assert
            SignalRNotifierMock.Verify(n => n.NotifyClients(It.IsAny<RemoteLockedNotification>()), Times.AtLeast(1));
        }


        [Theory]
        [InlineData(".12/11/2023, 13:35:28.949  slingBox2 Sending Start Channel 155", true, 155)]
        [InlineData(".12/11/2023, 13:35:28.949  slingBox2 Sending Start Channel 155", false, 155)]
        public async Task ParseLog_SelectingStartingChannelInConfig_SelectsTheChannel(string line, bool isAnalogue, int expectedChannel)
        {
            // Arrange
            const string slingBoxName = "slingBox2";
            var appConfiguration = new AppConfiguration();
            var sut = new ConsoleReaderService(null, null, appConfiguration.RemoteControlIrCodes, null, SignalRNotifierMock.Object);
            sut.ServerStatus.AddSlingBox(slingBoxName, "sb2", isAnalogue: isAnalogue);

            // Act
            await sut.ParseLogLineAsync(line);


            // Assert
            Assert.Equal(expectedChannel, sut.ServerStatus.SlingBoxes[slingBoxName].CurrentChannelNumber);
        }

        public static IEnumerable<object[]> GetAnalogueTunerChannelChangeTestData()
        {
            yield return new object[]
            {
                Log_HeaderOnly + Log_AnalogTuner_SlingBox2Channel0,
                0, -1
            };

            yield return new object[]
            {
                Log_HeaderOnly + Log_AnalogTuner_SlingBox2Channel0
                                    + Log_AnalogTuner_SlingBox2Channel1,
                1, 0
            };

            yield return new object[]
            {
                Log_HeaderOnly + Log_AnalogTuner_SlingBox2Channel0
                                    + Log_AnalogTuner_SlingBox2Channel1
                                    + Log_AnalogTuner_SlingBox2ChannelUp,
                2, 1
            };

            yield return new object[]
            {
                Log_HeaderOnly + Log_AnalogTuner_SlingBox2Channel0 // Channel = 0, Last = -1
                                    + Log_AnalogTuner_SlingBox2Channel1 // Channel = 1, Last = 0
                                    + Log_AnalogTuner_SlingBox2ChannelDown, // Channel = 0, Last = 1
                0, 1
            };


            yield return new object[]
            {
                Log_HeaderOnly + Log_AnalogTuner_SlingBox2Channel0 // Channel = 0, Last = -1
                                    + Log_AnalogTuner_SlingBox2Channel1 // Channel = 1, Last = 0
                                    + Log_AnalogTuner_SlingBox2LastChannel, // Channel = 0, Last = 1
                0, 1
            };


            //Edge cases:

            // No channel change 
            yield return new object[] { Log_HeaderOnly, -1, -1 };

            // Channel down when selected channel is 0
            yield return new object[]
            {
                Log_HeaderOnly + Log_AnalogTuner_SlingBox2Channel0 // Channel = 0, Last = -1
                                    + Log_AnalogTuner_SlingBox2ChannelDown, // Channel = 0, Last = -1
                0, -1
            };

        }

        private const string Log_HeaderOnly = @"
Version : 4.01 Running on Windows-10-10.0.19045-SP0 pid= 7088 slingbox_server.exe
Using config file config.ini
Connection Manager Running on port 12345 with 5 max streams using URL sb.
BOXES [('sb1', 'slingbox1'), ('sb2', 'slingbox2'), ('sb3', 'slingbox3')]
BOX URL /sb_secret/slingbox1
Building page for slingbox1
Reading Custom Remote definition from remote.txt
BOX URL /sb_secret/slingbox2
Building page for slingbox2
Reading Custom Remote definition from remote_ProHD_Tuner_AJAX_JS.html
BOX URL /sb_secret/slingbox3
Building page for slingbox3
Reading Custom Remote definition from remote_ProHD_Tuner_slingbox3.html
Streamer Running:  5 config.ini slingbox1 slingbox1 12345 8388608";

        private const string Log_AnalogTuner_SlingBox2Channel0 = @"
07/10/2023, 14:34:19.125   RemoteControl connection from ('192.168.1.10', 55519)
Remote Control Connected
Sending Channel Digits 0.
ProHD 2.0.0
slingbox2 Stream started at 07/10/2023, 14:34:21.139  732 0
slingbox2 Got Streamer Control Message ProHD
07/10/2023, 14:34:21.164  slingbox2 got ProHD 2.0.0 192.168.1.10
sending key 2.0.0 0
cmd: 0x89 err: 0xa 120
slingbox1 Giving up. Sorry...
.........07/10/2023,14:35:54 slingbox2 1 Clients.192.168.1.10:55512
.........07/10/2023,14:37:27 slingbox2 1 Clients.192.168.1.10:55512
.......07/10/2023, 14:38:45.358   RemoteControl connection from ('192.168.1.10', 55670)
";

        private const string Log_AnalogTuner_SlingBox2Channel1 = @"
Remote Control Connected
Sending Channel Digits 1.
ProHD 2.1.0
slingbox2 Got Streamer Control Message ProHD
07/10/2023, 14:38:45.366  slingbox2 got ProHD 2.1.0 192.168.1.10
sending key 2.1.0 0
..07/10/2023,14:39:00 slingbox2 1 Clients.192.168.1.10:55512
.........07/10/2023,14:40:33 slingbox2 1 Clients.192.168.1.10:55512
.........07/10/2023,14:42:06 slingbox2 1 Clients.192.168.1.10:55512
.........07/10/2023,14:43:39 slingbox2 1 Clients.192.168.1.10:55512
..07/10/2023, 14:44:05.06   RemoteControl connection from ('192.168.1.10', 55864)

";

        private const string Log_AnalogTuner_SlingBox2ChannelUp = @"
07/11/2023, 14:19:15.779   RemoteControl connection from ('192.168.1.10', 50111)
Remote Control Connected
GET Remote/slingbox2

..07/11/2023, 14:19:36.818   RemoteControl connection from ('192.168.1.10', 50119)
Remote Control Connected
Remote Ch+ 0.0
ProHD 0.0.0
slingbox2 Got Streamer Control Message ProHD
07/11/2023, 14:19:36.837  slingbox2 got ProHD 0.0.0 192.168.1.10
sending key 0.0.0 0

07/11/2023, 14:19:36.841   RemoteControl connection from ('192.168.1.10', 50120)
Remote Control Connected
GET js/slingerplayer.js

07/11/2023, 14:19:36.854   RemoteControl connection from ('192.168.1.10', 50124)
Remote Control Connected
GET Remote/slingbox2
";

        private const string Log_AnalogTuner_SlingBox2ChannelDown = @"
..07/11/2023, 14:20:01.175   RemoteControl connection from ('192.168.1.10', 50128)
Remote Control Connected
Remote Ch- 1.0
ProHD 1.0.0

07/11/2023, 14:20:01.196   RemoteControl connection from ('192.168.1.10', 50130)
Remote Control Connected
GET js/slingerplayer.js

07/11/2023, 14:20:01.215   RemoteControl connection from ('192.168.1.10', 50133)
Remote Control Connected
GET Remote/slingbox2
slingbox2 Got Streamer Control Message ProHD
07/11/2023, 14:20:01.254  slingbox2 got ProHD 1.0.0 192.168.1.10
sending key 1.0.0 0
";

        private const string Log_AnalogTuner_SlingBox2LastChannel = @"
....07/11/2023, 14:20:39.594   RemoteControl connection from ('192.168.1.10', 50139)
Remote Control Connected
Remote Last 3.0
ProHD 3.0.0
slingbox2 Got Streamer Control Message ProHD
07/11/2023, 14:20:39.600  slingbox2 got ProHD 3.0.0 192.168.1.10
sending key 3.0.0 0

07/11/2023, 14:20:39.617   RemoteControl connection from ('192.168.1.10', 50140)
Remote Control Connected
GET js/slingerplayer.js

07/11/2023, 14:20:39.626   RemoteControl connection from ('192.168.1.10', 50144)
Remote Control Connected
GET Remote/slingbox2
";
    }
}
