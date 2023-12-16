using RunSlingServer.Configuration.Models;
using RunSlingServer.Configuration.Services;

namespace UnitTests.Application.ConsoleServicesTests;

public class SlingerConfigParserTests
{

    [Fact]
    public void Parse_ConfigIniFileData_ReturnsSlingBoxConfiguration()
    {
        // Arrange
        var sut = new SlingerConfigurationParser("");
        var configBody = OriginalConfigIniDataWithASingleSlingBox;

        // Act
        var config = sut.Parse(configBody);

        // Assert
        Assert.Equal(1, config.SlingBoxesCount);
        Assert.Equal(8080, config.Port);
        Assert.Empty(config.UrlBase);
        Assert.Equal(10, config.MaxRemoteStreams);

        var keyValuePair = config.SlingBoxes.FirstOrDefault();
        var slingBoxConfiguration = keyValuePair.Value;

        Assert.NotNull(slingBoxConfiguration);
        Assert.Equal("SlingBox1", slingBoxConfiguration.SlingBoxName);
        Assert.Equal("Solo/Pro/ProHD", slingBoxConfiguration.SlingBoxType);
        Assert.Equal(0, slingBoxConfiguration.VideoSource);
        Assert.True(slingBoxConfiguration.IsAnalogue);
    }


    [Fact]
    public void Parse_Unified_ConfigFileData_ReturnsMultipleSlingBoxesConfiguration()
    {
        // Arrange
        var sut = new SlingerConfigurationParser("");
        var configBody = UnifiedConfigIniDataWithMultipleSlingBoxes;

        // Act
        var config = sut.Parse(configBody);

        // Assert
        Assert.Equal(3, config.SlingBoxesCount);
        Assert.Equal(12345, config.Port);
        Assert.Equal("secret1234", config.UrlBase);
        Assert.Equal(5, config.MaxRemoteStreams);

        KeyValuePair<string, SlingBoxConfiguration> keyValuePair = config.SlingBoxes.FirstOrDefault();
        var slingConfig = keyValuePair.Value;

        Assert.NotNull(slingConfig);
        Assert.Equal("slingbox1", slingConfig.SlingBoxName);
        Assert.Equal("sb1", slingConfig.SlingBoxId);
        Assert.Equal("Solo/Pro/ProHD", slingConfig.SlingBoxType);
        Assert.Equal(1, slingConfig.VideoSource);
        Assert.Equal("remote.txt", slingConfig.RemoteControlFileName);
        Assert.False(slingConfig.IsAnalogue);


        keyValuePair = config.SlingBoxes.Single(kv => kv.Key == "slingbox2");
        slingConfig = keyValuePair.Value;
        Assert.True(slingConfig.IsAnalogue);
    }





