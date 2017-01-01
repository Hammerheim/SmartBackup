using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  internal class FileInformationGatherer : IFileInformationGatherer
  {
    private IFileHasher hasher;

    public FileInformationGatherer(IFileHasher hasher)
    {
      this.hasher = hasher;
    }
    public async Task<FileInformation> Gather(FileInfo file, bool deepScan)
    {
      var info = new FileInformation
      {
        Directory = file.Directory.FullName,
        FileName = file.Name,
        LastModified = file.LastWriteTime,
        FullyQualifiedFilename = file.FullName,
        Size = file.Length,
        ContentHash = deepScan ? await hasher.GetHashString(file) : string.Empty
      };
      return info;
    }
  }
}
