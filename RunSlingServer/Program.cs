// ****[ 2023-06-18 ]***********************************************************************************
// SlingerServerWrapper: A wrapper for the Slinger server, managing server status storage and facilitating remote channel changes.
// - Initiates the Slinger server and a web API.
// - Monitors SlingBox server console output and communicates it to a remote channel-changer website through SignalR.
// - Accepts channel change requests from TvGuide via SignalR and the web API.
// *****************************************************************************************************
// Copyright: Bradut Dima 2023 under the MIT license
// *****************************************************************************************************

using Application.Abstractions;
using RunSlingServer;
using RunSlingServer.Configuration.Services;
using RunSlingServer.Helpers;
using RunSlingServer.Services;
using RunSlingServer.WebApi.Services;


const string appName = "RunSlingServer";

using var mutex = new Mutex(true, appName, out var createdNewInstance);
if (!createdNewInstance)
{
    DisplayErrorAnotherInstanceIsRunning(appName);
    return;
}


await Run(args);
return;


static async Task Run(string[] args)
{
    var serviceProvider = ConsoleAppDependencyInjection.GetServiceProvider(args);

    var fileSystemAccess = serviceProvider.GetRequiredService<IFileSystemAccess>();
    var slingerConfigParser = serviceProvider.GetRequiredService<ISlingerConfigurationParser>();
    var appConfiguration = serviceProvider.GetRequiredService<IAppConfiguration>();

    var slingBoxServerRunner = serviceProvider.GetRequiredService<SlingerServerRunner>();
    var webService = serviceProvider.GetRequiredService<WebApiService>();


    try
    {
        DisplayAppVersion(appConfiguration.Version);

        var result = SyncAppStatus.SynchronizeAppStatus(fileSystemAccess, slingerConfigParser, appConfiguration);
        if (!result.IsSuccess)
        {
            DisplayMessage(string.Join(",", result.ErrorMessages));
            return;
        }


        await slingBoxServerRunner.RunAsync();

        await webService.RunAsync();
    }
    catch (Exception e)
    {
        DisplayMessage(e.Message);
    }
}



static void DisplayErrorAnotherInstanceIsRunning(string msg)
{
    var errMsg = $"{msg} is already running.\n" +
                 "ONLY ONE INSTANCE can be active at a time.";

    DisplayMessage(errMsg);
}



static void DisplayMessage(string message)
{
    var color = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(
        "*******************************************************************************\n" +
         $"{message} \n" +
        "*******************************************************************************\n");
    Console.ForegroundColor = color;
    Console.WriteLine("Press any key to continue...");

    SoundPlayer.PlayArpeggio();

    Task.Factory.StartNew(Console.ReadKey).Wait(TimeSpan.FromSeconds(5.0));
}

static void DisplayAppVersion(string appVersion)
{
    Console.WriteLine("\n");
    Console.WriteLine("███████████████████████████████████████████████████████████████████████████████");
    Console.WriteLine($"██                  {appName}  -  Version {appVersion}                    ██ ");
    Console.WriteLine("███████████████████████████████████████████████████████████████████████████████\n\n");
}
