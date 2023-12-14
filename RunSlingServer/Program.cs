// ****[ 2023-06-18 ]***********************************************************************************
// Wrapper for the Slinger server to store server status and facilitate channel changes from a remote website.
// Starts the Slinger server and a web API.
// - Reads the SlingBox server console output and sends it to a remote channel-changer website via SignalR.
// - Receives channel change requests from TvGuide via the web API.
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
    var slingerConfigParser = serviceProvider.GetRequiredService<SlingerConfigurationParser>();
    var appConfiguration = serviceProvider.GetRequiredService<IAppConfiguration>();

    var slingBoxServerRunner = serviceProvider.GetRequiredService<SlingerServerRunner>();
    var webService = serviceProvider.GetRequiredService<WebApiService>();


    try
    {
        var result = SyncAppStatus.SynchronizeAppStatus(fileSystemAccess, slingerConfigParser, appConfiguration);
        if (!result.IsSuccess)
        {
            var errMsg = string.Join(",", result.ErrorMessages);
            DisplayMessage(errMsg);
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
    var errMsg = $"{msg} is already running. \n" +
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
