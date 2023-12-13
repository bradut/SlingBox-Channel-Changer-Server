using Application.Interfaces;
using Nito.AsyncEx;

namespace Application.Services
{
    /// <summary>
    /// Allow only one service to write to the console at a time.
    ///
    /// Uses the AsyncEx library, which offers an AsyncLock class, created by Stephen Cleary
    /// GitHub: https://github.com/StephenCleary/AsyncEx
    /// </summary>
    public class ConsoleDisplayDispatcher : IDisposable, IConsoleDisplayDispatcher
    {
        private readonly AsyncLock _consoleLock = new AsyncLock();

        public void WriteLine(string message)
        {
            // acquire the lock synchronously (blocking the thread).
            using (_consoleLock.Lock()) 
            {
                Console.WriteLine(message);
            }
        }
        

        public async Task WriteLineAsync(string message)
        {
            using (await _consoleLock.LockAsync())
            {
                Console.WriteLine(message);
            }
        }


        public async Task<IDisposable> GetLockAsync()
        {
            return await _consoleLock.LockAsync();
        }

        public async Task ReleaseLockAsync()      {
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            // Cleaning up resources if any
        }
    }
}
