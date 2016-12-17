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
    private Dictionary<int, FileInformation> log = new Dictionary<int, FileInformation>();

    public void Log(FileInformation fileInformation)
    {
      if (log.ContainsKey(fileInformation.FilenameHash))
        throw new Exception($"Duplicate key in file logger. \r\nNew file is {fileInformation.FileName}\r\nExisting file is: {log[fileInformation.FilenameHash].FileName}");
      log.Add(fileInformation.FilenameHash, fileInformation);
    }

    public async Task Read(string logFile)
    {
      FileInfo fi = new FileInfo(logFile);
      if (!fi.Exists)
        return;

      using (var stream = fi.OpenText())
      {
        var logWriterType = await stream.ReadLineAsync();
        if (logWriterType == GetType().FullName)
        {
          do
          {
            var line = await stream.ReadLineAsync();
            var item = new FileInformation(line);
            log.Add(item.FilenameHash, item);
          } while (!stream.EndOfStream);
        }
      }
    }

    public async Task Save(string logFile)
    {
      FileInfo fi = new FileInfo(logFile);
      if (fi.Exists)
        fi.Delete();

      try
      {
        using (var writer = fi.CreateText())
        {
          await writer.WriteLineAsync(GetType().FullName);
          foreach (var id in log.Keys)
          {
            await writer.WriteLineAsync(log[id].ToString());
          }
        }

      }
      catch (Exception err)
      {
        Console.WriteLine(err.ToString());
        throw;
      }
    }

    public override string ToString()
    {
      return "Not much to say...";
    }
  }
}
