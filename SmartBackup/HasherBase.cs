using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  public abstract class HasherBase : IHasher
  {
    public virtual async Task<string> GetHashString(FileInfo file)
    {
      var array = await GetHash(file);
      return array != null ? ByteConverter.ByteArrayToString(array) : string.Empty;
    }

    protected abstract Task<byte[]> GetHash(FileInfo file);
  }
}
