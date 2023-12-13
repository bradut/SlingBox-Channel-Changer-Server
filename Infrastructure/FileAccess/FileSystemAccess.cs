using Application.Abstractions;
using Application.Helpers;
using Domain.Abstractions;
using Domain.Models;
using Microsoft.Extensions.Logging;

namespace Infrastructure.FileAccess;

public class FileSystemAccess : IFileSystemAccess
{
    private readonly ILogger<IFileSystemAccess> _logger;
    private readonly object _lockFileAccess = new();

    public string RootPath { get; }


    public FileSystemAccess(string rootPath, ILogger<IFileSystemAccess> logger)
    {
        RootPath = rootPath;
        _logger = logger;
    }
    

    public SlingBoxServerStatus? LoadSlingBoxServerStatusFromFile()
    {
        var sbServerStatus = new SlingBoxServerStatus();
        var fullPath = Path.Combine(RootPath, sbServerStatus.JsonFileName);


        if (!File.Exists(fullPath))
        {
            return null;
        }

        var jsonString = ReadFile(fullPath);

        if (string.IsNullOrWhiteSpace(jsonString))
        {
            var msg = $"File {fullPath} is EMPTY";
            Console.WriteLine(msg);
            lock (_lockFileAccess)
            {
                _logger.LogWarning(msg);
            }

            DeleteFile(fullPath);

            return null;
        }

        try
        {
            var slingBoxesDto = SlingBoxServerDeserializer.DeserializeJson(jsonString);
            sbServerStatus = MapSlingBoxServerStatusDtoToModel.Map(slingBoxesDto);

            return sbServerStatus;
        }
        catch (Exception e)
        {
            var msg = $"Error reading data from file {fullPath}: {e.Message}";
            Console.WriteLine(msg);
            lock (_lockFileAccess)
            {
                _logger.LogWarning(msg);
            }

            return null;
        }
    }


    private string ReadFile(string fullPath)
    {
        var jsonString = RetryFunction(() => File.ReadAllText(fullPath), "read");

        return jsonString;
    }




    public void SaveToJsonFile(ISerializeToJsonFile someObject)
    {
        var jsonString = someObject.ToJson();
        var fileName = someObject.JsonFileName;

        var fullPath = Path.Combine(RootPath, fileName);
        Console.WriteLine($"Saving to file {fullPath}");

        WriteFile(fullPath, jsonString);
    }

    private void WriteFile(string fullPath, string jsonString)
    {
        RetryFunction(() =>
        {
            File.WriteAllText(fullPath, jsonString);
            return true; // Dummy value to match Func signature
        }, "write");
    }


    private void DeleteFile(string fullPath)
    {
        RetryFunction(
            func: () => { File.Delete(fullPath); return true; },
            operationName: "delete");
    }


    private T RetryFunction<T>(Func<T> func, string operationName = "")
    {
        lock (_lockFileAccess)
        {
            var retries = 0;
            const int maxRetries = 3;

            while (retries < maxRetries)
            {
                try
                {
                    T result = func(); // Perform the actual operation
                    return result; // Operation successful, return the result
                }
                catch (Exception ex)
                {
                    var msg = $"Error: {ex.Message}";
                    Console.WriteLine(msg); _logger.LogError(msg);

                    retries++;

                    var retryInterval = GetRetryIntervalSeconds();
                    Thread.Sleep(retryInterval);
                }
            }

            throw new Exception($"Operation {operationName} failed after {maxRetries} retries.");
        }

        static TimeSpan GetRetryIntervalSeconds()
        {
            var seconds = new Random().Next(2, 5) * 1000;
            return TimeSpan.FromSeconds(seconds);
        }
    }

}