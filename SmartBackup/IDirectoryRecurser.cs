using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vibe.Hammer.SmartBackup.Progress;

namespace Vibe.Hammer.SmartBackup
{
  public interface IDirectoryRecurser
  {
    Task<bool> RecurseDirectory(DirectoryInfo root, IFileHandler fileHandler, bool deepScan, IProgress<ProgressReport> progressCallback);

  }
}
