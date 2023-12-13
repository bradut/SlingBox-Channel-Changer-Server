namespace UnitTests.Application.ConsoleServicesTests;

public class LogEntries4UpdateLastHeartBeat : TheoryData<string, string>
{
    public LogEntries4UpdateLastHeartBeat()
    {
        // UpdateSlingBoxLastHeartBeat1
        Add("slingBox2 Selecting Video Source 0", "slingBox2");
        Add("slingBox2 Got Streamer Control Message ProHD", "slingBox2");
        Add("...slingBox2 Got Streamer Control Message ProHD", "slingBox2");
        Add("slingBox2 New Stream Starting 0", "slingBox2");
        Add("slingBox2 Stream started at 08/23/2023, 21:52:29.921  732 0", "slingBox2");

        // UpdateSlingBoxLastHeartBeat2
        Add(".....08/22/2023,22:11:54 slingBox2 1 Clients.192.168.1.10:63984", "slingBox2");
    }
}