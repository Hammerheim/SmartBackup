using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  public class FileTreeLog : IFileLog
  {
    private Dictionary<string, FileInformation> log = new Dictionary<string, FileInformation>();

    public IEnumerable<FileInformation> Files
    {
      get
      {
        return log.Values;
      }
    }

    public void Add(FileInformation fileInformation)
    {
      if (log.ContainsKey(fileInformation.FullyQualifiedFilename))
        throw new Exception($"Duplicate key in file logger. \r\nNew file is {fileInformation.FileName}\r\nExisting file is: {log[fileInformation.FullyQualifiedFilename].FileName}");
      log.Add(fileInformation.FullyQualifiedFilename, fileInformation);
    }
  }
}
