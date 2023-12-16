namespace UnitTests.Application.ConsoleServicesTests;

/// <summary>
/// This class holds the test data for the test cases in <see cref="ConsoleServicesTests"/>
/// </summary>
public class LogEntries4DigitalTunerTestData : TheoryData<string, string, int, string>
{
    public LogEntries4DigitalTunerTestData()
    {
        Add(Log_DigitalTuner_NoSpecialChars_Channel_1111_Ok, "slingBox2", 1111, "get digital channel 1111");
        Add(Log_DigitalTuner_NoSpecialChars_Channel_1111_With_Garbage_Ok, "slingBox2", 1111, "garbage ignored, get digital channel 1111");
        Add(Log_DigitalTuner_NoSpecialChars_Channel_1111_Ok + Log_DigitalTuner_ChannelUp_Ok, "slingBox2", 1111 + 1, "digital channel Ch+");
        Add(Log_DigitalTuner_NoSpecialChars_Channel_1111_Ok + Log_DigitalTuner_ChannelDown_Ok, "slingBox2", 1111 - 1, "digital channel Ch-");
        Add(Log_DigitalTuner_NoSpecialChars_Channel_1111_Ok + Log_DigitalTuner_ChannelDown_Ok + Log_DigitalTuner_Return_to_LastChannel_Ok, "slingBox2", 1111 + 1 - 1, "digital channel Last");
        Add(Log_DigitalTuner_SpecialChars_Channel_1234_Ok, "slingBox2", 1234, "get digital channel 1234");
        Add(Log_DigitalTuner_SpecialChars_Channel_5678_Ok, "slingBox2", 5678, "get digital channel 5678");
        Add(Log_DigitalTuner_SpecialChars_Channel_90_Ok, "slingBox2", 90, "get digital channel 90");
        Add(Log_DigitalTuner_SpecialChars_InvalidChannel_EmptyChannel, "slingBox2", -1, "invalid digital channel");
    }


    private const string Log_DigitalTuner_NoSpecialChars_Channel_1111_Ok = @"
Sending Channel Digits 1111
slingBox2 Got Streamer Control Message IR
";

    private const string Log_DigitalTuner_NoSpecialChars_Channel_1111_With_Garbage_Ok = @"
Sending Channel Digits 1111
11/23/2023, 16:16:55.971   RemoteControl connection from ('192.168.1.10', 56896)
Remote Control Connected
GET js/slingerplayer.js
11/23/2023, 16:16:55.981   RemoteControl connection from ('192.168.1.10', 56899)
Remote Control Connected
GET SlingBoxStatus.jsonusuall
11/23/2023, 16:16:55.989   RemoteControl connection from ('192.168.1.10', 56901)
Remote Control Connected
GET Remote/slingBox2
slingBox2 Got Streamer Control Message IR
";


    private const string Log_DigitalTuner_ChannelUp_Ok = @"
10/31/2023, 17:05:01.734  slingBox2 Sending IR keycode 4 1 for 192.168.1.10
";

    private const string Log_DigitalTuner_ChannelDown_Ok = @"
10/31/2023, 17:07:56.360  slingBox2 Sending IR keycode 5 1 for 192.168.1.10
";

    private const string Log_DigitalTuner_Return_to_LastChannel_Ok = @"
10/31/2023, 17:09:13.031  slingBox2 Sending IR keycode 56 1 for 192.168.1.10
";



    private const string Log_DigitalTuner_SpecialChars_Channel_1234_Ok = @"
slingBox2 Got Streamer Control Message IR
IR [b'\t192.168.1.127', b'\n192.168.1.127', b'\x0b192.168.1.127', b'\x0c192.168.1.127']
";


    private const string Log_DigitalTuner_SpecialChars_Channel_5678_Ok = @"
slingBox2 Got Streamer Control Message IR
IR [b'\r192.168.1.127', b'\x0e192.168.1.127', b'\x0f192.168.1.127', b'\x10192.168.1.127']
";

    private const string Log_DigitalTuner_SpecialChars_Channel_90_Ok = @"
slingBox2 Got Streamer Control Message IR
IR [b'\x11192.168.1.127', b'\x12192.168.1.127']
";



    private const string Log_DigitalTuner_SpecialChars_InvalidChannel_EmptyChannel = @"
slingBox2 Got Streamer Control Message IR
IR [b'\x11192.168.1.127', b'\*****12192.168.1.127']
";
}