using Domain.Abstractions;
using Domain.Models;

namespace Application.Abstractions;

public interface IFileSystemAccess
{
    SlingBoxServerStatus? LoadSlingBoxServerStatusFromFile();
    void SaveToJsonFile(ISerializeToJsonFile someObject);
}