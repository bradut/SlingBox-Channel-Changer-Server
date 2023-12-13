using Domain.Models;

namespace Application.Interfaces;

public interface IConsoleReaderService
{
    SlingBoxServerStatus ServerStatus { get; }
    Task ParseLogLineAsync(string line);
}