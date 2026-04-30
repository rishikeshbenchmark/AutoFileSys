using DumpUtility.Models;

namespace DumpUtility.Services;

public interface IDataService
{
    IEnumerable<DocumentRecord> GetRecords();
}
