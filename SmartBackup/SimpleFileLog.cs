using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  public class SimpleFileLog : IFileLog
  {
    private StringBuilder log = new StringBuilder();

    public void Log(FileInformation fileInformation)
    {
      log.Append($"{fileInformation.Directory},{fileInformation.FileName},{fileInformation.LastModified},{fileInformation.FilenameHash},{fileInformation.ContentHash}");
    }

    public override string ToString()
    {
      return log.ToString();
    }
  }
}
