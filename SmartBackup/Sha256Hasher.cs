using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Vibe.Hammer.SmartBackup
{
  public class Sha256Hasher : HasherBase
  {
    protected override async Task<byte[]> GetHash(FileInfo file)
    {
      using (var sha256 = SHA256.Create())
      {
        using (var stream = File.OpenRead(file.FullName))
        {
          return await Task.Run(() => sha256.ComputeHash(stream));
        }
      }
    }
  }
}
