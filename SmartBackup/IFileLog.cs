using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  public interface IFileLog
  {
    void Log(FileInformation fileInformation);
    Task<bool> FileHandler(FileInfo info);
  }
}
