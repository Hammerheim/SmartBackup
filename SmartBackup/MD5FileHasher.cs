using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  internal class MD5FileHasher : IFileHasher
  {
    public async Task<byte[]> GetHash(FileInfo file)
    {
      using (var md5 = MD5.Create())
      {
        using (var stream = File.OpenRead(file.FullName))
        {
          return await Task.Run(() => md5.ComputeHash(stream));
        }
      }
    }

    public async Task<string> GetHashString(FileInfo file, bool deepScan)
    {
      if (deepScan)
      {
        var array = await GetHash(file);
        return array != null ? ByteToString.ByteArrayToString(array) : string.Empty;
      }
      return string.Empty;
    }

    //public string ToHashString(string source)
    //{
    //  using (var md5 = MD5.Create())
    //  {
    //    using (var stream = new MemoryStream())
    //    {
    //      return await Task.Run(() => md5.ComputeHash(stream));
    //    }
    //  }
    //}
  }
}
