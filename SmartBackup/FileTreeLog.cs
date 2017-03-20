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
    private Dictionary<string, string> ignoredExtensions;

    public FileTreeLog(List<string> ignoredExtensions) => this.ignoredExtensions = ignoredExtensions.ToDictionary(s => $".{s.ToLower()}", s => s);

    public IEnumerable<FileInformation> Files
    {
      get => log.Values;
    }

    public void Add(FileInformation fileInformation)
    {
      if (IsIgnored(fileInformation.FullyQualifiedFilename))
        return;

      if (log.ContainsKey(fileInformation.FullyQualifiedFilename))
        throw new Exception($"Duplicate key in file logger. \r\nNew file is {fileInformation.FileName}\r\nExisting file is: {log[fileInformation.FullyQualifiedFilename].FileName}");
      log.Add(fileInformation.FullyQualifiedFilename, fileInformation);
    }

    private bool IsIgnored(string filename)
    {
      var file = new FileInfo(filename);
      return (ignoredExtensions.ContainsKey(file.Extension.ToLower()));
    }
  }
}
