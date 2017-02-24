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
    FileInformation Gather(FileInfo file, DirectoryInfo root, bool deepScan);
  }
}
