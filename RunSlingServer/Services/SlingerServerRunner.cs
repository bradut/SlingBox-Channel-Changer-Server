using Application.Abstractions;
using Application.Services;
using Application.SignalRServices;
using Domain.Models;
using RunSlingServer.Configuration.Services;
using System.Diagnostics;
using DataReceivedEventArgs = System.Diagnostics.DataReceivedEventArgs;

namespace RunSlingServer.Services
{
    /// <summary>
    /// Launches SlingBox_Server.exe and monitors its console output
    /// </summary>
    public class SlingerServerRunner
    {
        private readonly string _slingBoxServerExeName;
        private readonly string _slingBoxServerConfigFile;

        private readonly ConsoleColor _currentFontColor;
        private readonly string _serveRootPath;
        private readonly ConsoleDisplayDispatcher _console;

        private readonly SlingBoxServerStatus _serverStatus;
        private readonly ConsoleReaderService _consoleReaderService;
        private readonly string _slingBoxServerExeFullName;

        private string SlingBoxServerProcessName => _slingBoxServerExeName.Replace(".exe", "");

        public SlingerServerRunner(IAppConfiguration appConfiguration, string serveRootPath, ConsoleDisplayDispatcher console, 
                                   IFileSystemAccess fileService, ISignalRNotifier signalRNotifier)
        {
            _serveRootPath = serveRootPath;
            _console = console;
            _slingBoxServerExeName = appConfiguration.SlingboxServerExecutableName;
            _slingBoxServerConfigFile = appConfiguration.SlingBoxServerConfigFileName;

            _consoleReaderService = new ConsoleReaderService(console, fileService, appConfiguration.RemoteControlIrCodes, null, signalRNotifier);
            _slingBoxServerExeFullName = Path.Combine(_serveRootPath, _slingBoxServerExeName);
            _currentFontColor = Console.ForegroundColor;

            _serverStatus = fileService.LoadSlingBoxServerStatusFromFile() ?? new SlingBoxServerStatus();
        }


        public async Task RunAsync()
        {
            await RunSlingBoxServerAsync();
        }


        private async Task RunSlingBoxServerAsync()
        {
            await KillProcess(SlingBoxServerProcessName);

            await DisplaySlingServerParameters();


            if (!File.Exists(_slingBoxServerExeFullName))
            {
                await DisplaySlingBoxServerExeDoesNotExit();

                return;
            }

            await DisplaySlingStatusFilePath();

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _slingBoxServerExeName,
                    Arguments = _slingBoxServerConfigFile,

                    // Set UseShellExecute to false for redirection.
                    UseShellExecute = false,

                    // This stream is read asynchronously using an event handler.
                    RedirectStandardOutput = true,

                    RedirectStandardError = true, // new
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            // Note: comment out the next lines when running the WebApiService
            // var taskCompletionSource = new TaskCompletionSource<Process>();
            // process.Exited += (_, _) => taskCompletionSource.SetResult(process);


            process.ErrorDataReceived += ErrorOutputHandler;


            // Add event handler to capture console output
            process.OutputDataReceived += async (_, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                    return;

                var line = e.Data;
                await _consoleReaderService.ParseLogLineAsync(line);
            };


            process.Start();

            process.BeginOutputReadLine();
        }


        private void ErrorOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (outLine.Data != null) _console.WriteLine(outLine.Data);
        }


        private async Task DisplaySlingServerParameters()
        {
            using (await _console.GetLockAsync())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(
                    $"Running '{_slingBoxServerExeName}' with param '{_slingBoxServerConfigFile}'" +
                    $"{Environment.NewLine}{Environment.NewLine}");
                Console.ForegroundColor = ConsoleColor.Green;
            }
        }

        private async Task DisplaySlingStatusFilePath()
        {
            using (await _console.GetLockAsync())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                var configFilePath = Path.Combine(_serveRootPath, SlingBoxServerStatus.JSON_FILE_NAME);
                Console.WriteLine($"Current status is saved in {configFilePath}" +
                                  $"{Environment.NewLine}{Environment.NewLine}");
                Console.WriteLine(_serverStatus.ToString());
                Console.ForegroundColor = ConsoleColor.Green;
            }
        }

        private async Task DisplaySlingBoxServerExeDoesNotExit()
        {
            using (await _console.GetLockAsync())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"File {_slingBoxServerExeFullName} does not exist");
                Console.ForegroundColor = _currentFontColor;
            }
        }


        private async Task KillProcess(string processName)
        {
            var processesToKill = Process.GetProcessesByName(processName);
            foreach (var process in processesToKill)
            {
                process.Kill();
                await _console.WriteLineAsync($"Process {processName} was killed");
            }
        }
    }
}