    private const string UnifiedConfigIniDataWithMultipleSlingBoxes = @"
; This a sample of a unified config.ini for three slingboxes

;--[ Changinng channels in the textbox------------------------------------------
; somewhere in the updates it has this info, 
;You need to add a decimal point to the channel number so the code can tell the difference between IR and internal tuner. 
; - So in the US you can say 4.0 or 9.1 for subchannels. 
; - In the UK just put in the channel number with a decimal 101.
;---------------------------------------------------------------------------------


[SLINGBOXES]
sb1=slingbox1
sb2=slingbox2
sb3=slingbox3

[slingbox1]
sbtype=""Solo/Pro/ProHD""
password=slingbox1sbx    
ipaddress=192.168.1.254
;finderid=BAD95DAED609364C8204F2BC5FBDC363
port=5001

;Default resolution when server starts. You can change this and reconnect without having to restart the server
;valid range = 0..16 
Resolution=12

;Valid Values 1, 6, 10, 15, 20, 30, 60   depending on resolution
FrameRate=25

;Min 50 Max 8000
VideoBandwidth=8000

;Min 0 (auto) max 63
VideoSmoothness=63

;Video Source  0, 1, 2, 3 depending on your hardware corresponds to one of
;Composite, Component, S-Video, HDMI or Tuner.
;-------------------------------------------------------------------------
;ProHD:      0=Tuner           1=Composite   2=S-Video     3=Component
;-------------------------------------------------------------------------
; If you don't set this, the code will use the last configured input
; I recommend setting this value. If not set correctly, you'll often only see a black screen.
; Note: Setting invalid options may cause unexpected hardware behavior. For example, setting
;  2 or 3 for M2 hardware causes it to reboot.
VideoSource=1


;This will make the server only respond to remote control requests from the host that started the initial video stream
;from the slingbox. In other words, people who connect after the first stream will not be able to change
;the channel etc. while you’re watching your stream. This could be useful with the StartChannel option
;when implementing a stream recording to stop someone from changing the channel while recording
;slingbox1: to change it null yu need to leave it empty or to comment it out. Any value assigned to it will mean ""true"" ...
;RemoteLock=yes

;StartChannel=155
Remote=remote.txt


[slingbox2]
sbtype=""Solo/Pro/ProHD""
password=slingbox20000    
ipaddress=slingbox2.ddnsfree.com
;finderid=BAD95DAED609364C8204F2BC5FBDC363
port=5001
Resolution=12
FrameRate=25
VideoBandwidth=2500
VideoSmoothness=63
VideoSource=0
;RemoteLock=yes
;StartChannel=0
Remote=remote_ProHD_Tuner.txt



[slingbox3]
sbtype=""Solo/Pro/ProHD""
password=slingbox30000    
ipaddress=auroradima.ddnsfree.com
;finderid=BAD95DAED609364C8204F2BC5FBDC363
port=55002
Resolution=12
FrameRate=25
VideoBandwidth=2500
VideoSmoothness=63
VideoSource=0
;RemoteLock=yes
;StartChannel=155
Remote=remote_ProHD_Tuner_slingbox3.txt

; [500]
; sbtype=""350/500/M1""
; password=PasswordIalAakJ
; ;finderid=nogood4CF70531865D73650A16A0A536
; ipaddress=192.168.117.110
; port=5201
; Resolution=12
; FrameRate=60
; VideoBandwidth=2000
; VideoSmoothness=63
; VideoSource=1
; maxstreams=3
; StartChannel=155
; ;RemoteLock=yes
; Remote=500_remote.txt


; [M1]
; sbtype=""350/500/M1""
; password=SOmePassword
; ;finderid=E706A7405882C03A076502246DAD632F     
; ipaddress=192.168.117.129
; port=5301
; Resolution=12
; FrameRate=30
; VideoBandwidth=2000
; VideoSmoothness=63
; VideoSource=1
; Remote=m1_remote.txt

[SERVER]
; local port number for the server to listen on for connections
; Send all streaming and Remote request to this port number.
port=12345
maxremotestreams=5
;URLbase=Gerry
URLbase=secret1234
enableremote=yes
";



    private const string OriginalConfigIniDataWithASingleSlingBox = @"
[SLINGBOX]
;sbtype=""350/500/M1/M2""
sbtype=""Solo/Pro/ProHD""
;sbtype=""240""
password=admin


; uncomment the next lines ipaddress and port with your local network info
; if you don't want the server to automatically find your 
; slingbox on the local network. Needed if server and slingbox are
; not on the same LAN segment. Also removes requirement for the netifaces module  
;ipaddress=192.168.117.110
;port=5201

;ipaddress=12.34.56.78
;port=5001


ipaddress=192.168.1.254
port=5001


; Replace with your finderid, If and only if you need remote access and don't  
;have a static ip and you plan on using my service to access you server remotely. 
; Please read the release notes. Most people don't need this.
;finderid=BAD95DAED609364C8204F2BC5FBDC363



;If you've got more than one slingbox set name. The server will use this when generating 
;logs to make it easier to see what's going on. 
;;;name=MySling1

; Following are the default values
; Default resolution when server starts. You can change this and reconnect without having to restart the server
; valid range = 1..15
Resolution=12

; Valid Values 1, 6, 10, 15, 20, 30, 60   depending on resolution
FrameRate=30


;Min 50 Max 8000
VideoBandwidth=2000


; Min 0 (auto) max 63
VideoSmoothness=63

; Send Iframe every n seconds Max 30
IframeRate=5

; Audio Bit Rate. Valid Options 16, 20, 32, 40, 48, 64, 96 Default 32
AudioBitRate=32

; Video Source  0, 1, 2, 3 depending on your hardware corrosponds to one of
;Composite, Component, HDMI or Tuner.
;ProHD    0=Tuner,     1=composite   2=svideo     3=component
;Solo, M1 0=composite  1=Svideo      2=component
;500      0=composite, 1=component,  2=HDMI
; If you don't set this the code will use the last configured input
; I recommend setting this value. If not set correctly you'll often only see a black screen. 
VideoSource = 0

[SERVER]
; local port number for the server to listen on for connections
port=8080
maxstreams=10
enableremote=yes

[REMOTE]
; see release notes for changing this if your remote doesn't work, But as a start make it 
;the same as your configured VideoSource 0-3. See above.
code=1
; path to an external remote control definitions file, if the defaults
; aren't working for you or you want to change the format of the web page.
; use the supplied remote.txt as a starting point.
include=remote.txt 
";
}

