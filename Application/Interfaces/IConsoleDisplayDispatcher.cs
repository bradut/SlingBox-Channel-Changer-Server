namespace Application.Interfaces;

public interface IConsoleDisplayDispatcher
{
    void WriteLine(string message);
    Task WriteLineAsync(string message);
    Task<IDisposable> GetLockAsync();
    Task ReleaseLockAsync();
}