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
    public async Task<FileInformation> Gather(FileInfo file)
    {
      var info = new FileInformation
      {
        Directory = file.Directory.FullName,
        FileName = file.Name,
        LastModified = file.LastWriteTime,
        FilenameHash = file.FullName.GetHashCode(),
        ContentHash = await hasher.GetHashString(file)
      };
      return info;
    }
  }
}
