using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  public class SimpleFileLog : IFileLog
  {
    private StringBuilder log = new StringBuilder();

    public Task<bool> FileHandler(FileInfo info)
    {
      throw new NotImplementedException();
    }

    public void Log(FileInformation fileInformation)
    {
      log.Append($"{fileInformation.Directory},{fileInformation.FileName},{fileInformation.LastModified},{fileInformation.FullyQualifiedFilename},{fileInformation.ContentHash}");
    }

    public Task Read(string logFile)
    {
      throw new NotImplementedException();
    }

    public Task Save(string logFile)
    {
      throw new NotImplementedException();
    }

    public override string ToString()
    {
      return log.ToString();
    }
  }
}
