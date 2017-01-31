using System.IO;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  public interface IHasher
  {
    Task<byte[]> GetHash(FileInfo file);
    Task<byte[]> GetHash(string source);
    Task<string> GetHashString(FileInfo file);
    Task<string> GetHashString(string source);
  }
}
