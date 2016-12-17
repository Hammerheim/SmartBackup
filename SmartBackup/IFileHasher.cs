using System.IO;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  public interface IFileHasher
  {
    Task<byte[]> GetHash(FileInfo file);
    Task<string> GetHashString(FileInfo file, bool deepScan);
    //string ToHashString(string source);
  }
}
