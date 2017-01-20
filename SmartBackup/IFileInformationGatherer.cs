using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  public interface IFileInformationGatherer
  {
    Task<FileInformation> Gather(FileInfo file, DirectoryInfo root, bool deepScan);
  }
}
